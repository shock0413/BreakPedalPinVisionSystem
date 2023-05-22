using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HCore;
using HUtill;

namespace VisionSystem
{
    public class CarKindInfo : INotifyPropertyChanged
    {
        public string Name { get; set; }
        private List<HCameraShot> shots = new List<HCameraShot>();
        public List<HCameraShot> Shots
        {
            get
            {
                return shots;
            }
            set
            {
                shots = value;
                NotifyPropertyChanged("Shots");
            }
        }

        public event EventHandler ExposureChanged;
        public class ExposureChangedArgs : EventArgs
        {
            public string Message { get; set; }
        }

        private int oldExposure;
        public int OldExposure
        {
            get
            {
                return oldExposure;
            }
            set
            {
                oldExposure = value;

                isSaved = false;

                NotifyPropertyChanged("OldExposure");
            }
        }

        private int newExposure;
        public int NewExposure
        {
            get
            {
                return newExposure;
            }
            set
            {
                newExposure = value;
                NotifyPropertyChanged("NewExposure");
            }
        }

        private bool isSaved = true;
        public bool IsSaved
        {
            get
            {
                return isSaved;
            }
            set
            {
                isSaved = value;
                NotifyPropertyChanged("IsSaved");
            }
        }

        public int Exposure
        {
            get
            {
                if (isSaved)
                {
                    if (Config == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return Config.GetInt32("Camera", "Exposure", 0);
                    }
                }
                else
                {
                    return newExposure;
                }
            }
            set
            {
                newExposure = value;
                // Config.WriteValue("Camera", "Exposure", value);
                isSaved = false;
                NotifyPropertyChanged("Exposure");

                ExposureChangedArgs args = new ExposureChangedArgs();
                args.Message = "ExposureChanged";
                ExposureChanged(this, args);
            }
        }

        public string DetectClassName
        {
            get
            {
                return Config.GetString("Detect", "DetectClassName", "Pin");
            }
            set
            {
                Config.GetString("Detect", "DetectClassName", value);
            }
        }

        public string ConfigFile { get; set; }
        public IniFile Config { get; set; }

        public bool IsByPass
        {
            get
            {
                return Config.GetBoolian("Info", "ByPass", false);
            }
            set
            {
                Config.WriteValue("Info", "ByPass", value);
            }
        }

        public CarKindInfo()
        {

        }

        public CarKindInfo(string name)
        {
            Name = name;

            string dir = Environment.CurrentDirectory + "\\CarKind";

            if (Directory.Exists(dir))
            {
                string[] files = Directory.GetFiles(dir);

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];

                    if (Name == Path.GetFileNameWithoutExtension(file))
                    {
                        ConfigFile = file;
                        break;
                    }
                }

                if (File.Exists(ConfigFile))
                {
                    Config = new IniFile(ConfigFile);

                    List<KeyValuePair<string, string>> list = Config.GetSectionValuesAsList("Shot");

                    for (int i = 0; i < list.Count; i++)
                    {
                        string path = Environment.CurrentDirectory + "\\Camera\\" + list[i].Value + ".ini";

                        if (File.Exists(path))
                        {
                            HCameraShot shot = new HCameraShot(path);
                            shots.Add(shot);
                        }
                        
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void ExposureSave()
        {
            if (!isSaved)
            {
                Config.WriteValue("Camera", "Exposure", newExposure);
                isSaved = true;
                oldExposure = newExposure;
            }
        }
    }
}
