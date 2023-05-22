using HAsyncSocket;
using HCore;
using HVisionPro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HInspection
{
    public class InspectionManager
    {
        private ObservableCollection<HTool> tools = new ObservableCollection<HTool>();
        public ObservableCollection<HTool> Tools { get { return tools; } set { tools = value; } }

        private List<HModel> models = new List<HModel>();
        public List<HModel> Models { get { return models; } set { models = value; } }

        private AsyncSocketClient client;


        private bool isStopThread = false;

        private bool isConnected = false;
        public bool IsConnected { get { return isConnected; } }


        private int reconnectTime;
        public int ReconnectTime { get { return reconnectTime; } }


        private string cameraServerIP;
        public string CameraServerIP { get { return cameraServerIP; } }

        private int cameraServerPort;
        public int CameraServerPort { get { return cameraServerPort; } }

        Thread connectThread;

        object receiveImageLock = new object();

        public InspectionManager()
        {
 
        }

        public InspectionManager(string cameraServerIP, int cameraServerPort, int reconnectTime)
        {
            this.cameraServerIP = cameraServerIP;
            this.cameraServerPort = cameraServerPort;
            this.reconnectTime = reconnectTime;
        }

        public void Start()
        {
            connectThread = new Thread(ConnectThreadDo);
            connectThread.Start();
        }

        private void ConnectThreadDo()
        {
            isStopThread = false;
            while (isStopThread == false)
            {
                try
                {
                    if (isConnected == false)
                    {
                        if (Connect() == false)
                        {
                            Thread.Sleep(reconnectTime);
                        }
                        else
                        {
                            isConnected = true;
                        }
                    }
                    else
                    {
                        Thread.Sleep(reconnectTime);
                    }
                }
                catch
                {
                    Thread.Sleep(reconnectTime);
                }
            }
        }

        private bool Connect()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client = new AsyncSocketClient(0, sock);
            bool isConnected = client.Connect(CameraServerIP, CameraServerPort);
            client.OnConnet += Client_OnConnet;
            client.OnReceive += Client_OnReceive;
            client.OnSend += Client_OnSend;
            client.OnError += Client_OnError;

            return isConnected;
        }


        private void Client_OnError(object sender, AsyncSocketErrorEventArgs e)
        {
            isConnected = false;

        }

        private void Client_OnConnet(object sender, AsyncSocketConnectionEventArgs e)
        {

        }

        private void Client_OnSend(object sender, AsyncSocketSendEventArgs e)
        {

        }

        byte[] beforeReceivedData = new byte[0];

        //private BitmapSource lastReceiveImage;
        Queue<BitmapSource> receiveImage = new Queue<BitmapSource>();

        private void Client_OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            byte[] newReceiveData = new byte[e.ReceiveBytes + beforeReceivedData.Length];
            Buffer.BlockCopy(beforeReceivedData, 0, newReceiveData, 0, beforeReceivedData.Length);
            Buffer.BlockCopy(e.ReceiveData, 0, newReceiveData, beforeReceivedData.Length, e.ReceiveBytes);

            int frameSize = BitConverter.ToInt32(new byte[] { newReceiveData[0], newReceiveData[1], newReceiveData[2], newReceiveData[3] }, 0);

            if (newReceiveData.Length >= frameSize)
            {
                byte[] currentFrame = new byte[frameSize];
                Buffer.BlockCopy(newReceiveData, 0, currentFrame, 0, frameSize);

                byte[] imageBuffer = new byte[frameSize - 4];
                Buffer.BlockCopy(currentFrame, 4, imageBuffer, 0, imageBuffer.Length);

                BitmapImage bitmapSource = new BitmapImage();
                using (var mem = new MemoryStream(imageBuffer))
                {
                    mem.Position = 0;
                    bitmapSource.BeginInit();
                    bitmapSource.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    bitmapSource.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapSource.UriSource = null;
                    bitmapSource.StreamSource = mem;
                    bitmapSource.EndInit();
                }
                bitmapSource.Freeze();
                receiveImage.Enqueue(bitmapSource);

                //lock (receiveImageLock)
                //{
                //    Monitor.Pulse(receiveImageLock);
                //}


                beforeReceivedData = new byte[newReceiveData.Length - frameSize];
                Buffer.BlockCopy(newReceiveData, frameSize, beforeReceivedData, 0, newReceiveData.Length - frameSize);
            }
            else
            {
                beforeReceivedData = newReceiveData;
            }
        }

        public void SendMsg(byte[] msg)
        {
            if(client != null)
            {
                client.Send(msg);
            }
        }

        public void ClearReceiveImageQueue()
        {
            receiveImage.Clear();
        }

        public BitmapSource RequestImage(HCameraShot shot)
        {
            if (client != null)
            {
                //lastReceiveImage = null;
                //string serialNumber = shot.SerialNumber;
                //long exposure = shot.Exposure;

                //List<byte> dataBuffer = new List<byte>();
                //dataBuffer.AddRange(BitConverter.GetBytes((short)serialNumber.Length));
                //dataBuffer.AddRange(BitConverter.GetBytes(exposure));
                //dataBuffer.AddRange(Encoding.ASCII.GetBytes(serialNumber));
                //List<byte> frameBuffer = new List<byte>();
                //frameBuffer.AddRange(BitConverter.GetBytes((short)(dataBuffer.Count + 2)));
                //frameBuffer.AddRange(dataBuffer);

                //client.Send(frameBuffer.ToArray());

                //lock (receiveImageLock)
                {
                    //if (Monitor.Wait(receiveImageLock, 2000))
                    //{

                    //}
                    //else
                    //{
                    //    return null;
                    //}
                    Stopwatch escapeSw = new Stopwatch();
                    escapeSw.Start();

                    BitmapSource bitmapSource = null;

                    while (escapeSw.ElapsedMilliseconds < 2000)
                    {
                        if (receiveImage.Count > 0)
                        {
                            bitmapSource = receiveImage.Dequeue();

                            try
                            {
                                int rotate = shot.Rotate;

                                bitmapSource = new TransformedBitmap(bitmapSource, new RotateTransform(rotate));
                                if (bitmapSource.CanFreeze)
                                {
                                    bitmapSource.Freeze();
                                }
                            }
                            catch
                            {

                            }

                            break;
                        }
                         

                    }


                    return bitmapSource;
                }
            }
            else
            {
                return null;
            }
        }

        public List<HModel> LoadModel()
        {
            List<HModel> result = new List<HModel>();
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Model\\";
            if (Directory.Exists(path))
            {
                string[] models = Directory.GetFiles(path);
                for(int i = 0; i < models.Length; i++)
                {
                    HModel model = new HModel(models[i]);
                    result.Add(model);
                }
            }
            Models = result;
            return result;
        }

        public List<HCameraShot> LoadCamera()
        {
            List<HCameraShot> result = new List<HCameraShot>();
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Camera\\";
            if (Directory.Exists(path))
            {
                string[] cameras = Directory.GetFiles(path);
                for (int i = 0; i < cameras.Length; i++)
                {
                    HCameraShot camera = new HCameraShot(cameras[i]);
                    result.Add(camera);
                }
            }

            return result;
        }

        public List<HCore.HInspection> LoadInspection()
        {
            List<HCore.HInspection> result = new List<HCore.HInspection>();
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Inspection\\";
            if (Directory.Exists(path))
            {
                string[] inspections = Directory.GetFiles(path);
                for (int i = 0; i < inspections.Length; i++)
                {
                    HCore.HInspection inspection = new HCore.HInspection(inspections[i]);
                    result.Add(inspection);
                }
            }

            return result;
        }

        public void LoadTools(HCameraShot shot, HCore.HInspection inspection, HPart part)
        {
            tools.Clear();
            if(inspection != null && shot != null && part != null)
            {
                List<KeyValuePair<string, string>> loadToolNames = inspection.LoadToolsName(shot, part);

                for (int i = 0; i < loadToolNames.Count; i++)
                {
                    string toolName = loadToolNames[i].Key;
                    string toolType = loadToolNames[i].Value;
                    if (toolType == HCognexToolTypeConstants.PatternTool.ToString())
                    {
                        HPatternTool patternTool = new HPatternTool(shot, inspection, part, toolName);
                        for(int j  = 0; j < tools.Count; j++)
                        {
                            patternTool.BeforeTool.Add(tools[j]);
                        }
                        tools.Add(patternTool);
                    }
                    else if(toolType == HCognexToolTypeConstants.BarcodeTool)
                    {
                        HBarcodeTool barcodeTool = new HBarcodeTool(shot, inspection, part, toolName);
                        for (int j = 0; j < tools.Count; j++)
                        {
                            barcodeTool.BeforeTool.Add(tools[j]);
                        }
                        tools.Add(barcodeTool);
                    }
                    else if(toolType == HCognexToolTypeConstants.BlobTool)
                    {
                        HBlobTool blobTool = new HBlobTool(shot, inspection, part, toolName);
                        for(int j = 0; j < tools.Count; j++)
                        {
                            blobTool.BeforeTool.Add(tools[j]);
                        }
                        tools.Add(blobTool);
                    }
                    else if(toolType == HCognexToolTypeConstants.VIDITool)
                    {
                        HVIDITool vidiTool = new HVIDITool(shot, inspection, part, toolName);
                        tools.Add(vidiTool);
                    }
                    else
                    {

                    }
                }
            }
            else
            {
                tools = new ObservableCollection<HTool>();
            }
        }

        public List<UIElement> LoadToolsUI()
        {
            List<UIElement> result = new List<UIElement>();

            for(int i = 0; i < tools.Count; i++)
            {
                tools[i].LoadTool();
                result.Add(tools[i].LoadToolUI());
            }

            return result;
        }

        public HModel GetModelByName(string name)
        {
            List<HModel> findModels = Models.Where(x => x.Name == name).ToList();
            if(findModels.Count > 0)
            {
                return findModels[0];
            }
            else
            {
                return null;
            }
        }
         
    }
}
