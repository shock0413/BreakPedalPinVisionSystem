using Cognex.VisionPro;
using HCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HVisionPro.UI
{
    /// <summary>
    /// HBlobToolUI.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HBlobToolUI : TabItem, IToolUI
    {
        private HToolUIStateConstant state = HToolUIStateConstant.Normal;

        private HBlobTool tool;
        public HBlobTool BlobTool { get { return tool; } }

        public HBlobToolUI(HBlobTool tool)
        {
            this.tool = tool;
            InitializeComponent();
            this.DataContext = this;
        }


        private void Button_ROISetting_Click(object sender, RoutedEventArgs e)
        {
            if (tool.CogDisplay != null && tool.CogDisplay.Image != null)
            {
                state = HToolUIStateConstant.ROISetting;
                ClearDisplay();

                CogRectangleAffine roi = new CogRectangleAffine(tool.ROI);


                HTool masterTool = tool.GetMasterTool();
                if (masterTool != null && masterTool.LastResult == null)
                {
                    masterTool.Run();
                }

                if (masterTool != null)
                {
                    roi.CenterX += masterTool.LastResult.X;
                    roi.CenterY += masterTool.LastResult.Y;
                }

                CogRectangleAffine rectangleAffine = new CogRectangleAffine(roi);
                rectangleAffine.GraphicDOFEnable = CogRectangleAffineDOFConstants.All;
                rectangleAffine.Interactive = true;
                tool.CogDisplay.Engine.CogDisplay.InteractiveGraphics.Add(rectangleAffine, "", true);
            }
        }
        private void ClearDisplay()
        {
            tool.CogDisplay.Engine.CogDisplay.InteractiveGraphics.Clear();
            tool.CogDisplay.Engine.CogDisplay.StaticGraphics.Clear();
        }

        public void Cancel()
        {
            ClearDisplay();
            state = HToolUIStateConstant.Normal;
        }

        public void Confirm()
        {
            if (state == HToolUIStateConstant.ROISetting)
            {
                if (tool.CogDisplay.Engine.CogDisplay.InteractiveGraphics.Count > 0)
                {
                    if (tool.CogDisplay.Engine.CogDisplay.InteractiveGraphics[0].GetType() == typeof(CogRectangleAffine))
                    {

                        HTool masterTool = tool.GetMasterTool();
                        if (masterTool != null && masterTool.LastResult == null)
                        {
                            masterTool.Run();
                        }



                        CogRectangleAffine rec = tool.CogDisplay.Engine.CogDisplay.InteractiveGraphics[0] as CogRectangleAffine;
                        double originX;
                        double originY;
                        double sideXLength;
                        double sideYLength;
                        double rotation;
                        double skew;

                        rec.GetOriginLengthsRotationSkew(out originX, out originY, out sideXLength, out sideYLength, out rotation, out skew);

                        if (masterTool != null)
                        {
                            originX -= masterTool.LastResult.X;
                            originY -= masterTool.LastResult.Y;
                        }

                        tool.SaveRoi(originX, originY, sideXLength, sideYLength, rotation, skew);
                    }
                }
            }
            else if (state == HToolUIStateConstant.Setting)
            {

            }

            ClearDisplay();
        }


        private void Button_Inspection_Click(object sender, RoutedEventArgs e)
        {
            ClearDisplay();
            if (tool.CogDisplay != null && tool.CogDisplay.Image != null)
            {
                HResult result = tool.Run();
                if (result.IsOK)
                {
                    tool.CogDisplay.DrawOk();
                }
                else
                {
                    tool.CogDisplay.DrawNG();
                }
            }
        }

        private void Button_MasterAlignSave_Click(object sender, RoutedEventArgs e)
        {
            tool.SaveMaster();
        }

        private void Button_SaveParams_Click(object sender, RoutedEventArgs e)
        {
            tool.SaveBlobThreshold(tool.Threshold);
            tool.SaveAreaMaxThreshold(tool.AreaMax);
            tool.SaveAreaMinThreshold(tool.AreaMin);
        }
    }
}
