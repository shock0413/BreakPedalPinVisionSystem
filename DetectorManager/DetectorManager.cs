using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AsyncDetectSocket;
using HUtill;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;

namespace DetectorManager
{
    public class DetectorManager
    {
        private string path = Environment.CurrentDirectory + "\\Detector\\Detector.exe";
        private Process mainProcess = null;
        private ProcessStartInfo info = null;
        private AsyncSocketServer detectServer = null;
        private AsyncSocketClient detectClient = null;
        private int detectClientId = 0;
        private IniFile m_Config = null;
        private int netWidth = 416;
        private int netHeight = 416;
        private int netChannels = 3;
        private int sendImageIndex = 0;
        private List<string> m_Process_Recv = new List<string>();


        private bool isProcess = false;
        public bool IsProcess
        {
            get
            {
                return isProcess;
            }
            set
            {
                isProcess = value;
            }
        }

        public DetectorManager()
        {
            m_Config = new IniFile(Environment.CurrentDirectory + "\\Config.ini");
            InitServer();
        }

        private void InitServer()
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    detectServer = new AsyncSocketServer(9960);
                    detectServer.OnAccept += new AsyncSocketAcceptEventHandler(OnAccept);
                    detectServer.OnError += new AsyncSocketErrorEventHandler(OnError);
                    detectServer.OnSend += new AsyncSocketSendEventHandler(OnSend);
                    detectServer.OnClose += new AsyncSocketCloseEventHandler(OnClose);
                    detectServer.Listen(IPAddress.Parse("127.0.0.1"));
                }
                catch
                {

                }
            })).Start();
        }

        private void OnAccept(object sender, AsyncSocketAcceptEventArgs e)
        {
            if (detectClient != null)
            {
                detectClient.Close();
                detectClient = null;
            }

            detectClient = new AsyncSocketClient(detectClientId++, e.Worker);
            detectClient.OnConnet += new AsyncSocketConnectEventHandler(OnConnect);
            detectClient.OnReceive += new AsyncSocketReceiveEventHandler(OnReceive);
            detectClient.OnError += new AsyncSocketErrorEventHandler(OnError);
            detectClient.OnSend += new AsyncSocketSendEventHandler(OnSend);
            detectClient.Receive();

            string m_CfgPath = m_Config.GetString("Detector", "CfgPath", "");
            string m_WeightsPath = m_Config.GetString("Detector", "WeightsPath", "");
            string m_NamesPath = m_Config.GetString("Detector", "NamesPath", "");

            IniFile cfgFile = new IniFile(m_CfgPath);

            netWidth = cfgFile.GetInt32("net", "width", 416);
            netHeight = cfgFile.GetInt32("net", "height", 416);
            netChannels = cfgFile.GetInt32("net", "channels", 3);

            string sendStr = "PARAMS," + m_CfgPath + "," + m_WeightsPath + "," + m_NamesPath + "," + Convert.ToString(netWidth) + "," + Convert.ToString(netHeight) + "," + Convert.ToString(netChannels);

            byte[] buf = Encoding.Default.GetBytes(sendStr);
            int len = buf.Length + 4;
            byte[] sendBytes = new byte[len];

            Buffer.BlockCopy(BitConverter.GetBytes(len - 4), 0, sendBytes, 0, 4);
            Buffer.BlockCopy(buf, 0, sendBytes, 4, buf.Length);

            detectClient.Send(sendBytes);

            Console.WriteLine("OnAccept");
        }

        private void OnError(object sender, AsyncSocketErrorEventArgs e)
        {
            Console.WriteLine("OnError : " + e.AsyncSocketException.Message);

            if (detectClient != null)
            {
                detectClient.Close();
                detectClient = null;
            }
        }

        private void OnSend(object sender, AsyncSocketSendEventArgs e)
        {

        }

        private void OnClose(object sender, AsyncSocketConnectionEventArgs e)
        {

        }

        private void OnConnect(object sender, AsyncDetectSocket.AsyncSocketConnectionEventArgs e)
        {

        }

        private void OnReceive(object sender, AsyncDetectSocket.AsyncSocketReceiveEventArgs e)
        {
            byte[] receiveData = e.ReceiveData;

            byte[] bitConvert = new byte[4] { receiveData[0], receiveData[1], receiveData[2], receiveData[3] };

            int len = BitConverter.ToInt32(bitConvert, 0);

            Console.WriteLine("받은 실제 데이터 길이 : " + len);

            byte[] data = new byte[len];

            Buffer.BlockCopy(receiveData, 4, data, 0, len);

            string recvStr = Encoding.Default.GetString(data);

            Console.WriteLine("OnReceive : " + recvStr);

            if (recvStr.StartsWith("Echo,"))
            {

            }
            else
            {
                m_Process_Recv.Add(recvStr);
            }
        }

        public void Start()
        {
            try
            {
                info = new ProcessStartInfo();
                info.FileName = path;
                /*
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.CreateNoWindow = true;
                */
                info.WindowStyle = ProcessWindowStyle.Normal;
                info.CreateNoWindow = false;
                mainProcess = Process.Start(info);
                isProcess = true;
            }
            catch (Exception e)
            {

            }
        }

        public void Close()
        {
            try
            {
                if (mainProcess != null)
                {
                    if (!mainProcess.HasExited)
                    {
                        mainProcess.Kill();
                    }

                    mainProcess.Close();
                    mainProcess.Dispose();
                    mainProcess = null;

                    isProcess = false;
                }
            }
            catch (Exception e)
            {

            }
        }

        public DetectResult Run(Bitmap bitmap, string name, int count)
        {
            List<YoloItem> detectItems = new List<YoloItem>();
            bool m_IsFind = false;
            DetectResult result = new DetectResult();

            if (detectClient != null)
            {
                bool isConnected = detectClient.Connection.Connected;
                bool isAliveSocket = detectClient.IsAliveSocket();
                int _index = -1;

                if (detectClient != null && isAliveSocket)
                {
                    OpenCvSharp.Mat mat = BitmapConverter.ToMat(bitmap);

                    if (netChannels == 1)
                    {
                        if (mat.Channels() == 3)
                        {
                            mat = mat.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                        }
                    }
                    else if (netChannels == 3)
                    {
                        if (mat.Channels() == 1)
                        {
                            mat = mat.CvtColor(OpenCvSharp.ColorConversionCodes.GRAY2BGR);
                        }
                    }

                    int width = mat.Width;
                    int height = mat.Height;
                    int channels = mat.Channels();
                    byte[] output = new byte[width * height * channels];

                    OpenCvSharp.Cv2.ImEncode(".bmp", mat, out output);

                    string sendStr = "IMAGE," + sendImageIndex + ",";
                    _index = sendImageIndex;
                    sendImageIndex++;

                    byte[] buf = Encoding.Default.GetBytes(sendStr);
                    int len = buf.Length + output.Length + 16;
                    byte[] merge = new byte[len];

                    Buffer.BlockCopy(BitConverter.GetBytes(len - 4), 0, merge, 0, 4);
                    Buffer.BlockCopy(buf, 0, merge, 4, buf.Length);
                    Buffer.BlockCopy(BitConverter.GetBytes(height), 0, merge, buf.Length + 4, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(width), 0, merge, buf.Length + 8, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(channels), 0, merge, buf.Length + 12, 4);
                    Buffer.BlockCopy(output, 0, merge, buf.Length + 16, output.Length);

                    detectClient.Send(merge);
                }

                Stopwatch detectSw = new Stopwatch();
                detectSw.Start();

                while (detectSw.ElapsedMilliseconds <= m_Config.GetInt32("Detector", "DetectLimitMilliSeconds", 200))
                {
                    if (m_Process_Recv != null && m_Process_Recv.Count > 0)
                    {
                        for (int i = 0; i < m_Process_Recv.Count; i++)
                        {
                            string data = m_Process_Recv[i];

                            if (data != null)
                            {
                                List<string> list = data.Split('/').ToList();

                                if (list[0] == Convert.ToString(_index))
                                {
                                    list.GetRange(1, list.Count - 1).ForEach(_x =>
                                    {
                                        if (_x != "")
                                        {
                                            string[] arr = _x.Split(',');

                                            YoloItem item = new YoloItem()
                                            {
                                                Type = arr[0],
                                                Confidence = Convert.ToDouble(arr[1]),
                                                X = Convert.ToDouble(arr[2]),
                                                Y = Convert.ToDouble(arr[3]),
                                                Width = Convert.ToDouble(arr[4]),
                                                Height = Convert.ToDouble(arr[5])
                                            };

                                            item.X = item.X - item.Width / 2;
                                            item.Y = item.Y - item.Height / 2;

                                            if (item.Type == name)
                                            {
                                                detectItems.Add(item);
                                            }
                                        }
                                    });

                                    m_Process_Recv.RemoveAt(i);

                                    m_IsFind = true;

                                    // yoloSw.Stop();

                                    // logManager.Trace("인식 소요 시간 : " + yoloSw.ElapsedMilliseconds + "ms");

                                    // logManager.Trace("인식 종료 시간 : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                                    break;
                                }
                            }
                        }
                    }

                    if (m_IsFind)
                    {
                        break;
                    }
                }

                detectSw.Stop();

                m_Process_Recv.Clear();
            }

            result.Items = detectItems;

            if (detectItems.Count >= count)
            {
                result.Result = true;
            }
            else
            {
                result.Result = false;
            }

            /*
            for (int j = 0; j < detectItems.Count; j++)
            {
                YoloItem item = detectItems[j];

                result.Name = item.Type;
                result.X = item.X;
                result.Y = item.Y;
                result.Width = item.Width;
                result.Height = item.Height;
                result.Result = true;
                result.Score = item.Confidence;
            }
            */

            return result;
        }

        public DetectResult Run(BitmapSource bitmapSource, string name, int count)
        {
            List<YoloItem> detectItems = new List<YoloItem>();
            bool m_IsFind = false;
            DetectResult result = new DetectResult();

            if (detectClient != null)
            {
                bool isConnected = detectClient.Connection.Connected;
                bool isAliveSocket = detectClient.IsAliveSocket();
                int _index = -1;

                if (detectClient != null && isAliveSocket)
                {
                    OpenCvSharp.Mat mat = BitmapSourceConverter.ToMat(bitmapSource);

                    if (netChannels == 1)
                    {
                        if (mat.Channels() == 3)
                        {
                            mat = mat.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                        }
                    }
                    else if (netChannels == 3)
                    {
                        if (mat.Channels() == 1)
                        {
                            mat = mat.CvtColor(OpenCvSharp.ColorConversionCodes.GRAY2BGR);
                        }
                    }

                    int width = mat.Width;
                    int height = mat.Height;
                    int channels = mat.Channels();
                    byte[] output = new byte[width * height * channels];

                    OpenCvSharp.Cv2.ImEncode(".bmp", mat, out output);

                    string sendStr = "IMAGE," + sendImageIndex + ",";
                    _index = sendImageIndex;
                    sendImageIndex++;

                    byte[] buf = Encoding.Default.GetBytes(sendStr);
                    int len = buf.Length + output.Length + 16;
                    byte[] merge = new byte[len];

                    Buffer.BlockCopy(BitConverter.GetBytes(len - 4), 0, merge, 0, 4);
                    Buffer.BlockCopy(buf, 0, merge, 4, buf.Length);
                    Buffer.BlockCopy(BitConverter.GetBytes(height), 0, merge, buf.Length + 4, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(width), 0, merge, buf.Length + 8, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(channels), 0, merge, buf.Length + 12, 4);
                    Buffer.BlockCopy(output, 0, merge, buf.Length + 16, output.Length);

                    detectClient.Send(merge);
                }

                Stopwatch detectSw = new Stopwatch();
                detectSw.Start();

                while (detectSw.ElapsedMilliseconds <= m_Config.GetInt32("Detector", "DetectLimitMilliSeconds", 200))
                {
                    if (m_Process_Recv != null && m_Process_Recv.Count > 0)
                    {
                        for (int i = 0; i < m_Process_Recv.Count; i++)
                        {
                            string data = m_Process_Recv[i];

                            if (data != null)
                            {
                                List<string> list = data.Split('/').ToList();

                                if (list[0] == Convert.ToString(_index))
                                {
                                    list.GetRange(1, list.Count - 1).ForEach(_x =>
                                    {
                                        if (_x != "")
                                        {
                                            string[] arr = _x.Split(',');

                                            YoloItem item = new YoloItem()
                                            {
                                                Type = arr[0],
                                                Confidence = Convert.ToDouble(arr[1]),
                                                X = Convert.ToDouble(arr[2]),
                                                Y = Convert.ToDouble(arr[3]),
                                                Width = Convert.ToDouble(arr[4]),
                                                Height = Convert.ToDouble(arr[5])
                                            };

                                            item.X = item.X - item.Width / 2;
                                            item.Y = item.Y - item.Height / 2;

                                            if (item.Type == name)
                                            {
                                                detectItems.Add(item);
                                            }
                                        }
                                    });

                                    m_Process_Recv.RemoveAt(i);

                                    m_IsFind = true;

                                    // yoloSw.Stop();

                                    // logManager.Trace("인식 소요 시간 : " + yoloSw.ElapsedMilliseconds + "ms");

                                    // logManager.Trace("인식 종료 시간 : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                                    break;
                                }
                            }
                        }
                    }

                    if (m_IsFind)
                    {
                        break;
                    }
                }

                detectSw.Stop();

                m_Process_Recv.Clear();
            }

            result.Items = detectItems;

            if (detectItems.Count >= count)
            {
                result.Result = true;
            }
            else
            {
                result.Result = false;
            }

            /*
            for (int j = 0; j < detectItems.Count; j++)
            {
                YoloItem item = detectItems[j];

                result.Name = item.Type;
                result.X = item.X;
                result.Y = item.Y;
                result.Width = item.Width;
                result.Height = item.Height;
                result.Result = true;
                result.Score = item.Confidence;
            }
            */

            return result;
        }

        public DetectResult Run(BitmapSource bitmapSource, string name, int count, int sendIdx)
        {
            List<YoloItem> detectItems = new List<YoloItem>();
            bool m_IsFind = false;
            DetectResult result = new DetectResult();

            if (detectClient != null)
            {
                bool isConnected = detectClient.Connection.Connected;
                bool isAliveSocket = detectClient.IsAliveSocket();
                int _index = -1;

                if (detectClient != null && isAliveSocket)
                {
                    OpenCvSharp.Mat mat = BitmapSourceConverter.ToMat(bitmapSource);

                    if (netChannels == 1)
                    {
                        if (mat.Channels() >= 3)
                        {
                            mat = mat.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                        }
                    }
                    else if (netChannels == 3)
                    {
                        if (mat.Channels() == 1)
                        {
                            mat = mat.CvtColor(OpenCvSharp.ColorConversionCodes.GRAY2BGR);
                        }
                    }

                    int width = mat.Width;
                    int height = mat.Height;
                    int channels = mat.Channels();
                    byte[] output = new byte[width * height * channels];

                    OpenCvSharp.Cv2.ImEncode(".bmp", mat, out output);

                    string sendStr = "IMAGE," + sendIdx + ",";
                    _index = sendIdx;

                    byte[] buf = Encoding.Default.GetBytes(sendStr);
                    int len = buf.Length + output.Length + 16;
                    byte[] merge = new byte[len];

                    Buffer.BlockCopy(BitConverter.GetBytes(len - 4), 0, merge, 0, 4);
                    Buffer.BlockCopy(buf, 0, merge, 4, buf.Length);
                    Buffer.BlockCopy(BitConverter.GetBytes(height), 0, merge, buf.Length + 4, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(width), 0, merge, buf.Length + 8, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(channels), 0, merge, buf.Length + 12, 4);
                    Buffer.BlockCopy(output, 0, merge, buf.Length + 16, output.Length);

                    detectClient.Send(merge);
                }

                Stopwatch detectSw = new Stopwatch();
                detectSw.Start();

                while (detectSw.ElapsedMilliseconds <= m_Config.GetInt32("Detector", "DetectLimitMilliSeconds", 200))
                {
                    if (m_Process_Recv != null && m_Process_Recv.Count > 0)
                    {
                        for (int i = 0; i < m_Process_Recv.Count; i++)
                        {
                            string data = m_Process_Recv[i];

                            if (data != null)
                            {
                                List<string> list = data.Split('/').ToList();

                                if (list[0] == Convert.ToString(_index))
                                {
                                    list.GetRange(1, list.Count - 1).ForEach(_x =>
                                    {
                                        if (_x != "")
                                        {
                                            string[] arr = _x.Split(',');

                                            YoloItem item = new YoloItem()
                                            {
                                                Type = arr[0],
                                                Confidence = Convert.ToDouble(arr[1]),
                                                X = Convert.ToDouble(arr[2]),
                                                Y = Convert.ToDouble(arr[3]),
                                                Width = Convert.ToDouble(arr[4]),
                                                Height = Convert.ToDouble(arr[5])
                                            };

                                            item.X = item.X - item.Width / 2;
                                            item.Y = item.Y - item.Height / 2;

                                            if (item.Type == name)
                                            {
                                                detectItems.Add(item);
                                            }
                                        }
                                    });

                                    m_Process_Recv.RemoveAt(i);

                                    m_IsFind = true;

                                    // yoloSw.Stop();

                                    // logManager.Trace("인식 소요 시간 : " + yoloSw.ElapsedMilliseconds + "ms");

                                    // logManager.Trace("인식 종료 시간 : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                                    break;
                                }
                            }
                        }
                    }

                    if (m_IsFind)
                    {
                        break;
                    }
                }

                detectSw.Stop();

                m_Process_Recv.Clear();
            }

            result.Items = detectItems;

            if (detectItems.Count >= count)
            {
                result.Result = true;
            }
            else
            {
                result.Result = false;
            }

            /*
            for (int j = 0; j < detectItems.Count; j++)
            {
                YoloItem item = detectItems[j];

                result.Name = item.Type;
                result.X = item.X;
                result.Y = item.Y;
                result.Width = item.Width;
                result.Height = item.Height;
                result.Result = true;
                result.Score = item.Confidence;
            }
            */

            return result;
        }
    }
}
