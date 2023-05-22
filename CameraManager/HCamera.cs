using MvCamCtrl.NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace CameraManager
{
    public class HCamera : HCameraBase, INotifyPropertyChanged
    {
        private PixelFormat m_bitmapPixelFormat = PixelFormat.DontCare;
        bool m_bGrabbing = false;
        private IntPtr m_ConvertDstBuf = IntPtr.Zero;
        private UInt32 m_nConvertDstBufLen = 0;
        private Bitmap m_bitmap = null;
        private MyCamera.MV_FRAME_OUT_INFO_EX m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
        private static Object BufForDriverLock = new Object();
        private IntPtr m_BufForDriver = IntPtr.Zero;
        private UInt32 m_nBufSizeForDriver = 0;
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
        private Thread receiveThread = null;

        public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        var handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }

        private bool isUseTestMode = false;
        public bool IsUseTestMode { get { return isUseTestMode; } set { isUseTestMode = value; NotifyPropertyChanged("IsUseTestMode"); } }

        private string testFilePath;
        public string TestFilePath { get { return testFilePath; } set { testFilePath = value; NotifyPropertyChanged("TestFilePath"); } }

        UInt32 driverBufferSize = 3072 * 2048 * 3;
        byte[] driverBuffer = new byte[3072 * 2048 * 3];
        UInt32 saveImageBufferSize = 3072 * 2048 * 3 * 3 + 2048;
        byte[] saveImageBuffer = new byte[3072 * 2048 * 3 * 3 + 2048];

        int connectionLostTimeout;
        public long ConnectionLostTimeout { get { return connectionLostTimeout; } }

        MyCamera.MV_CC_DEVICE_INFO deviceInfo;
        MyCamera.MV_GIGE_DEVICE_INFO gigeInfo;
        IntPtr pDeviceInfo;

        private bool isOpen = false;
        public bool IsOpen 
        { 
            get 
            { 
                return isOpen; 
            } 
            set 
            { 
                if(isOpen == false)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            } 
        }

        public HCamera(string serialNumber)
        {

            Info = new CameraInfo(serialNumber);
            Parameters = new Parameter(this);
        }

        public HCamera(IntPtr pDeviceInfo, int connectionLostTimeout)
        {

            this.pDeviceInfo = pDeviceInfo;
            MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(pDeviceInfo, typeof(MyCamera.MV_CC_DEVICE_INFO));

            IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
            gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));


            Info = new CameraInfo(gigeInfo);
            Parameters = new Parameter(this);

            this.connectionLostTimeout = connectionLostTimeout;
        }

        public CameraInfo Info { get; }
        public new Parameter Parameters { get; }
        
        public void Open()
        {
            if (camera == null)
            {
                camera = new MyCamera();
            }

            if (isOpen == false)
            {
                deviceInfo =
        (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(pDeviceInfo,
                                                      typeof(MyCamera.MV_CC_DEVICE_INFO));
                int result = camera.MV_CC_CreateDevice_NET(ref deviceInfo);
                if (MyCamera.MV_OK != result)
                {
                    throw new Exception("Device Create Fail");
                }

                result = camera.MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != result)
                {

                    Console.WriteLine("Failed To Open Camera");
                    isOpen = false;
                }
                else
                {
                    isOpen = true;
                }

            }
             
        }

        private Int32 NecessaryOperBeforeGrab()
        {
            // ch:取图像宽 | en:Get Iamge Width
            MyCamera.MVCC_INTVALUE_EX stWidth = new MyCamera.MVCC_INTVALUE_EX();
            int nRet = camera.MV_CC_GetIntValueEx_NET("Width", ref stWidth);
            if (MyCamera.MV_OK != nRet)
            {
                throw new Exception("Get Width Info Fail!");
            }
            // ch:取图像高 | en:Get Iamge Height
            MyCamera.MVCC_INTVALUE_EX stHeight = new MyCamera.MVCC_INTVALUE_EX();
            nRet = camera.MV_CC_GetIntValueEx_NET("Height", ref stHeight);
            if (MyCamera.MV_OK != nRet)
            {
                throw new Exception("Get Height Info Fail!");
            }
            // ch:取像素格式 | en:Get Pixel Format
            MyCamera.MVCC_ENUMVALUE stPixelFormat = new MyCamera.MVCC_ENUMVALUE();
            nRet = camera.MV_CC_GetEnumValue_NET("PixelFormat", ref stPixelFormat);
            if (MyCamera.MV_OK != nRet)
            {
                throw new Exception("Get Pixel Format Fail!");
            }

            // ch:设置bitmap像素格式，申请相应大小内存 | en:Set Bitmap Pixel Format, alloc memory
            if ((Int32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined == stPixelFormat.nCurValue)
            {
                throw new Exception("Unknown Pixel Format!");
            }
            else if (IsMono(stPixelFormat.nCurValue))
            {
                m_bitmapPixelFormat = PixelFormat.Format8bppIndexed;

                if (IntPtr.Zero != m_ConvertDstBuf)
                {
                    Marshal.Release(m_ConvertDstBuf);
                    m_ConvertDstBuf = IntPtr.Zero;
                }

                // Mono8为单通道
                m_nConvertDstBufLen = (UInt32)(stWidth.nCurValue * stHeight.nCurValue);
                m_ConvertDstBuf = Marshal.AllocHGlobal((Int32)m_nConvertDstBufLen);
                if (IntPtr.Zero == m_ConvertDstBuf)
                {
                    throw new Exception("Malloc Memory Fail!");
                }
            }
            else
            {
                m_bitmapPixelFormat = PixelFormat.Format24bppRgb;

                if (IntPtr.Zero != m_ConvertDstBuf)
                {
                    Marshal.FreeHGlobal(m_ConvertDstBuf);
                    m_ConvertDstBuf = IntPtr.Zero;
                }

                // RGB为三通道
                m_nConvertDstBufLen = (UInt32)(3 * stWidth.nCurValue * stHeight.nCurValue);
                m_ConvertDstBuf = Marshal.AllocHGlobal((Int32)m_nConvertDstBufLen);
                if (IntPtr.Zero == m_ConvertDstBuf)
                {
                    throw new Exception("Malloc Memory Fail!");
                }
            }

            // 确保释放保存了旧图像数据的bitmap实例，用新图像宽高等信息new一个新的bitmap实例
            if (null != m_bitmap)
            {
                m_bitmap.Dispose();
                m_bitmap = null;
            }
            m_bitmap = new Bitmap((Int32)stWidth.nCurValue, (Int32)stHeight.nCurValue, m_bitmapPixelFormat);

            // ch:Mono8格式，设置为标准调色板 | en:Set Standard Palette in Mono8 Format
            if (PixelFormat.Format8bppIndexed == m_bitmapPixelFormat)
            {
                ColorPalette palette = m_bitmap.Palette;
                for (int i = 0; i < palette.Entries.Length; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                m_bitmap.Palette = palette;
            }

            return MyCamera.MV_OK;
        }

        private Boolean IsMono(UInt32 enPixelType)
        {
            switch (enPixelType)
            {
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono1p:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono2p:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono4p:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8_Signed:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono14:
                case (UInt32)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono16:
                    return true;
                default:
                    return false;
            }
        }

        public void StartGrab(IntPtr display)
        {
            //트리거 모드 변경
            SetEnumValue(HCameraParameterConstant.TRIGGER_MODE, HCameraTriggerModeConstant.OFF);
            //트리거 소스 변경
            SetEnumValue(HCameraParameterConstant.TRIGGER_SOURCE, HCameraTriggerSourceConstant.SOFTWARE);

            // ch:前置配置 | en:pre-operation
            int nRet = NecessaryOperBeforeGrab();
            if (MyCamera.MV_OK != nRet)
            {
                return;
            }

            // ch:标志位置true | en:Set position bit true
            m_bGrabbing = true;

            m_stFrameInfo.nFrameLen = 0;//取流之前先清除帧长度
            m_stFrameInfo.enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;

            receiveThread = new Thread(() => ReceiveThreadDo(display));
            // receiveThread.Start();

            // ch:开始采集 | en:Start Grabbing
            nRet = camera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                m_bGrabbing = false;
                receiveThread.Join();
                throw new Exception("Start Grabbing Fail!");
            }
        }

        public bool Close()
        {
            if(camera == null)
            {
                camera = new MyCamera();
            }
            int nRet;

            nRet = camera.MV_CC_CloseDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            nRet = camera.MV_CC_DestroyDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return false;
            }

            isOpen = false;
            return true;
        }

        public void StopGrab()
        {
            // ch:标志位设为false | en:Set flag bit false
            m_bGrabbing = false;
            receiveThread.Join();

            // ch:停止采集 | en:Stop Grabbing
            int nRet = camera.MV_CC_StopGrabbing_NET();
            if (nRet != MyCamera.MV_OK)
            {
                throw new Exception("Stop Grabbing Fail!");
            }
        }


        private void ReceiveThreadDo(IntPtr display)
        {
            MyCamera.MV_FRAME_OUT stFrameInfo = new MyCamera.MV_FRAME_OUT();
            MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();
            MyCamera.MV_PIXEL_CONVERT_PARAM stConvertInfo = new MyCamera.MV_PIXEL_CONVERT_PARAM();
            int nRet = MyCamera.MV_OK;

            while (m_bGrabbing)
            {
                nRet = camera.MV_CC_GetImageBuffer_NET(ref stFrameInfo, 1000);
                if (nRet == MyCamera.MV_OK)
                {
                    lock (BufForDriverLock)
                    {
                        if (m_BufForDriver == IntPtr.Zero || stFrameInfo.stFrameInfo.nFrameLen > m_nBufSizeForDriver)
                        {
                            if (m_BufForDriver != IntPtr.Zero)
                            {
                                Marshal.Release(m_BufForDriver);
                                m_BufForDriver = IntPtr.Zero;
                            }

                            m_BufForDriver = Marshal.AllocHGlobal((Int32)stFrameInfo.stFrameInfo.nFrameLen);
                            if (m_BufForDriver == IntPtr.Zero)
                            {
                                return;
                            }
                            m_nBufSizeForDriver = stFrameInfo.stFrameInfo.nFrameLen;
                        }

                        m_stFrameInfo = stFrameInfo.stFrameInfo;
                        CopyMemory(m_BufForDriver, stFrameInfo.pBufAddr, stFrameInfo.stFrameInfo.nFrameLen);

                        // ch:转换像素格式 | en:Convert Pixel Format
                        stConvertInfo.nWidth = stFrameInfo.stFrameInfo.nWidth;
                        stConvertInfo.nHeight = stFrameInfo.stFrameInfo.nHeight;
                        stConvertInfo.enSrcPixelType = stFrameInfo.stFrameInfo.enPixelType;
                        stConvertInfo.pSrcData = stFrameInfo.pBufAddr;
                        stConvertInfo.nSrcDataLen = stFrameInfo.stFrameInfo.nFrameLen;
                        stConvertInfo.pDstBuffer = m_ConvertDstBuf;
                        stConvertInfo.nDstBufferSize = m_nConvertDstBufLen;
                        if (PixelFormat.Format8bppIndexed == m_bitmap.PixelFormat)
                        {
                            stConvertInfo.enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                            camera.MV_CC_ConvertPixelType_NET(ref stConvertInfo);
                        }
                        else
                        {
                            stConvertInfo.enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed;
                            camera.MV_CC_ConvertPixelType_NET(ref stConvertInfo);
                        }

                        // ch:保存Bitmap数据 | en:Save Bitmap Data
                        BitmapData bitmapData = m_bitmap.LockBits(new Rectangle(0, 0, stConvertInfo.nWidth, stConvertInfo.nHeight), ImageLockMode.ReadWrite, m_bitmap.PixelFormat);
                        CopyMemory(bitmapData.Scan0, stConvertInfo.pDstBuffer, (UInt32)(bitmapData.Stride * m_bitmap.Height));
                        m_bitmap.UnlockBits(bitmapData);
                    }

                    if (display != null)
                    {
                        stDisplayInfo.hWnd = display;
                        stDisplayInfo.pData = stFrameInfo.pBufAddr;
                        stDisplayInfo.nDataLen = stFrameInfo.stFrameInfo.nFrameLen;
                        stDisplayInfo.nWidth = stFrameInfo.stFrameInfo.nWidth;
                        stDisplayInfo.nHeight = stFrameInfo.stFrameInfo.nHeight;
                        stDisplayInfo.enPixelType = stFrameInfo.stFrameInfo.enPixelType;
                    }

                    /*
                    if (isOneShotCapture || isContinousCapture)
                    {
                        bool isChecked = false;

                        if (isOneShotCapture)
                        {
                            isOneShotCapture = false;
                            isChecked = true;
                        }

                        if (isContinousCapture)
                        {
                            isChecked = true;

                            if (enableCaptureSpeed)
                            {
                                nextContinousDateTime = DateTime.Now;

                                // Console.WriteLine(endContinousDateTime.Subtract(startContinousDateTime).TotalMilliseconds);

                                if (nextContinousDateTime.Subtract(preContinousDateTime).TotalMilliseconds >= captureSpeed)
                                {
                                    isChecked = true;
                                    preContinousDateTime = nextContinousDateTime;
                                }
                                else
                                {
                                    isChecked = false;
                                }
                            }

                            if (enableCaptureInterval)
                            {
                                nextContinousDateTime = DateTime.Now;

                                if (nextContinousDateTime.Subtract(preContinousDateTime).TotalSeconds >= captureInterval)
                                {
                                    isChecked = true;
                                    preContinousDateTime = nextContinousDateTime;
                                }
                                else
                                {
                                    isChecked = false;
                                }
                            }

                            if (useLimitSeconds)
                            {
                                if (DateTime.Now.Subtract(startContinousDateTime).TotalMilliseconds >= (limitSeconds * 1000))
                                {
                                    isChecked = false;
                                    isContinousCapture = false;
                                    useLimitSeconds = false;
                                }
                            }
                        }

                        if (isChecked)
                        {
                            MyCamera.MV_SAVE_IMG_TO_FILE_PARAM stSaveFileParam = new MyCamera.MV_SAVE_IMG_TO_FILE_PARAM();
                            stSaveFileParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Jpeg;
                            stSaveFileParam.enPixelType = m_stFrameInfo.enPixelType;
                            stSaveFileParam.pData = m_BufForDriver;
                            stSaveFileParam.nDataLen = m_stFrameInfo.nFrameLen;
                            stSaveFileParam.nHeight = m_stFrameInfo.nHeight;
                            stSaveFileParam.nWidth = m_stFrameInfo.nWidth;
                            stSaveFileParam.iMethodValue = 0;
                            stSaveFileParam.nQuality = 80;
                            stSaveFileParam.pImagePath = saveDir + "\\" + DateTime.Now.ToString("HH_mm_ss_fff") + ".jpg";
                            nRet = camera.MV_CC_SaveImageToFile_NET(ref stSaveFileParam);

                            if (MyCamera.MV_OK != nRet)
                            {
                                throw new Exception("Save Jpeg Failed!");
                            }
                        }
                    }
                    */

                    camera.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);

                    camera.MV_CC_FreeImageBuffer_NET(ref stFrameInfo);
                }
            }
        }

        public BitmapSource OneShot()
        {

            try
            {
                //트리거 모드 변경
                SetEnumValue(HCameraParameterConstant.TRIGGER_MODE, HCameraTriggerModeConstant.OFF);
                //트리거 소스 변경
                SetEnumValue(HCameraParameterConstant.TRIGGER_SOURCE, HCameraTriggerSourceConstant.SOFTWARE);

                if (camera.MV_CC_StartGrabbing_NET() != MyCamera.MV_OK)
                {
                    throw new Exception("Failed To Start Grapping : " + this.ToString());
                }

                int captureTimeOut = 1000;

                bool result;
                UInt32 nPayloadSize = 0;
                result = GetIntValue("PayloadSize", ref nPayloadSize);
                if (result == false)
                {
                    //실패
                    return null;
                }
                if (nPayloadSize + 2048 > driverBufferSize)
                {
                    driverBufferSize = nPayloadSize + 2048;
                    driverBuffer = new byte[driverBufferSize];

                    saveImageBufferSize = driverBufferSize * 3 + 2048;
                    saveImageBuffer = new byte[saveImageBufferSize];
                }

                IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(driverBuffer, 0);


                MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();
                if (camera.MV_CC_GetOneFrameTimeout_NET(pData, driverBufferSize, ref stFrameInfo, captureTimeOut) != MyCamera.MV_OK)
                {
                    throw new Exception("Failed To Get One Frame : " + this.ToString());
                }

                if (camera.MV_CC_StopGrabbing_NET() != MyCamera.MV_OK)
                {
                    throw new Exception("Failed To Stop Grabbing : " + this.ToString());
                }

                IntPtr pImage = Marshal.UnsafeAddrOfPinnedArrayElement(saveImageBuffer, 0);
                MyCamera.MV_SAVE_IMAGE_PARAM_EX stSaveParam = new MyCamera.MV_SAVE_IMAGE_PARAM_EX();
                stSaveParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Bmp;
                stSaveParam.enPixelType = stFrameInfo.enPixelType;
                stSaveParam.pData = pData;
                stSaveParam.nDataLen = stFrameInfo.nFrameLen;
                stSaveParam.nHeight = stFrameInfo.nHeight;
                stSaveParam.nWidth = stFrameInfo.nWidth;
                stSaveParam.pImageBuffer = pImage;
                stSaveParam.nBufferSize = saveImageBufferSize;
                stSaveParam.nJpgQuality = 80;
                if (camera.MV_CC_SaveImageEx_NET(ref stSaveParam) != MyCamera.MV_OK)
                {
                    throw new Exception("Failed To Save Image Ex : " + this.ToString());
                }

                using (var ms = new System.IO.MemoryStream(saveImageBuffer))
                {
                    ms.Position = 0;
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad; // here
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    ms.Flush();
                    return image;
                }
            }
            catch
            {

            }

            return null;
        }

        public BitmapSource Trig()
        {


            bool result;
            //result = SetEnumValue("TriggerMode", 1);
            //result = SetEnumValue("TriggerSource", 7);

            //if (camera.MV_CC_StartGrabbing_NET() != MyCamera.MV_OK)
            {

            }

            UInt32 nPayloadSize = 0;
            result = GetIntValue("PayloadSize", ref nPayloadSize);
            if (result == false)
            {
                //실패
                return null;
            }
            if (nPayloadSize + 2048 > driverBufferSize)
            {
                driverBufferSize = nPayloadSize + 2048;
                driverBuffer = new byte[driverBufferSize];

                saveImageBufferSize = driverBufferSize * 3 + 2048;
                saveImageBuffer = new byte[saveImageBufferSize];
            }

            CommandExecute("TriggerSoftware");

            IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(driverBuffer, 0);
            UInt32 nDataLen = 0;
            MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

            int nRet = camera.MV_CC_GetOneFrameTimeout_NET(pData, driverBufferSize, ref stFrameInfo, 1000);
            nDataLen = stFrameInfo.nFrameLen;
            if(nRet == MyCamera.MV_OK)
            {
                Console.WriteLine(Info.ModelName + " : suc");
            }
            else
            {
                Console.WriteLine(Info.ModelName +  " : fail");
            }

            //nRet = camera.MV_CC_StopGrabbing_NET();

            //IntPtr pImage = Marshal.UnsafeAddrOfPinnedArrayElement(saveImageBuffer, 0);
            //MyCamera.MV_SAVE_IMAGE_PARAM_EX stSaveParam = new MyCamera.MV_SAVE_IMAGE_PARAM_EX();
            //stSaveParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Bmp;
            //stSaveParam.enPixelType = stFrameInfo.enPixelType;
            //stSaveParam.pData = pData;
            //stSaveParam.nDataLen = stFrameInfo.nFrameLen;
            //stSaveParam.nHeight = stFrameInfo.nHeight;
            //stSaveParam.nWidth = stFrameInfo.nWidth;
            //stSaveParam.pImageBuffer = pImage;
            //stSaveParam.nBufferSize = saveImageBufferSize;
            //stSaveParam.nJpgQuality = 80;
            //nRet = camera.MV_CC_SaveImageEx_NET(ref stSaveParam);
            //if(nRet == MyCamera.MV_OK)
            //{
            //    Console.WriteLine("cap Suc");
            //    using (var ms = new System.IO.MemoryStream(saveImageBuffer))
            //    {
            //        var image = new BitmapImage();
            //        image.BeginInit();
            //        image.CacheOption = BitmapCacheOption.OnLoad; // here
            //        image.StreamSource = ms;
            //        image.EndInit();
            //        return image;
            //    }
            //}
            //else
            //{
            //    Console.WriteLine("cap Fail");
            //}

            return null;

        }

        public new class CameraInfo
        {
            public CameraInfo(string serialNumber)
            {
                this.serialNumber = serialNumber;
            }

            public CameraInfo(MyCamera.MV_GIGE_DEVICE_INFO cameraInfo)
            {

                defaultGateway = GetAddressToString(cameraInfo.nDefultGateWay);
                deviceClass = cameraInfo.chManufacturerName;
                deviceFactory = cameraInfo.chManufacturerSpecificInfo;
                deviceVersion = cameraInfo.chDeviceVersion;
                //friendlyName = cameraInfo["FriendlyName"];
                //fullName = cameraInfo["FullName"];
                interfaceName = GetAddressToString(cameraInfo.nNetExport);

                ipAddress = GetAddressToString(cameraInfo.nCurrentIp);
                address = ipAddress;
                ipConfigCurrent = cameraInfo.nIpCfgCurrent.ToString();
                ipConfigOptions = cameraInfo.nIpCfgOption.ToString();
                //macAddress = cameraInfo["MacAddress"];
                modelName = cameraInfo.chModelName;
                //portNumber = cameraInfo["PortNr"];
                serialNumber = cameraInfo.chSerialNumber;
                subnetMask = GetAddressToString(cameraInfo.nCurrentSubNetMask);
                userDefinedName = cameraInfo.chUserDefinedName;
                venderName = cameraInfo.chManufacturerName;
            }

            private string address;
            public string Address { get { return address; } }

            private string defaultGateway;
            public string DefaultGateway { get { return defaultGateway; } }
            private string deviceClass;
            public string DeviceClass { get { return deviceClass; } }
            private string deviceFactory;
            public string DeviceFactory { get { return deviceFactory; } }
            private string deviceVersion;
            public string DeviceVersion { get { return deviceVersion; } }
            private string friendlyName;
            public string FriendlyName { get { return friendlyName; } }
            private string fullName;
            public string FullName { get { return fullName; } }
            private string interfaceName;
            public string InterfaceName { get { return interfaceName; } }
            private string ipAddress;
            public string IpAddress { get { return ipAddress; } }
            private string ipConfigCurrent;
            public string IpConfigCurrent { get { return ipConfigCurrent; } }
            private string ipConfigOptions;
            public string IpConfigOptions { get { return ipConfigOptions; } }
            private string macAddress;
            public string MacAddress { get { return macAddress; } }
            private string modelName;
            public string ModelName { get { return modelName; } }
            private string portNumber;
            public string PortNumber { get { return portNumber; } }
            private string serialNumber;
            public string SerialNumber { get { return serialNumber; } }
            private string subnetAddress;
            public string SubnetAddress { get { return subnetAddress; } }
            private string subnetMask;
            public string SubnetMask { get { return subnetMask; } }
            private string userDefinedName;
            public string UserDefinedName { get { return userDefinedName; } }
            private string venderName;
            public string VenderName { get { return venderName; } }


            private string GetAddressToString(UInt32 address)
            {
                byte[] bytes = BitConverter.GetBytes(address);
                int ipAddressA = Convert.ToInt32(bytes[3]);
                int ipAddressB = Convert.ToInt32(bytes[2]);
                int ipAddressC = Convert.ToInt32(bytes[1]);
                int ipAddressD = Convert.ToInt32(bytes[0]);
                StringBuilder sb = new StringBuilder();
                sb.Append(ipAddressA);
                sb.Append(".");
                sb.Append(ipAddressB);
                sb.Append(".");
                sb.Append(ipAddressC);
                sb.Append(".");
                sb.Append(ipAddressD);

                return sb.ToString();
            }
        }



        public class Parameter
        {
            HCamera camera;
            public Parameter(HCamera camera)
            {
                this.camera = camera;
            }

            private void TryOpen()
            {
                if(camera.isOpen == false)
                {
                    camera.Open();
                }
            }

            public uint Exposure
            {
                get {
                    Stopwatch sw = new Stopwatch();
                    sw.Start(); 
                    TryOpen();
                    uint exposure = 0; 
                    bool result = camera.GetIntValue(HCameraParameterConstant.EXPOSURE_TIME_RAW, ref exposure); 
                    sw.Stop();

                    if(result == false)
                    {
                        float floatExposure = 0;
                        camera.GetFloatValue(HCameraParameterConstant.EXPOSURE_TIME, ref floatExposure);
                        return (uint)floatExposure;
                    }

                    return exposure;
                }
                set
                {
                    TryOpen();
                    uint exposure = CulcExposure(value);
                    bool result = camera.SetIntValue(HCameraParameterConstant.EXPOSURE_TIME_RAW, exposure);
                    if(result == false)
                    {
                        camera.SetFloatValue(HCameraParameterConstant.EXPOSURE_TIME, exposure);
                    }
                }
            }

            private uint CulcExposure(uint value)
            {
                uint exposure = ((value - ExposureMin) - (value % ExposureInterval)) + ExposureMin;
                return exposure;
            }

            public uint ExposureInterval
            {
                get { TryOpen();
                    uint value = 0;
                    bool result = camera.GetIntIntervalValue(HCameraParameterConstant.EXPOSURE_TIME_RAW, ref value);

                    if(result == false)
                    {
                        return 1;
                    }
                    return value; }
            }

            public uint ExposureMin
            {
                get { TryOpen();
                    uint value = 0; 
                    bool result = camera.GetIntMinValue(HCameraParameterConstant.EXPOSURE_TIME_RAW, ref value);
                    if(result == false)
                    {
                        float floatValue = 0;
                        camera.GetFloatMinValue(HCameraParameterConstant.EXPOSURE_TIME, ref floatValue);
                        return (uint)floatValue;
                    }
                    return value; }
            }

            public uint ExposureMax
            {
                get { 
                    TryOpen();
                    uint value = 0;  
                    bool result = camera.GetIntMaxValue(HCameraParameterConstant.EXPOSURE_TIME_RAW, ref value);

                    if (result == false)
                    {
                        float floatValue = 0;
                        camera.GetFloatMaxValue(HCameraParameterConstant.EXPOSURE_TIME, ref floatValue);
                        return (uint)floatValue;
                    }

                    return value; }
            }
        }
    }
}
