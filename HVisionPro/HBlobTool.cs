using Cognex.VisionPro;
using Cognex.VisionPro.Blob;
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
using static HVisionPro.HBarcodeTool;

namespace HVisionPro
{
    public class HBlobTool : HTool
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

        private int threshold;
        public int Threshold { get { return threshold; } set { threshold = value; NotifyPropertyChanged("Threshold"); } }


        private int areaMin;
        public int AreaMin { get { return areaMin; } set { areaMin = value; NotifyPropertyChanged("AreaMin"); } }

        private int areaMax;
        public int AreaMax { get { return areaMax; } set { areaMax = value; NotifyPropertyChanged("AreaMax"); } }

        private int findArea;
        public int FindArea { get { return findArea; } set { findArea = value; NotifyPropertyChanged("FindArea"); } }

        public HBlobTool(HCameraShot shot, HInspection inspection, HPart part, string toolName) :
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

            LoadBlobThreshold();
            LoadAreaMinThreshold();
            LoadAreaMaxThreshold();
        }

        public void LoadBlobThreshold()
        {
            Threshold = config.GetInt32("Info", "Threshold", 100);
        }

        public void SaveBlobThreshold(int value)
        {
            config.WriteValue("Info", "Threshold", value);
        }

        public void LoadAreaMinThreshold()
        {
            areaMin = config.GetInt32("Info", "Area Min", 0);
        }

        public void SaveAreaMinThreshold(int value)
        {
            config.WriteValue("Info", "Area Min", value);
        }

        public void LoadAreaMaxThreshold()
        {
            areaMax = config.GetInt32("Info", "Area Max", 1000000);
        }

        public void SaveAreaMaxThreshold(int value)
        {
            config.WriteValue("Info", "Area Max", value);
        }

        public override UIElement LoadToolUI()
        {
            HBlobToolUI tool = new HBlobToolUI(this);

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
            if (isUseMaster && MasterTool != null)
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
            BlobResult result = new BlobResult();
            CogBlobTool tool = new CogBlobTool();

            if (cogDisplay != null && cogDisplay.Image != null)
            {
                bool isOk = false;
                CogImage8Grey image = HImageProcessing.Get8GrayImage(cogDisplay.Image);
                 

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

                if (isUseMaster == false || (MasterTool != null && masterTool.LastResult.IsOK == true))
                {
                    CogRectangleAffine roi = new CogRectangleAffine(ROI);
                    roi.CenterX = roi.CenterX + masterXBias;
                    roi.CenterY = roi.CenterY + masterYBias;

                     
                    tool.InputImage = image;
                    tool.Region = roi;
                    tool.RunParams.SegmentationParams.Mode = CogBlobSegmentationModeConstants.HardFixedThreshold;
                    tool.RunParams.SegmentationParams.HardFixedThreshold = Threshold;
                    tool.RunParams.SegmentationParams.Polarity = CogBlobSegmentationPolarityConstants.LightBlobs;


                   cogDisplay.Engine.CogDisplay.StaticGraphics.Add(roi, "Result");



                    tool.Run();
                    if (tool.Results != null)
                    {
                        if (tool.Results.GetBlobs().Count > 0)
                        {
                          

                            result.X = tool.Results.GetBlobs()[0].CenterOfMassX;
                            result.Y = tool.Results.GetBlobs()[0].CenterOfMassY;

                            FindArea = (int)tool.Results.GetBlobs()[0].Area;
                            if(findArea > areaMin && findArea <AreaMax)
                            {
                                isOk = true;
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


                if (isOk)
                {
                    if (IsDrawResultGraphics)
                    {
                        cogDisplay.Engine.CogDisplay.StaticGraphics.Add(tool.Results.GetBlobs()[0].CreateResultGraphics(CogBlobResultGraphicConstants.Boundary), "Result");
                    }

                    result.IsOK = true;
                }
                else
                {
                    if (IsDrawResultGraphics)
                    {
                        if (tool.Results != null && tool.Results.GetBlobs().Count > 0)
                        {
                            cogDisplay.Engine.CogDisplay.StaticGraphics.Add(tool.Results.GetBlobs()[0].CreateResultGraphics(CogBlobResultGraphicConstants.Boundary), "Result");
                        }
                    }
                    result.IsOK = false;
                }
            }

            LastResult = result;

            return result;
        }

        public class BlobResult : HResult
        { 

        }
    }
}
