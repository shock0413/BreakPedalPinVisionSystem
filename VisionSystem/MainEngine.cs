using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// using DetectorManager;
using Hansero;
using HPLCManager;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using CameraManager;
using System.Collections.ObjectModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using HAsyncSocket;
using HVisionPro;
using HInspection;
using Cognex.VisionPro;
using HCore;
using System.Diagnostics;
using System.Windows.Shapes;
using HUtill;
using System.Windows.Controls;

namespace VisionSystem
{
    public partial class MainEngine
    {
        private HMelsecManager plcManager;
        private bool isRunning = false;
        private LogManager logManager;
        private IniManager iniManager = null;
        private bool plcState = false;
        private Thread plcThread = null;
        private Thread plcHeartBeatThread = null;
        private MetroWindow window = null;
        private bool isOutputHeartBeat = false;
        // private DetectorManager.DetectorManager detectorManager = null;
        private HCameraManager cameraManager = null;
        // private HanseroDisplay.HDisplay cameraSettingDisplay = null;
        // private HanseroDisplay.HDisplay settingWindowInspectionDisplay = null;
        private HCognexDisplay settingWindowInspectionDisplay = null;
        private HCognexDisplay cameraSettingDisplay = null;
        private InspectionManager inspectionManager = null;

        public MainEngine(MetroWindow window)
        {
            this.window = window;
            logManager = new LogManager(true, true);
            iniManager = new IniManager();

            isRunning = true;

            InitViDiEngine();

            InitCamera();

            InitPLC();

            // InitInspection();

            // YOLO 프로그램 실행
            // InitDetector();
        }

        private void InitInspection()
        {
            inspectionManager = new InspectionManager();
        }

        private Process ViDiEngineProcess = null;

        private void InitViDiEngine()
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Environment.CurrentDirectory + "\\ViDiEngine\\HVidiEngine.exe";
            /*
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.CreateNoWindow = true;
            */
            info.WindowStyle = ProcessWindowStyle.Normal;
            info.CreateNoWindow = false;
            ViDiEngineProcess = Process.Start(info);
        }

