using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.PMAlign;
using HCore;
using HUtill;
using HVisionPro.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HVisionPro
{
    public class HPatternTool : HTool
    {
        private ObservableCollection<HTool> beforeTool = new ObservableCollection<HTool>();
        public ObservableCollection<HTool> BeforeTool { get { return beforeTool; } set { beforeTool = value; NotifyPropertyChanged("BeforeTool"); } }

        private ObservableCollection<HPatternTool.HPattern> patterns = new ObservableCollection<HPatternTool.HPattern>();
        public ObservableCollection<HPatternTool.HPattern> Patterns { get { return patterns; } set { patterns = value; NotifyPropertyChanged("Patterns"); } }

        internal IniFile config;

        private HCognexDisplay cogDisplay;
        public HCognexDisplay CogDisplay
        {
            get { return cogDisplay; }
            set { cogDisplay = value; }
        }

        public CogRectangleAffine ROI { get; set; }

        private int score = 60;
        public int Score{ get { return score; } set { score = value; NotifyPropertyChanged("Score"); } }


        private int angleMin = 0;
        public int AngleMin { get { return angleMin; } set { angleMin = value; NotifyPropertyChanged("AngleMin"); } }

        private int angleMax = 0;
        public int AngleMax { get { return angleMax; } set { angleMax = value; NotifyPropertyChanged("AngleMax"); } }


        private double scaleMin = 1;
        public double ScaleMin { get { return scaleMin; } set { scaleMin = value; NotifyPropertyChanged("ScaleMin"); } }


        private double scaleMax = 1;
        public double ScaleMax { get { return scaleMax; } set { scaleMax = value; NotifyPropertyChanged("ScaleMax"); } }

        public string ToolName { get; set; }

        private bool isUseMaster = false;
        public bool IsUseMaster { get { return isUseMaster; } set { isUseMaster = value; NotifyPropertyChanged("IsUseMaster"); } }

        private HTool masterTool;
        public HTool MasterTool { get { return masterTool; } set { masterTool = value; NotifyPropertyChanged("MasterTool"); } }

    public HPatternTool(HCameraShot shot, HInspection inspection, HPart part, string toolName): 
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
            if(originX != -1)
            {
                ROI.SetOriginLengthsRotationSkew(originX, originY, sideXLength, sideYLength, rotation, skew);
            }

            bool isUseMaster = config.GetBoolian("Master", "Is Use", false);
            string masterToolName = config.GetString("Master", "Tool Name", "");

            IsUseMaster = isUseMaster;
            for(int i = 0; i < beforeTool.Count; i++)
            {
                if(beforeTool[i].ToolName == masterToolName)
                {
                    MasterTool = beforeTool[i];
                    break;
                }

            }

            LoadScore();
            LoadPattern();
            LoadAngleMin();
            LoadAngleMax();
            LoadScaleMin();
            LoadScaleMax();
        }

        internal void LoadScore()
        {
            Score = config.GetInt32("Info", "Score", 60);
        }

        internal void SaveScore(int score)
        {
            config.WriteValue("Info", "Score", score);
        }

        internal void LoadAngleMax()
        {
            AngleMax = config.GetInt32("Info", "Angle Max", 0);
        }

        internal void SaveAngleMax(int angle)
        {
            config.WriteValue("Info", "Angle Max", angle);
        }

        internal void LoadAngleMin()
        {
            AngleMin = config.GetInt32("Info", "Angle Min", 0);
        }

        internal void SaveAngleMin(int angle)
        {
            config.WriteValue("Info", "Angle Min", angle);
        }

        internal void LoadScaleMin()
        {
            scaleMin = config.GetDouble("Info", "Scale Min", 1);
        }

        internal void SaveScaleMin(double scale)
        {
            config.WriteValue("Info", "Scale Min", scale);
        }

        internal void LoadScaleMax()
        {
            scaleMax = config.GetDouble("Info", "Scale Max", 1);
        }

        internal void SaveScaleMax(double scale)
        {
            config.WriteValue("Info", "Scale Max", scale);
        }

        internal void LoadPattern()
        {
            List<HPatternTool.HPattern> patterns = new List<HPatternTool.HPattern>();

            string patternDir = Dir + "\\Pattern";
            if(Directory.Exists(patternDir))
            {
                string[] files = Directory.GetFiles(patternDir);
                for(int i = 0; i < files.Length; i++)
                {
                    string patternPath = files[i];
                    string ext = Path.GetExtension(patternPath);
                    if(ext == ".hsr")
                    {
                        object obj = CogSerializer.LoadObjectFromFile(patternPath);
                        CogPMAlignPattern pattern = obj as CogPMAlignPattern;
                        if(pattern != null)
                        {
                            HPattern hPattern = new HPattern(pattern);
                            patterns.Add(hPattern);
                        }
                    }
                }
            }

            Patterns = new ObservableCollection<HPatternTool.HPattern>(patterns);
        }

        public override UIElement LoadToolUI()
        {
            HPatternToolUI tool = new HPatternToolUI(this);
            
            return tool;
        }



        public override HResult Run()
        {
            HResult result = new HResult();

            if (cogDisplay != null && cogDisplay.Image != null)
            {
                bool isOk = false;

                CogImage8Grey image = HImageProcessing.Get8GrayImage(cogDisplay.Image);

                CogPMAlignTool tool = new CogPMAlignTool();
                List<double> patternScore = new List<double>();
                List<CogPMAlignResult> patternResult = new List<CogPMAlignResult>();

                double maxScore = 0;
                int maxIndex = -1;

                HTool masterTool = GetMasterTool();
                double masterXBias = 0;
                double masterYBias = 0;

                double findX;
                double findY;

                if (masterTool != null && masterTool.GetType() == typeof(HPatternTool))
                {
                    if(masterTool.LastResult == null)
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
                    tool.SearchRegion = roi;
                    //tool.RunParams.AcceptThreshold = score * 1.0 / 100;
                    tool.RunParams.AcceptThreshold = 0.15;

                    if (angleMin != 0 && angleMax != 0)
                    {
                        CogPMAlignZoneAngle angle = new CogPMAlignZoneAngle();
                        angle.High = angleMax;
                        angle.Low = angleMin;

                        angle.Configuration = CogPMAlignZoneConstants.LowHigh;
                        tool.RunParams.ZoneAngle = angle;
                    }


                    CogPMAlignZoneScale scale = new CogPMAlignZoneScale();
                    scale.High = scaleMax;
                    scale.Low = scaleMin;
                    scale.Configuration = CogPMAlignZoneConstants.LowHigh;
                    tool.RunParams.ZoneScale = scale;

                    for (int i = 0; i < patterns.Count; i++)
                    {
                        CogPMAlignPattern pattern = patterns[i].CognexPattern;
                        tool.Pattern = pattern;
                        tool.Run();

                        if (tool.Results != null && tool.Results.Count > 0)
                        {
                            maxScore = 0;
                            maxIndex = -1;
                            for (int j = 0; j < tool.Results.Count; j++)
                            {
                                if (tool.Results[j].Score > maxScore)
                                {
                                    maxScore = tool.Results[j].Score;
                                    maxIndex = j;
                                }
                            }

                            patternScore.Add(maxScore);
                            patternResult.Add(tool.Results[maxIndex]);
                        }
                        else
                        {
                            patternScore.Add(0);
                            patternResult.Add(null);
                        }
                    }

                    maxIndex = -1;
                    maxScore = 0;
                    for (int i = 0; i < patternScore.Count; i++)
                    {
                        if (maxScore < patternScore[i])
                        {
                            maxIndex = i;
                            maxScore = patternScore[i];
                        }
                    }

                    cogDisplay.Engine.CogDisplay.StaticGraphics.Add(roi, "Result");

                    if (maxScore * 100 >= score)
                    {
                        isOk = true;

                        findX = patternResult[maxIndex].GetPose().TranslationX;
                        findY = patternResult[maxIndex].GetPose().TranslationY;

                        result.X = findX;
                        result.Y = findY;

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

                if(isOk)
                {
                    if (IsDrawResultGraphics)
                    {
                        cogDisplay.Engine.CogDisplay.StaticGraphics.Add(patternResult[maxIndex].CreateResultGraphics(CogPMAlignResultGraphicConstants.Origin | CogPMAlignResultGraphicConstants.MatchRegion), "Result");

                        CogGraphicLabel scoreLabel = new CogGraphicLabel();
                        scoreLabel.X = result.X;
                        scoreLabel.Y = result.Y;
                        scoreLabel.Text = Math.Round((maxScore * 100), 2).ToString();
                        scoreLabel.BackgroundColor = CogColorConstants.Black;
                        scoreLabel.Color = CogColorConstants.White;
                        scoreLabel.Alignment = CogGraphicLabelAlignmentConstants.TopRight;
                        
                        cogDisplay.Engine.CogDisplay.StaticGraphics.Add(scoreLabel, "Result");
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

        public void SavePattern(CogRectangleAffine rec)
        {
            try
            {
                CogPMAlignTool tool = new CogPMAlignTool();
                CogImage8Grey image = HImageProcessing.Get8GrayImage(cogDisplay.Engine.CogDisplay.Image);
                tool.InputImage = image;
                tool.Pattern.TrainImage = image;
                tool.Pattern.TrainRegion = rec;
                tool.Pattern.TrainRegionMode = CogRegionModeConstants.PixelAlignedBoundingBoxAdjustMask;
                tool.Pattern.Origin.TranslationX = rec.CenterX;
                tool.Pattern.Origin.TranslationY = rec.CenterY;
                tool.Pattern.Train();

                if (tool.Pattern.Trained)
                {
                    string path = Dir + "\\Pattern\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".hsr";
                    string dir = Path.GetDirectoryName(path);
                    if(Directory.Exists(dir) == false)
                    {
                        Directory.CreateDirectory(dir);
                    }

                    CogSerializer.SaveObjectToFile(tool.Pattern, path);

                }
            }
            catch(Exception ex)
            {
                
            }
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

        public class HPattern
        {
            public HPattern(CogPMAlignPattern cogPattern)
            {
                this.cogPattern = cogPattern;
            }
            CogPMAlignPattern cogPattern;
            public CogPMAlignPattern CognexPattern { get { return cogPattern; } }

            public BitmapSource Image { get { return ImageManager.ToBitmapSource(cogPattern.GetTrainedPatternImage().ToBitmap()); } }
        }

    }
}
