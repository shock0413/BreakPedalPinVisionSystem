using HUtill;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCore
{
    public class HModel
    {
        private string name;
        public string Name { get { return name; } }
        string path;

        IniFile config;

        public HModel(string iniPath)
        {
            this.path = iniPath;
            name = Path.GetFileNameWithoutExtension(iniPath);

            config = new IniFile(path);
        }

        public override string ToString()
        {
            return name;
        }

        public List<HCameraShot> LoadCameras(List<HCameraShot> allCameras)
        {
            List<HCameraShot> result = new List<HCameraShot>();
            Dictionary<string, string> modelCameras = config.GetSectionValues("Shot");
            for(int i = 0; i < modelCameras.Count; i++)
            {
                string index = modelCameras.ElementAt(i).Key;
                string shotName = modelCameras.ElementAt(i).Value;

                List<HCameraShot> findCamera= allCameras.Where(x => x.Name == shotName).ToList();
                if(findCamera.Count > 0)
                {
                    HCameraShot cameraShot = new HCameraShot(findCamera[0].IniPath);
                    result.Add(cameraShot);
                }

            }

            return result;
        }
    }
}
