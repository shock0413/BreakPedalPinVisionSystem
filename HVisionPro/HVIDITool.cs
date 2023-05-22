using Cognex.VisionPro;
using HCore;
using HUtill;
using HVisionPro.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace HVisionPro
{
    public class HVIDITool : HTool
    {
        

        private ObservableCollection<VidiRegex> regices = new ObservableCollection<VidiRegex>();
        public ObservableCollection<VidiRegex> Regices { get { return regices; } set { regices = value; NotifyPropertyChanged("Regices"); } }

        private ObservableCollection<HTool> beforeTool = new ObservableCollection<HTool>();
        public ObservableCollection<HTool> BeforeTool { get { return beforeTool; } set { beforeTool = value; NotifyPropertyChanged("BeforeTool"); } }

        IniFile config;

        private HCognexDisplay cogDisplay;
        public HCognexDisplay CogDisplay
        {
            get { return cogDisplay; }
            set { cogDisplay = value; }
        }

        public string ToolName { get; set; }

        private string dbPath;
        public string DBPath { get { return dbPath; } set { dbPath = value; NotifyPropertyChanged("DBPath"); } }

        private string serverIP;
        public string ServerIP { get { return serverIP; } set { serverIP = value; NotifyPropertyChanged("ServerIP"); } }

        private int serverPort;
        public int ServerPort { get { return serverPort; } set { serverPort = value; NotifyPropertyChanged("ServerPort"); } }

        private string streamName;
        public string StreamName { get { return streamName; } set { streamName = value; NotifyPropertyChanged("StreamName"); } }

        private ObservableCollection<VidiMatch> matches;
        public ObservableCollection<VidiMatch> Matches { get { return matches; } set { matches = value; NotifyPropertyChanged("Matches"); } }

        public string VidiRegexPath;

        public HVIDITool(HCameraShot shot, HInspection inspection, HPart part, string toolName) :
            base(shot, inspection, part, toolName)
        {
            // config = new IniFile(ToolIniPath);
            // config = new IniFile(Environment.CurrentDirectory + "\\Config.ini");
            config = new IniFile(inspection.IniPath);
            this.ToolName = config.GetString("ViDi Runtime", "ToolName", "");
            // VidiRegexPath = Dir + "\\" + toolName + "_Vidi Regex.hsr";
            VidiRegexPath = Environment.CurrentDirectory + "\\Camera\\" + shot.Name + "_Vidi Regex.hsr";
        }

        public override void LoadTool()
        {
            dbPath = config.GetString("ViDi Runtime", "Path", "");
            serverIP = config.GetString("Server", "IP", "127.0.0.1");
            serverPort = config.GetInt32("Server", "Port", 9995);
            streamName = config.GetString("ViDi Runtime", "Stream Name", "default");

            LoadRegex();
        }

        public override UIElement LoadToolUI()
        {
            HVidiToolUI tool = new HVidiToolUI(this);
            return tool;
        }

        public void SaveDBPath()
        {
            config.WriteValue("ViDi Runtime", "Path", dbPath);
            config.WriteValue("Server", "IP", serverIP);
            config.WriteValue("Server", "Port", serverPort);
            config.WriteValue("ViDi Runtime", "Stream Name", streamName);
        }

        public override void SetDisplay(object display)
        {
            CogDisplay = (HCognexDisplay)display;
        }

        public override HResult Run()
        {

            List<VidiMatch> matches = new List<VidiMatch>();

            VIDIResult result = new VIDIResult();
            result.IsOK = true;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            socket.ReceiveTimeout = 10000;
            try
            {
                IPAddress[] ips = Dns.GetHostAddresses(ServerIP);
                IPEndPoint remoteEP = new IPEndPoint(ips[0], ServerPort);
                socket.Connect(remoteEP);

                //데이터 전달
                string workSpaceName = ToolName;
                string workSpacePath = DBPath;
                string streamName = this.StreamName;

                Bitmap bitmap = null;

                cogDisplay.Dispatcher.Invoke(() =>
                {
                    bitmap = cogDisplay.Image.ToBitmap();
                });

                int height = bitmap.Height;
                int width = bitmap.Width;
                int channels = 3;
                if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    channels = 1;
                }


                BitmapData bmpData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                IntPtr ptr = bmpData.Scan0;

                int stride = bmpData.Stride;
                int bmpBytes = Math.Abs(bmpData.Stride) * bitmap.Height;

                byte[] header = new byte[33];

                byte[] workSpaceNameBytes = Encoding.Default.GetBytes(workSpaceName);
                byte[] workSpacePathBytes = Encoding.Default.GetBytes(workSpacePath);
                byte[] streamNameBytes = Encoding.Default.GetBytes(streamName);

                Buffer.BlockCopy(new byte[] { 0x02 }, 0, header, 0, 1);
                Buffer.BlockCopy(BitConverter.GetBytes(workSpaceNameBytes.Length), 0, header, 1, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(workSpacePathBytes.Length), 0, header, 5, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(streamNameBytes.Length), 0, header, 9, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(height), 0, header, 13, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(width), 0, header, 17, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(channels), 0, header, 21, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(stride), 0, header, 25, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(bmpBytes), 0, header, 29, 4);

                byte[] datas = new byte[workSpaceNameBytes.Length + workSpacePathBytes.Length + streamNameBytes.Length + bmpBytes];
                Buffer.BlockCopy(workSpaceNameBytes, 0, datas, 0, workSpaceNameBytes.Length);
                Buffer.BlockCopy(workSpacePathBytes, 0, datas, workSpaceNameBytes.Length, workSpacePathBytes.Length);
                Buffer.BlockCopy(streamNameBytes, 0, datas, workSpaceNameBytes.Length + workSpacePathBytes.Length, streamNameBytes.Length);
                Marshal.Copy(ptr, datas, workSpaceNameBytes.Length + workSpacePathBytes.Length + streamNameBytes.Length, bmpBytes);

                int totalLength = 4 + header.Length + datas.Length;
                byte[] totalData = new byte[totalLength];
                Buffer.BlockCopy(BitConverter.GetBytes(totalLength), 0, totalData, 0, 4);
                Buffer.BlockCopy(header, 0, totalData, 4, header.Length);
                Buffer.BlockCopy(datas, 0, totalData, 4 + header.Length, datas.Length);



                socket.Send(totalData);
                byte[] receiveBuffer = new byte[65535];
                 

                //데이터 수신
                byte[] totalReceiveBuffer = new byte[] { };
                while (true)
                {
                    Console.WriteLine("receivebuffer");
                    int reads = socket.Receive(receiveBuffer);
                    Console.WriteLine("reads");
                    byte[] newTotalReceiveBuffer = new byte[totalReceiveBuffer.Length + reads];

                    Buffer.BlockCopy(totalReceiveBuffer, 0, newTotalReceiveBuffer, 0, totalReceiveBuffer.Length);
                    Buffer.BlockCopy(receiveBuffer, 0, newTotalReceiveBuffer, totalReceiveBuffer.Length, reads);

                    totalReceiveBuffer = newTotalReceiveBuffer;
                    Console.WriteLine("1");
                    if (totalReceiveBuffer.Length > 4)
                    {
                        Console.WriteLine("2");
                        int frameSize = BitConverter.ToInt32(new byte[] { totalReceiveBuffer[0], totalReceiveBuffer[1], totalReceiveBuffer[2], totalReceiveBuffer[3] }, 0);

                        //데이터가 다 들어옴
                        if (totalReceiveBuffer.Length >= frameSize)
                        {
                            Console.WriteLine("3");
                            byte[] currentData = new byte[frameSize];
                            Buffer.BlockCopy(totalReceiveBuffer, 0, currentData, 0, frameSize);

                            int headerSize = BitConverter.ToInt32(new byte[]{currentData[4], currentData[5], currentData[6], currentData[7]}, 0);

                            byte[] headerFrame = new byte[headerSize];
                            Buffer.BlockCopy(currentData, 8, headerFrame, 0, headerSize - 4);

                            XmlDocument xmlDocument = new XmlDocument();
                            xmlDocument.LoadXml(Encoding.Default.GetString(headerFrame));


                            byte[] dataFrame = new byte[frameSize - headerSize - 4];
                            Buffer.BlockCopy(currentData, headerSize + 4, dataFrame, 0, dataFrame.Length);

                            try
                            {
                                List<byte[]> graphicResults = new List<byte[]>();
                                for (int pibot = 0; pibot < dataFrame.Length;)
                                {
                                    int size = BitConverter.ToInt32(new byte[] { dataFrame[pibot], dataFrame[pibot + 1], dataFrame[pibot + 2], dataFrame[pibot + 3] }, 0);
                                    byte[] data = new byte[size];
                                    pibot += 4;
                                    Buffer.BlockCopy(dataFrame, pibot, data, 0, size);
                                    pibot += size;

                                    graphicResults.Add(data);
                                }

                                List<ICogGraphic> graphics = new List<ICogGraphic>();
                                for (int i = 0; i < graphicResults.Count; i++)
                                {
                                    using (MemoryStream ms = new MemoryStream(graphicResults[i]))
                                    {
                                        object obj = CogSerializer.LoadObjectFromStream(ms);
                                        graphics.Add((ICogGraphic)obj);
                                    }

                                }

                                CogGraphicInteractiveCollection collection = new CogGraphicInteractiveCollection();
                                for (int i = 0; i < graphics.Count; i++)
                                {
                                    if (graphics[i].GetType() == typeof(CogCompositeShape))
                                    {
                                        CogCompositeShape shape = (CogCompositeShape)graphics[i];
                                        //if(shape.Color == CogColorConstants.Yellow)
                                        {
                                            collection.Add((ICogGraphicInteractive)graphics[i]);
                                        }
                                    }
                                    else
                                    {
                                        collection.Add((ICogGraphicInteractive)graphics[i]);
                                    }

                                }

                                List<VidiToolResult> toolResults = new List<VidiToolResult>();

                                for (int i = 0; i < xmlDocument.DocumentElement.ChildNodes.Count; i++)
                                {
                                    XmlNode toolElement = xmlDocument.DocumentElement.ChildNodes[i];
                                    string toolName = toolElement.Attributes[0].Value;

                                    List<VidiMatch> toolMatches = new List<VidiMatch>();
                                    VidiToolResult toolResult = new VidiToolResult(toolName, toolMatches);

                                    for (int j = 0; j < toolElement.ChildNodes.Count; j++)
                                    {
                                        XmlNode matchElement = toolElement.ChildNodes[j];
                                        string matchName = matchElement.Attributes[0].Value;
                                        string featureString = "";

                                        if (matchElement.Attributes.Count > 1)
                                        {
                                            featureString = matchElement.Attributes[1].Value;
                                        }

                                        VidiMatch match = new VidiMatch(toolResult, matchName);
                                        match.FeatureString = featureString;
                                        toolMatches.Add(match);
                                        matches.Add(match);

                                        toolResult.Matches.Add(match);
                                    }

                                    toolResults.Add(toolResult);
                                }

                                result.Tools = new ObservableCollection<VidiToolResult>(toolResults);

                                cogDisplay.Engine.CogDisplay.InteractiveGraphics.AddList(collection, "", false);

                                break;
                            }
                            catch(Exception ex)
                            {

                                break;
                            }
                            
                        }
                        
                    }
                } 
            }
            catch(Exception ex)
            {
                
                result.IsOK = false;
            }

            Matches = new ObservableCollection<VidiMatch>(matches);

            bool[] okList = new bool[regices.Count];
            for(int i = 0; i < regices.Count; i++)
            {
                VidiRegex regex = regices[i];

                List<VidiMatch> toolMatches = Matches.Where(x => x.ToolName == regices[i].ToolName).ToList();
                int findCount = toolMatches.Count;

                string term = regex.Term;
                string value = regex.Value;

                int FeatureStringSameCount = 0;

                bool currentResult = false;

                if(term == "Match Count =")
                {
                    int termValue = 0;
                    if(Int32.TryParse(value, out termValue) && findCount == termValue)
                    {
                        if (FeatureStringSameCount == findCount)
                        {
                            currentResult = true;
                        }
                        else
                        {
                            currentResult = false;
                        }
                    }
                }
                else if(term == "Match Count <")
                {
                    int termValue = 0;
                    if (Int32.TryParse(value, out termValue) && findCount < termValue)
                    {
                        currentResult = true;
                    }
                }
                else if (term == "Match Count ≤")
                {
                    int termValue = 0;
                    if (Int32.TryParse(value, out termValue) && findCount <= termValue)
                    {
                        currentResult = true;
                    }
                }
                else if (term == "Match Count >")
                {
                    int termValue = 0;
                    if (Int32.TryParse(value, out termValue) && findCount > termValue)
                    {
                        currentResult = true;
                    }
                }
                else if (term == "Match Count ≥")
                {
                    int termValue = 0;
                    if (Int32.TryParse(value, out termValue) && findCount >= termValue)
                    {
                        currentResult = true;
                    }
                }
                else if(term == "Feature String =")
                {
                    if (value == "OK")
                    {
                        if (toolMatches.Where(x => x.FeatureString == value).Count() > 0)
                        {
                            FeatureStringSameCount += toolMatches.Where(x => x.FeatureString == value).Count();
                            currentResult = true;
                        }

                        if (toolMatches.Where(x => x.FeatureString == "NG").Count() > 0)
                        {
                            currentResult = false;
                        }
                    }
                    else
                    {
                        if (toolMatches.Where(x => x.FeatureString == value).Count() > 0)
                        {
                            currentResult = true;
                        }
                    }

                }
                else if(term == "Feature String Contain")
                {
                    if (toolMatches.Where(x => x.FeatureString.Contains(value)).Count() > 0)
                    {
                        currentResult = true;
                    }
                }

                okList[i] = currentResult;
            }

            if(okList.Where(x=> x == false).Count() == 0)
            {
                result.IsOK = true;
            }
            else
            {
                result.IsOK = false;
            }

            return result;
        }

        public void LoadRegex()
        {
            List<VidiRegex> regices = new List<VidiRegex>();

            if(File.Exists(VidiRegexPath) == true)
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(VidiRegexPath);

                XmlElement root = xmlDocument.DocumentElement;
                for(int i = 0; i < root.ChildNodes.Count; i++)
                {
                    XmlNode regex = root.ChildNodes[i];
                    string toolName = regex.Attributes.GetNamedItem("ToolName").Value;
                    string term = regex.Attributes.GetNamedItem("Term").Value;
                    string value = regex.Attributes.GetNamedItem("Value").Value;

                    VidiRegex vidiRegex = new VidiRegex();
                    vidiRegex.ToolName = toolName;
                    vidiRegex.Term = term;
                    vidiRegex.Value = value;

                    regices.Add(vidiRegex);
                }
            }

            Regices = new ObservableCollection<VidiRegex>(regices);
            
        }

        public class VIDIResult : HResult
        {
            private ObservableCollection<VidiToolResult> tools;
            public ObservableCollection<VidiToolResult> Tools { get { return tools; } internal set { tools = value; } }
            public VIDIResult()
            {

            }
        }

        public class VidiToolResult
        {
            public string ToolName{ get { return toolName; } }
            string toolName;
            private ObservableCollection<VidiMatch> matches;
            public ObservableCollection<VidiMatch> Matches { get { return matches; } }

            public VidiToolResult(string toolName, List<VidiMatch> matches)
            {
                this.toolName = toolName;
                this.matches = new ObservableCollection<VidiMatch>(matches);
            }

            
        }

        public class VidiMatch
        {
            string name;
            public string Name { get { return name; } }
            VidiToolResult tool;

            string featureString;
            public string FeatureString { get { return featureString; }  internal set { featureString = value; } }

            public string ToolName{get{ return tool.ToolName; } }

            public VidiMatch(VidiToolResult tool, string name)
            {
                this.tool = tool;
                this.name = name;
            }
        }

        public class VidiRegex
        {
            public string ToolName { get; set; }
            public string Term { get; set; }
            public string Value { get; set; }


            private static ObservableCollection<string> terms = new ObservableCollection<string>(){
                "Match Count =",
                "Match Count <",
                "Match Count ≤",
                "Match Count >",
                "Match Count ≥",
                "Feature String =",
                "Feature String Contain"
            };

            public static ObservableCollection<string> Terms { get { return terms; } set { terms = value; } }
        }


    }
}