        private void InitPLC()
        {
            new Thread(new ThreadStart(() =>
            {
                while (isRunning)
                {
                    try
                    {
                        if (!plcState)
                        {
                            logManager.Trace("Init Plc");

                            if (plcManager != null)
                            {
                                plcManager.Close();
                            }

                            plcManager = new HMelsecManager(iniManager.LogicalStationNumber);
                            logManager.Trace("Try to Open Plc");

                            if (plcManager.Open())
                            {
                                logManager.Trace("Start Plc Tick Thread");

                                StartPlcThread();
                                plcState = true;
                            }
                            else
                            {
                                logManager.Fatal("Failed To Open Plc");
                                plcState = false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logManager.Error("PLC 접속 실패 : " + e.Message);
                        plcState = false;
                    }

                    try
                    {
                        Thread.Sleep(100);
                    }
                    catch
                    {

                    }
                }
            })).Start();

            StartPlcHeartBeatThread();
        }

        private void InitCamera()
        {
            cameraManager = new HCameraManager(500);
            Cameras = new ObservableCollection<HCamera>(cameraManager.LoadCameras());
        }

        private void InitDetector()
        {
            // detectorManager = new DetectorManager.DetectorManager();
            // detectorManager.Start();
        }

        private void StartPlcThread()
        {
            plcThread = new Thread(new ThreadStart(PlcThreadDo));
            plcThread.Start();
        }

        private void PlcThreadDo()
        {
            while (isRunning)
            {
                try
                {
                    int tickInterval = iniManager.ReadTickInterval;
                    string readAddr = iniManager.ReadAddr;
                    int readSize = iniManager.ReadSize;

                    // Read Address
                    string inputAddr_HeartBeat = iniManager.ReadAddrHeartBeat;
                    string inputAddr_Kind = iniManager.ReadAddrKind;
                    string inputAddr_Seq = iniManager.ReadAddrSeq;
                    string inputAddr_BodyNumber = iniManager.ReadAddrBodyNumber;
                    string inputAddr_VisionStart = iniManager.ReadAddrVisionStart;

                    int inputPos_HeartBeat = Convert.ToInt32(inputAddr_HeartBeat.Replace("D", ""));
                    int inputPos_Kind = Convert.ToInt32(inputAddr_Kind.Replace("D", ""));
                    int inputPos_Seq = Convert.ToInt32(inputAddr_Seq.Replace("D", ""));
                    int inputPos_BodyNumber = Convert.ToInt32(inputAddr_BodyNumber.Replace("D", ""));
                    int inputPos_VisionStart = Convert.ToInt32(inputAddr_VisionStart.Replace("D", ""));

                    int inputSize_HeartBeat = iniManager.ReadSizeHeartBeat;
                    int inputSize_Kind = iniManager.ReadSizeKind;
                    int inputSize_Seq = iniManager.ReadSizeSeq;
                    int inputSize_BodyNumber = iniManager.ReadSizeBodyNumber;
                    int inputSize_VisionStart = iniManager.ReadSizeVisionStart;

                    int startPos = Convert.ToInt32(readAddr.Replace("D", ""));
                    inputPos_HeartBeat -= startPos;
                    inputPos_Kind -= startPos;
                    inputPos_Seq -= startPos;
                    inputPos_BodyNumber -= startPos;
                    inputPos_VisionStart -= startPos;

                    int[] readData = new int[readSize];

                    while (isRunning)
                    {
                        try
                        {
                            if (plcManager.ReadBlock(readAddr, readSize, out readData) == false)
                            {
                                plcState = false;

                                continue;
                            }

                            int input_HeartBeat = readData[inputPos_HeartBeat];

                            int input_Kind = readData[inputPos_Kind];
                            /*
                            int[] input_Kind_Read_Data = new int[inputSize_Kind];
                            Array.Copy(readData, inputPos_Kind, input_Kind_Read_Data, 0, inputSize_Kind);
                            string input_Kind = string.Join("", ConvertIntToString(input_Kind_Read_Data));
                            */

                            /*
                            // int input_Seq = readData[inputPos_Seq];
                            int[] input_Seq_Read_Data = new int[inputSize_Seq];
                            Array.Copy(readData, inputPos_Seq, input_Seq_Read_Data, 0, inputSize_Seq);
                            string input_Seq = string.Join("", ConvertIntToString(input_Seq_Read_Data));

                            // int input_BodyNumber = readData[inputPos_BodyNumber];
                            int[] input_BodyNumber_Read_Data = new int[inputSize_BodyNumber];
                            Array.Copy(readData, inputPos_BodyNumber, input_BodyNumber_Read_Data, 0, inputSize_BodyNumber);
                            string input_BodyNumber = string.Join("", ConvertIntToString(input_BodyNumber_Read_Data));
                            */

                            int input_VisionStart = readData[inputPos_VisionStart];
                            
                            if (input_HeartBeat == 0x01)
                            {
                                window.Dispatcher.Invoke(() =>
                                {
                                    Input_HeartBeatColor = Brushes.LimeGreen;
                                });
                            }
                            else
                            {
                                window.Dispatcher.Invoke(() =>
                                {
                                    Input_HeartBeatColor = Brushes.DarkGray;
                                });
                            }

                            if (input_VisionStart == 0x01)
                            {
                                window.Dispatcher.Invoke(() =>
                                {
                                    Input_VisionStartColor = Brushes.LimeGreen;
                                });

                                /*
                                input_Kind = "LX2 LH";
                                input_BodyNumber = "0A102385";
                                input_Seq = "0048";
                                */

                                string path = Environment.CurrentDirectory + "\\CarKind\\" + input_Kind + ".ini";
                                IniFile iniFile = new IniFile(path);

                                InspectionInfo = new InspectionInfo(iniFile.GetString("Info", "Name", ""));
                                // CarKindInfo carKindInfo = new CarKindInfo();
                                InspectionInfo.CarKindInfo = new CarKindInfo(System.IO.Path.GetFileNameWithoutExtension(path));

                                // BitmapSource bitmapSource = NewImageCapture(Cameras.Where(x => x.Info.SerialNumber == carKindInfo.SerialNum).ToArray()[0]);

                                bool[] results = new bool[inspectionInfo.CarKindInfo.Shots.Count];
                                string dir_temp = "";

                                for (int i = 0; i < inspectionInfo.CarKindInfo.Shots.Count; i++)
                                {
                                    BitmapSource bitmapSource = NewImageCapture(inspectionInfo.CarKindInfo.Shots[i]);

                                    bool result = DoInspection(inspectionInfo, bitmapSource, inspectionInfo.CarKindInfo.Shots[i]);

                                    if (result)
                                    {
                                        results[i] = true;
                                    }
                                    else
                                    {
                                        results[i] = false;
                                    }

                                    dir_temp = SaveResultImage(InspectionInfo, display, inspectionInfo.CarKindInfo.Shots[i], result);
                                }

                                /*
                                if (bitmapSource != null)
                                {
                                    bitmapSource = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Test.jpg", UriKind.RelativeOrAbsolute));
                                }
                                */

                                bool total_result = results.Where(x => x == false).Count() > 0 ? false : true;

                                window.Dispatcher.Invoke(() =>
                                {
                                    if (inspectionInfo.CarKindInfo.IsByPass)
                                    {
                                        total_result = true;
                                    }

                                    if (total_result)
                                    {
                                        InspectionInfo.Result = "OK";
                                        plcManager.WriteBlock(iniManager.WriteAddrResult, 1, new int[] { 1 });

                                        window.Dispatcher.Invoke(() =>
                                        {
                                            Output_ResultColor = Brushes.LimeGreen;
                                        });

                                        string dir_tmp = dir_temp + "OK";
                                        Directory.Move(dir_temp, dir_tmp);
                                    }
                                    else
                                    {
                                        InspectionInfo.Result = "NG";
                                        plcManager.WriteBlock(iniManager.WriteAddrResult, 1, new int[] { 2 });

                                        window.Dispatcher.Invoke(() =>
                                        {
                                            Output_ResultColor = Brushes.Red;
                                        });

                                        string dir_tmp = dir_temp + "NG";
                                        Directory.Move(dir_temp, dir_tmp);
                                    }
                                });

                                plcManager.WriteBlock(iniManager.WriteAddrComplete, 1, new int[] { 1 });

                                window.Dispatcher.Invoke(() =>
                                {
                                    Output_CompleteColor = Brushes.LimeGreen;
                                });

                                Thread.Sleep(500);

                                plcManager.WriteBlock(iniManager.WriteAddrResult, 1, new int[] { 0 });
                                plcManager.WriteBlock(iniManager.WriteAddrComplete, 1, new int[] { 0 });

                                window.Dispatcher.Invoke(() =>
                                {
                                    Output_ResultColor = Brushes.DarkGray;
                                    Output_CompleteColor = Brushes.DarkGray;
                                });
                            }
                            else
                            {
                                window.Dispatcher.Invoke(() =>
                                {
                                    Input_VisionStartColor = Brushes.DarkGray;
                                });
                            }

                            plcState = true;
                        }
                        catch (Exception e)
                        {
                            logManager.Error("PLC 통신 이상 : " + e.Message);
                            plcState = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    
                }
            }
        }

        private string SaveResultImage(InspectionInfo info, HCognexDisplay display, HCameraShot shot, bool result)
        {
            string path = iniManager.ResultPath;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path += "\\Image";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path += "\\" + info.DateTime.ToString("yyyy-MM-dd");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path += "\\" + info.DateTime.ToString("HHmmss") + "_" + info.Seq + "_" + info.BodyNumber + "_" + info.Kind + "_";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string temp = path;

            if (result)
            {
                path += "\\" + DateTime.Now.ToString("HHmmss") + "_" + shot.Name + "_OK.bmp";
            }
            else
            {
                path += "\\" + DateTime.Now.ToString("HHmmss") + "_" + shot.Name + "_NG.bmp";
            }

            // display.SaveJpegImage(path);
            display.SaveImage(path);

            return temp;
        }

        private void StartPlcHeartBeatThread()
        {
            plcHeartBeatThread = new Thread(new ThreadStart(StartPlcHeartBeatThreadDo));
            plcHeartBeatThread.Start();
        }

        private void StartPlcHeartBeatThreadDo()
        {
            while (isRunning)
            {
                try
                {
                    if (plcState)
                    {
                        string outputAddr_HeartBeat = iniManager.WriteAddrHeartBeat;

                        if (isOutputHeartBeat)
                        {
                            if (plcManager.WriteBlock(outputAddr_HeartBeat, 1, new int[] { 0x00 }))
                            {
                                window.Dispatcher.Invoke(() =>
                                {
                                    Output_HeartBeatColor = Brushes.DarkGray;
                                });

                                isOutputHeartBeat = false;
                            }
                        }
                        else
                        {
                            if (plcManager.WriteBlock(outputAddr_HeartBeat, 1, new int[] { 0x01 }))
                            {
                                window.Dispatcher.Invoke(() =>
                                {
                                    Output_HeartBeatColor = Brushes.LimeGreen;
                                });

                                isOutputHeartBeat = true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {

                }

                try
                {
                    int interval = iniManager.ReadTickInterval;
                    Thread.Sleep(interval);
                }
                catch
                {

                }
            }
        }

        public void CameraSettingButton_Click(object sender, RoutedEventArgs e)
        {
            CameraSettingCarKindList = LoadCameraSettingCarKind();
            SelectedCameraSettingCamera = null;
            SelectedCameraSettingCarKindInfo = null;
            SelectedCameraSettingCarKindText = null;

            CameraSettingWindow cameraSettingWindow = new CameraSettingWindow();
            cameraSettingWindow.DataContext = this;
            SelectedCameraSettingCarKind = "";
            cameraSettingWindow.ShowDialog();
        }

        private ObservableCollection<string> LoadCameraSettingCarKind()
        {
            ObservableCollection<string> list = new ObservableCollection<string>();

            string dir = Environment.CurrentDirectory + "\\CarKind";
            string[] files = Directory.GetFiles(dir);

            foreach (string file in files)
            {
                if (System.IO.Path.GetExtension(file) == ".ini")
                {
                    list.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }

            return list;
        }

        private ObservableCollection<string> LoadSettingWindowCarKind()
        {
            ObservableCollection<string> list = new ObservableCollection<string>();

            string dir = Environment.CurrentDirectory + "\\CarKind";
            string[] files = Directory.GetFiles(dir);

            foreach (string file in files)
            {
                if (System.IO.Path.GetExtension(file) == ".ini")
                {
                    list.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }

            return list;
        }

        public void CameraSettingExposureSlide_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (selectedCameraSettingCamera != null)
            {
                Slider slider = (Slider)sender;
                SelectedCameraSettingCamera.Exposure = Convert.ToInt64(slider.Value);
            }
        }

        public void CameraSettingDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            cameraSettingDisplay = (HCognexDisplay)sender;
        }

        public void CameraSettingOneShotButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCameraSettingCamera != null)
            {
                BitmapSource bs = NewImageCapture(selectedCameraSettingCamera);
                cameraSettingDisplay.LoadImage(bs);
            }
        }

        private BitmapSource NewImageCapture(HCameraShot shot)
        {
            // plcManager.WriteBlock(inspectionInfo.CarKindInfo.LightPLCAddress, 1, new int[] { 1 });
            plcManager.WriteBlock(shot.LightAddress, 1, new int[] { 1 });
            logManager.Trace(shot.LightAddress + " 조명 ON");

            window.Dispatcher.Invoke(() =>
            {
                if (shot.LightAddress == iniManager.WriteAddrLight1)
                {
                    Output_Light1Color = Brushes.LimeGreen;
                }
                else if (shot.LightAddress == iniManager.WriteAddrLight2)
                {
                    Output_Light2Color = Brushes.LimeGreen;
                }
                else if (shot.LightAddress == iniManager.WriteAddrLight3)
                {
                    Output_Light3Color = Brushes.LimeGreen;
                }
                else if (shot.LightAddress == iniManager.WriteAddrLight4)
                {
                    Output_Light4Color = Brushes.LimeGreen;
                }
            });

            
            Thread.Sleep(shot.LightDelay);

            HCamera camera = Cameras.Where(x => x.Info.SerialNumber == shot.SerialNumber).ToArray()[0];
            camera.Parameters.Exposure = Convert.ToUInt32(shot.Exposure);

            camera.Open();

            BitmapSource bitmapSource = null;

            for (int i = 0; i < 5; i++)
            {
                logManager.Trace((i + 1) + "번째 촬영 시작");

                bitmapSource = camera.OneShot();

                if (bitmapSource != null)
                {
                    logManager.Trace((i + 1) + "번째 촬영 이미지 획득 성공");
                    break;
                }
            }

            camera.Close();

            // plcManager.WriteBlock(inspectionInfo.CarKindInfo.LightPLCAddress, 1, new int[] { 0 });
            plcManager.WriteBlock(shot.LightAddress, 1, new int[] { 0 });
            logManager.Trace(shot.LightAddress + " 조명 OFF");

            window.Dispatcher.Invoke(() =>
            {
                if (shot.LightAddress == iniManager.WriteAddrLight1)
                {
                    Output_Light1Color = Brushes.DarkGray;
                }
                else if (shot.LightAddress == iniManager.WriteAddrLight2)
                {
                    Output_Light2Color = Brushes.DarkGray;
                }
                else if (shot.LightAddress == iniManager.WriteAddrLight3)
                {
                    Output_Light3Color = Brushes.DarkGray;
                }
                else if (shot.LightAddress == iniManager.WriteAddrLight4)
                {
                    Output_Light4Color = Brushes.DarkGray;
                }
            });

            return bitmapSource;
        }

        private SettingWindow settingWindow = null;

        public void SettingWindowButton_Click(object sender, RoutedEventArgs e)
        {
            SettingWindowCarKindList = LoadSettingWindowCarKind();
            SelectedSettingWindowCarKindByPass = false;
            SelectedSettingWindowCarKindByPassCheckBoxEnabled = false;
            SelectedSettingWindowCarKind = null;
            SelectedSettingWindowCarKindInfo = null;
            SelectedSettingWindowCarKindText = null;
            ToolUIs = null;

            settingWindow = new SettingWindow();
            SettingWindowInspectionImage = null;
            settingWindow.DataContext = this;
            Application.Current.MainWindow = settingWindow;
            settingWindow.ShowDialog();
        }

        public void Window_Closing(object sender, CancelEventArgs e)
        {
            MetroDialogSettings settings = new MetroDialogSettings();
            settings.AffirmativeButtonText = "확인";
            settings.NegativeButtonText = "취소";

            if (window.ShowModalMessageExternal("확인", "정말로 종료하시겠습니까?", MessageDialogStyle.AffirmativeAndNegative, settings) == MessageDialogResult.Affirmative)
            {
                isRunning = false;

                // detectorManager.Close();

                if (ViDiEngineProcess != null)
                {
                    if (!ViDiEngineProcess.HasExited)
                    {
                        ViDiEngineProcess.Kill();
                    }

                    ViDiEngineProcess.Close();
                    ViDiEngineProcess.Dispose();
                    ViDiEngineProcess = null;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private string[] ConvertIntToString(int[] Input)
        {
            //0~7은 데이터가 없고 8~15엔 데이터가 있는경우는 상정안됨. 하지만 될것 같음.

            List<string> ConvString = new List<string>();

            foreach (int data in Input)
            {
                string bianryData = Convert.ToString(data, 2).PadLeft(16, '0');

                string Bit_firstValue = bianryData.Substring(8);
                string Bit_lastValue = bianryData.Substring(0, 8);

                int DEC_firstValue = Convert.ToInt32(Bit_firstValue, 2);
                int DEC_lastValue = Convert.ToInt32(Bit_lastValue, 2);

                string ASCII_firstValue = "";
                if (DEC_firstValue != 0)
                {
                    ASCII_firstValue = Encoding.ASCII.GetString(new byte[] { (byte)DEC_firstValue });
                }

                string ASCII_lastValue = "";
                if (DEC_lastValue != 0)
                {
                    ASCII_lastValue = Encoding.ASCII.GetString(new byte[] { (byte)DEC_lastValue });
                }

                ConvString.Add(ASCII_firstValue + ASCII_lastValue);
            }

            return ConvString.ToArray();
        }

        private bool DoInspection(InspectionInfo info, BitmapSource bitmapSource, HCameraShot shot)
        {
            try
            {
                window.Dispatcher.Invoke(() =>
                {
                    display.Clear();
                    display.ClearImage();
                });

                window.Dispatcher.Invoke(() =>
                {
                    display.LoadImage(bitmapSource);
                });

                HCore.HInspection inspectionInfo = new HCore.HInspection(shot.IniPath);
                HVIDITool tool = new HVIDITool(new HCameraShot(shot.IniPath), inspectionInfo, null, "default");

                window.Dispatcher.Invoke(() =>
                {
                    tool.CogDisplay = display;
                });

                tool.LoadTool();
                tool.LoadRegex();
                
                HResult result = tool.Run();

                return result.IsOK;
            }
            catch (Exception e)
            {
                logManager.Error("DoInspection 에러 : " + e.Message);

                return false;
            }
        }

        private HCognexDisplay display;

        public void HDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            display = (HCognexDisplay)sender;
        }

        public void CameraSettingSaveButton_Click(object sender, RoutedEventArgs e)
        {
            selectedCameraSettingCamera.SaveExposure();
        }

        public void SettingWindowImageLoadButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog();
            commonOpenFileDialog.Filters.Add(new CommonFileDialogFilter("이미지 파일", "*jpeg.;*.jpg;*.bmp;*.png"));
            commonOpenFileDialog.Filters.Add(new CommonFileDialogFilter("모든 파일", "*.*"));
            commonOpenFileDialog.Multiselect = false;

            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                window.Dispatcher.Invoke(() =>
                {
                    // settingWindowInspectionDisplay.BitmapImage = null;
                });

                string fileName = commonOpenFileDialog.FileName;
                // settingWindowInspectionDisplay.BitmapImage = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));
                // settingWindowInspectionDisplay.Clear();

                window.Dispatcher.Invoke(() =>
                {
                    // settingWindowInspectionDisplay.RemoveSelectRectangle();
                    settingWindowInspectionDisplay.LoadImage(fileName);
                    settingWindowInspectionDisplay.Clear();
                });
            }
        }

        public void SettingWindowInspectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (settingWindowInspectionDisplay.Image != null && selectedToolUI != null)
            {
                HVisionPro.UI.HVidiToolUI ui = (HVisionPro.UI.HVidiToolUI)selectedToolUI;
                List<HTool> tools = inspectionManager.Tools.Where(x => ((HVIDITool)x).ToolName == ui.Header.ToString()).ToList();

                if (tools.Count > 0)
                {
                    HVIDITool tool = (HVIDITool)tools[tools.Count - 1];

                    /*
                    HCore.HInspection inspectionInfo = new HCore.HInspection(shot.IniPath);
                    HVIDITool tool = new HVIDITool(new HCameraShot(shot.IniPath), inspectionInfo, null, "default");
                    */

                    window.Dispatcher.Invoke(() =>
                    {
                        tool.CogDisplay = settingWindowInspectionDisplay;
                    });

                    tool.LoadTool();
                    tool.LoadRegex();

                    HResult result = tool.Run();
                }
            }
        }

        public void SettingWindowInspectionDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            // settingWindowInspectionDisplay = (HanseroDisplay.HDisplay)sender;
            settingWindowInspectionDisplay = (HCognexDisplay)sender;
        }
    }
}
