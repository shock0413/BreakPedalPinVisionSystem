using HUtill;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HCore
{
    public class HCameraShot
    {
        private string name;
        public string Name { get { return name; } }

        private string serialNumber;
        public string SerialNumber { get{ return serialNumber; } }

        private long exposure;
        public long Exposure { get { return exposure; } set { exposure = value; } }

        public int Rotate { get { return config.GetInt32("Camera", "Rotate", 0); } }

        IniFile config;

        string path;
        public string IniPath { get { return path;} }

        public HCameraShot(string iniPath)
        {
            this.path = iniPath;
            name = Path.GetFileNameWithoutExtension(iniPath);

            config = new IniFile(path);

            LoadInfo();
        }

        public string LightAddress
        {
            get
            {
                return config.GetString("PLC", "LIGHT", "");
            }
        }

        public int LightDelay
        {
            get
            {
                return config.GetInt32("PLC", "LIGHT DELAY", 300);
            }
        }

        private void LoadInfo()
        {
            serialNumber = config.GetString("Camera", "SerialNum", "");
            Exposure = config.GetInt32("Camera", "Exposure", 35000);
        }

        public void SaveExposure()
        {
            config.WriteValue("Camera", "Exposure", exposure);
        }

        public override string ToString()
        {
            return name + " (" + SerialNumber + ")";
        }

        public List<HInspection> LoadInspection(HModel model, List<HInspection> allInspections)
        {
            List<HInspection> result = new List<HInspection>();

            Dictionary<string, string> inspections = config.GetSectionValues(model.Name);
            
            for(int i = 0; i < inspections.Count; i++)
            {
                string index = inspections.ElementAt(i).Key;
                string shotName = inspections.ElementAt(i).Value;

                List<HInspection> findInspection = allInspections.Where(x => x.Name == shotName).ToList();
                if (findInspection.Count > 0)
                {
                    HInspection inspection = new HInspection(findInspection[0].IniPath); ;
                    result.Add(inspection);
                }
            }
            
            return result;
        }
        public string GetMasterImagePath(HModel model)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Camera\\Master Image\\" + model.Name + " " + name + ".jpg";
            return path;
        }
    }
}
