using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CameraManager;
using HCore;
using HInspection;
using HVisionPro;

namespace VisionSystem
{
    public partial class MainEngine : INotifyPropertyChanged
    {
        private InspectionInfo inspectionInfo = null;
        public InspectionInfo InspectionInfo
        {
            get
            {
                return inspectionInfo;
            }
            set
            {
                inspectionInfo = value;
                NotifyPropertyChanged("InspectionInfo");
            }
        }

        private BitmapSource inspectionImage = null;
        public BitmapSource InspectionImage
        {
            get
            {
                return inspectionImage;
            }
            set
            {
                inspectionImage = value;
                NotifyPropertyChanged("InspectionImage");
            }
        }

        private ObservableCollection<HCamera> cameras = null;
        public ObservableCollection<HCamera> Cameras
        {
            get
            {
                return cameras;
            }
            set
            {
                cameras = value;
                NotifyPropertyChanged("Cameras");
            }
        }

        private ObservableCollection<string> cameraSettingCarKindList = null;
        public ObservableCollection<string> CameraSettingCarKindList
        {
            get
            {
                return cameraSettingCarKindList;
            }
            set
            {
                cameraSettingCarKindList = value;
                NotifyPropertyChanged("CameraSettingCarKindList");
            }
        }

        private string selectedSettingWindowCarKindText = null;
        public string SelectedSettingWindowCarKindText
        {
            get
            {
                return selectedSettingWindowCarKindText;
            }
            set
            {
                selectedSettingWindowCarKindText = value;
                NotifyPropertyChanged("SelectedSettingWindowCarKindText");
            }
        }

        private bool selectedSettingWindowCarKindByPass = false;
        public bool SelectedSettingWindowCarKindByPass
        {
            get
            {
                return selectedSettingWindowCarKindByPass;
            }
            set
            {
                selectedSettingWindowCarKindByPass = value;
                NotifyPropertyChanged("SelectedSettingWindowCarKindByPass");
            }
        }

        private bool selectedSettingWindowCarKindByPassCheckBoxEnabled = false;
        public bool SelectedSettingWindowCarKindByPassCheckBoxEnabled
        {
            get
            {
                return selectedSettingWindowCarKindByPassCheckBoxEnabled;
            }
            set
            {
                selectedSettingWindowCarKindByPassCheckBoxEnabled = value;
                NotifyPropertyChanged("SelectedSettingWindowCarKindByPassCheckBoxEnabled");
            }
        }

        private UIElement selectedToolUI = null;
        public UIElement SelectedToolUI
        {
            get
            {
                return selectedToolUI;
            }
            set
            {
                selectedToolUI = value;
                NotifyPropertyChanged("SelectedToolUI");
            }
        }

        private CarKindInfo selectedSettingWindowCarKindInfo = null;
        public CarKindInfo SelectedSettingWindowCarKindInfo
        {
            get
            {
                return selectedSettingWindowCarKindInfo;
            }
            set
            {
                selectedSettingWindowCarKindInfo = value;
                NotifyPropertyChanged("SelectedSettingWindowCarKindInfo");
            }
        }

