using MvCamCtrl.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CameraManager
{
    
    public class HCameraManager
    {
        int cameraConnectionLostTimeout;
        MyCamera.MV_CC_DEVICE_INFO_LIST deviceList;

        public HCameraManager(int cameraConnectionLostTimeout)
        {
            this.cameraConnectionLostTimeout = cameraConnectionLostTimeout;
            new MyCamera.MV_CC_DEVICE_INFO_LIST();
        }

        public List<HCamera> LoadCameras()
        {
            List<HCamera> cameras = new List<HCamera>();

            MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref deviceList);
            for(int i = 0; i < deviceList.nDeviceNum; i++)
            {
                HCamera camera = new HCamera(deviceList.pDeviceInfo[i], cameraConnectionLostTimeout);
                cameras.Add(camera);
            }

            return cameras;
        }
    }
}
