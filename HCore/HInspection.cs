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
    public class HInspection
    {
        private string name;
        public string Name { get { return name; } }

        IniFile config;

        string path;
        public string IniPath { get { return path; } }

        public HInspection(string iniPath)
        {
            this.path = iniPath;
            name = Path.GetFileNameWithoutExtension(iniPath);

            config = new IniFile(path);

            LoadInfo();
        }

        private void LoadInfo()
        {
            //serialNumber = config.GetString("Info", "Camera Serial Number", "");
        }

        public override string ToString()
        {
            return name;
        }

        public HPart LoadPart(HCameraShot shot, HModel model)
        {
            string partName = config.GetString("Parts " + shot.Name, model.Name, "");
            return new HPart(partName);
        }

        public List<KeyValuePair<string, string>> LoadToolsName(HCameraShot shot, HPart part)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            
            Dictionary<string, string> tools = config.GetSectionValues(shot.Name + " " + part);
            for(int i = 0; i < tools.Count; i++)
            {
                string toolSection = shot.Name + " " + tools.ElementAt(i).Value + " " + part;
                string toolName = config.GetString(toolSection, "Tool", "");

                result.Add(new KeyValuePair<string,string>(toolSection, toolName));
            }

            return result;
        }

    }
}