        private string selectedSettingWindowCarKind = null;
        public string SelectedSettingWindowCarKind
        {
            get
            {
                return selectedSettingWindowCarKind;
            }
            set
            {
                selectedSettingWindowCarKind = value;
                NotifyPropertyChanged("SelectedSettingWindowCarKind");

                // SelectedCarKindInfo = new CarKindInfo(SelectedSettingWindowCarKind);

                if (value != null && value.Trim() != "")
                {
                    /*
                    selectedCarKindInfo.ExposureChanged += delegate
                    {
                        SelectedCameraSettingCamera.Parameters.Exposure = (uint)selectedCarKindInfo.Exposure;
                    };

                    for (int i = 0; i < cameras.Count; i++)
                    {
                        if (selectedCarKindInfo.SerialNum == cameras[i].Info.SerialNumber)
                        {
                            SelectedCameraSettingCamera = cameras[i];
                            SelectedCameraSettingCamera.Parameters.Exposure = (uint)selectedCarKindInfo.Exposure;
                            break;
                        }
                    }
                    */

                    SelectedSettingWindowCarKindInfo = new CarKindInfo(selectedSettingWindowCarKind);
                    string path = Environment.CurrentDirectory + "\\CarKind\\" + value + ".ini";
                    HUtill.IniFile iniFile = new HUtill.IniFile(path);
                    string name = iniFile.GetString("Info", "Name", "");
                    SelectedSettingWindowCarKindText = name;
                    selectedSettingWindowCarKindByPassCheckBoxEnabled = true;
                    SelectedSettingWindowCarKindByPass = iniFile.GetBoolian("Info", "ByPass", false);

                    List<KeyValuePair<string, string>> list = selectedSettingWindowCarKindInfo.Config.GetSectionValuesAsList("Shot");

                    inspectionManager = null;
                    inspectionManager = new HInspection.InspectionManager();

                    for (int i = 0; i < list.Count; i++)
                    {
                        string iniPath = Environment.CurrentDirectory + "\\Camera\\" + list[i].Value + ".ini";
                        // inspectionManager.LoadTools(new HCameraShot(iniPath), null, null);

                        HCore.HInspection info = new HCore.HInspection(iniPath);

                        HUtill.IniFile ini = new HUtill.IniFile(iniPath);

                        inspectionManager.Tools.Add(new HVIDITool(new HCameraShot(iniPath), info, null, ini.GetString("ViDi Runtime", "ToolName", "")));
                    }

                    List<UIElement> userControls = inspectionManager.LoadToolsUI();
                    ToolUIs = new ObservableCollection<UIElement>(userControls);
                }
            }
        }

        private string selectedCameraSettingCarKindText = "";
        public string SelectedCameraSettingCarKindText
        {
            get
            {
                return selectedCameraSettingCarKindText;
            }
            set
            {
                selectedCameraSettingCarKindText = value;
                NotifyPropertyChanged("SelectedCameraSettingCarKindText");
            }
        }

        private CarKindInfo selectedCarKindInfo = null;
        public CarKindInfo SelectedCarKindInfo
        {
            get
            {
                return selectedCarKindInfo;
            }
            set
            {
                selectedCarKindInfo = value;
                NotifyPropertyChanged("SelectedCarKindInfo");
            }
        }

        private CarKindInfo selectedCameraSettingCarKindInfo = null;
        public CarKindInfo SelectedCameraSettingCarKindInfo
        {
            get
            {
                return selectedCameraSettingCarKindInfo;
            }
            set
            {
                selectedCameraSettingCarKindInfo = value;
                NotifyPropertyChanged("SelectedCameraSettingCarKindInfo");
            }
        }

        private HCameraShot selectedCameraSettingCamera = null;
        public HCameraShot SelectedCameraSettingCamera
        {
            get
            {
                return selectedCameraSettingCamera;
            }
            set
            {
                selectedCameraSettingCamera = value;
                NotifyPropertyChanged("SelectedCameraSettingCamera");
            }
        }

        private HCamera selectedCamera = null;
        public HCamera SelectedCamera
        {
            get
            {
                return selectedCamera;
            }
            set
            {
                selectedCamera = value;
            }
        }

        private string selectedCameraSettingCarKind = "";
        public string SelectedCameraSettingCarKind
        {
            get
            {
                return selectedCameraSettingCarKind;
            }
            set
            {
                selectedCameraSettingCarKind = value;
                NotifyPropertyChanged("SelectedCameraSettingCarKind");

                if (value != null && value.Trim() != "")
                {
                    SelectedCameraSettingCarKindInfo = new CarKindInfo(selectedCameraSettingCarKind);
                    string path = Environment.CurrentDirectory + "\\CarKind\\" + value + ".ini";
                    HUtill.IniFile iniFile = new HUtill.IniFile(path);
                    string name = iniFile.GetString("Info", "Name", "");
                    SelectedCameraSettingCarKindText = name;
                }
            }
        }

