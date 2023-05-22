using Cognex.VisionPro;
using Cognex.VisionPro.ID;
using HCore;
using HUtill;
using HVisionPro.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HVisionPro
{
    public class HBarcodeTool : HTool
    {
        private ObservableCollection<HTool> beforeTool = new ObservableCollection<HTool>();
        public ObservableCollection<HTool> BeforeTool { get { return beforeTool; } set { beforeTool = value; NotifyPropertyChanged("BeforeTool"); } }

        IniFile config;

        private HCognexDisplay cogDisplay;
        public HCognexDisplay CogDisplay
        {
            get { return cogDisplay; }
            set { cogDisplay = value; }
        }

        public CogRectangleAffine ROI { get; set; }
        public string ToolName { get; set; }

        private bool isUseMaster = false;
        public bool IsUseMaster { get { return isUseMaster; } set { isUseMaster = value; NotifyPropertyChanged("IsUseMaster"); } }

        private HTool masterTool;
        public HTool MasterTool { get { return masterTool; } set { masterTool = value; NotifyPropertyChanged("MasterTool"); } }

        private string readString;
        public string ReadString { get { return readString; } set { readString = value; NotifyPropertyChanged("ReadString"); } }

        public HBarcodeTool(HCameraShot shot, HInspection inspection, HPart part, string toolName) :
            base(shot, inspection, part, toolName)
        {
            this.ToolName = toolName;
            config = new IniFile(ToolIniPath);
        }

        public override void LoadTool()
        {
            double originX = config.GetDouble("ROI", "OriginX", -1);
            double originY = config.GetDouble("ROI", "OriginY", -1);
            double sideXLength = config.GetDouble("ROI", "SideXLength", -1);
            double sideYLength = config.GetDouble("ROI", "SideYLength", -1);
            double rotation = config.GetDouble("ROI", "Rotation", -1);
            double skew = config.GetDouble("ROI", "Skew", -1);

            ROI = new CogRectangleAffine();
            if (originX != -1)
            {
                ROI.SetOriginLengthsRotationSkew(originX, originY, sideXLength, sideYLength, rotation, skew);
            }

            bool isUseMaster = config.GetBoolian("Master", "Is Use", false);
            string masterToolName = config.GetString("Master", "Tool Name", "");

            IsUseMaster = isUseMaster;
            for (int i = 0; i < beforeTool.Count; i++)
            {
                if (beforeTool[i].ToolName == masterToolName)
                {
                    MasterTool = beforeTool[i];
                    break;
                }
            }
        }

        public override UIElement LoadToolUI()
        {
            HBarcodeToolUI tool = new HBarcodeToolUI(this);

            return tool;
        }

        public override void SetDisplay(object display)
        {
            CogDisplay = (HCognexDisplay)display;
        }


        public void SaveRoi(double originX, double originY, double sideXLength, double sideYLength, double rotation, double skew)
        {
            config.WriteValue("ROI", "OriginX", originX);
            config.WriteValue("ROI", "OriginY", originY);
            config.WriteValue("ROI", "SideXLength", sideXLength);
            config.WriteValue("ROI", "SideYLength", sideYLength);
            config.WriteValue("ROI", "Rotation", rotation);
            config.WriteValue("ROI", "Skew", skew);

            ROI.SetOriginLengthsRotationSkew(originX, originY, sideXLength, sideYLength, rotation, skew);
        }

        public void SaveMaster()
        {

            config.WriteValue("Master", "Is Use", IsUseMaster);
            config.WriteValue("Master", "Tool Name", MasterTool.ToolName);
        }

        public HTool GetMasterTool()
        {
            if (isUseMaster)
            {
                List<HTool> findTools = beforeTool.Where(x => x.ToolName == MasterTool.ToolName).ToList();
                if (findTools.Count > 0)
                {
                    return findTools[0];
                }
            }
            return null;
        }

        public override HResult Run()
        {
            BarcodeResult result = new BarcodeResult();

            if (cogDisplay != null && cogDisplay.Image != null)
            {
                bool isOk = false;
                string readData = "";

                CogImage8Grey image = HImageProcessing.Get8GrayImage(cogDisplay.Image);

                CogIDTool tool = new CogIDTool();
                 
                HTool masterTool = GetMasterTool();
                double masterXBias = 0;
                double masterYBias = 0;
                 

                if (masterTool != null && masterTool.GetType() == typeof(HPatternTool))
                {
                    if (masterTool.LastResult == null)
                    {
                        masterTool.Run();
                    }

                    if (masterTool.LastResult.IsOK == true)
                    {
                        masterXBias = masterTool.LastResult.X;
                        masterYBias = masterTool.LastResult.Y;
                    }
                }

                if (isUseMaster == false || masterTool.LastResult.IsOK == true)
                {
                    CogRectangleAffine roi = new CogRectangleAffine(ROI);
                    roi.CenterX = roi.CenterX + masterXBias;
                    roi.CenterY = roi.CenterY + masterYBias;

                    tool.InputImage = image;
                    tool.Region = roi;
                    tool.RunParams.I2Of5.Enabled = true;

                    cogDisplay.Engine.CogDisplay.StaticGraphics.Add(roi, "Result");

                    tool.Run();
                    if(tool.Results != null)
                    {
                        if (tool.Results.Count > 0)
                        {
                            isOk = true;
                            readData = tool.Results[0].DecodedData.DecodedString;
                            result.X = tool.Results[0].CenterX;
                            result.Y = tool.Results[0].CenterY;
                            result.ReadString = readData;
                        }
                        else
                        {
                            isOk = false;
                        }
                    }
                   else
                    {
                        isOk = false;
                    }

                    ReadString = readData;

                }
                else
                {
                    isOk = false;
                    ReadString = "";
                }

                
                if (isOk)
                {
                    if (IsDrawResultGraphics)
                    {
                        cogDisplay.Engine.CogDisplay.StaticGraphics.Add(tool.Results[0].CreateResultGraphics(CogIDResultGraphicConstants.All), "Result");
                    }

                    result.IsOK = true;
                }
                else
                {
                    result.IsOK = false;
                }
            }

            LastResult = result;

            return result;
        }

        public class BarcodeResult : HResult
        {
            public string ReadString { get; set; }
        }
    }
}