        private BitmapSource settingWindowInspectionImage = null;
        public BitmapSource SettingWindowInspectionImage
        {
            get
            {
                return settingWindowInspectionImage;
            }
            set
            {
                settingWindowInspectionImage = value;
                NotifyPropertyChanged("SettingWindowInspectionImage");
            }
        }

        private ObservableCollection<string> settingWindowCarKindList = null;
        public ObservableCollection<string> SettingWindowCarKindList
        {
            get
            {
                return settingWindowCarKindList;
            }
            set
            {
                settingWindowCarKindList = value;
                NotifyPropertyChanged("SettingWindowCarKindList");
            }
        }

        private SolidColorBrush input_HeartBeatColor = Brushes.DarkGray;
        public SolidColorBrush Input_HeartBeatColor
        {
            get
            {
                return input_HeartBeatColor;
            }
            set
            {
                input_HeartBeatColor = value;
                NotifyPropertyChanged("Input_HeartBeatColor");
            }
        }

        private SolidColorBrush input_VisionStartColor = Brushes.DarkGray;
        public SolidColorBrush Input_VisionStartColor
        {
            get
            {
                return input_VisionStartColor;
            }
            set
            {
                input_VisionStartColor = value;
                NotifyPropertyChanged("Input_VisionStartColor");
            }
        }

        private SolidColorBrush output_HeartBeatColor = Brushes.DarkGray;
        public SolidColorBrush Output_HeartBeatColor
        {
            get
            {
                return output_HeartBeatColor;
            }
            set
            {
                output_HeartBeatColor = value;
                NotifyPropertyChanged("Output_HeartBeatColor");
            }
        }

        private SolidColorBrush output_ResultColor = Brushes.DarkGray;
        public SolidColorBrush Output_ResultColor
        {
            get
            {
                return output_ResultColor;
            }
            set
            {
                output_ResultColor = value;
                NotifyPropertyChanged("Output_ResultColor");
            }
        }

        private SolidColorBrush output_CompleteColor = Brushes.DarkGray;
        public SolidColorBrush Output_CompleteColor
        {
            get
            {
                return output_CompleteColor;
            }
            set
            {
                output_CompleteColor = value;
                NotifyPropertyChanged("Output_CompleteColor");
            }
        }

        private SolidColorBrush output_Light1Color = Brushes.DarkGray;
        public SolidColorBrush Output_Light1Color
        {
            get
            {
                return output_Light1Color;
            }
            set
            {
                output_Light1Color = value;
                NotifyPropertyChanged("Output_Light1Color");
            }
        }

        private SolidColorBrush output_Light2Color = Brushes.DarkGray;
        public SolidColorBrush Output_Light2Color
        {
            get
            {
                return output_Light2Color;
            }
            set
            {
                output_Light2Color = value;
                NotifyPropertyChanged("Output_Light2Color");
            }
        }

        private SolidColorBrush output_Light3Color = Brushes.DarkGray;
        public SolidColorBrush Output_Light3Color
        {
            get
            {
                return output_Light3Color;
            }
            set
            {
                output_Light3Color = value;
                NotifyPropertyChanged("Output_Light3Color");
            }
        }

        private SolidColorBrush output_Light4Color = Brushes.DarkGray;
        public SolidColorBrush Output_Light4Color
        {
            get
            {
                return output_Light4Color;
            }
            set
            {
                output_Light4Color = value;
                NotifyPropertyChanged("Output_Light4Color");
            }
        }

        private ObservableCollection<UIElement> toolUIs;
        public ObservableCollection<UIElement> ToolUIs { get { return toolUIs; } set { toolUIs = value; NotifyPropertyChanged("ToolUIs"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
