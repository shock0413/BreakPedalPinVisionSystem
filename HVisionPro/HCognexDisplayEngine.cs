using Cognex.VisionPro.Display;
using HUtill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HVisionPro
{
    public class HCognexDisplayEngine : ViewModel
    {
        private string header;
        public string Header { get { return header; } set { header = value; NotifyPropertyChanged("Header"); } }

        private HCognexDisplay control;

        private CogDisplay cogDisplay;
        public CogDisplay CogDisplay { get { return cogDisplay; } }

        public HCognexDisplayEngine(HCognexDisplay control)
        {
            this.control = control;
            control.Loaded += Control_Loaded;
            //control.LayoutUpdated += Control_LayoutUpdated;

            CreateDisplay();
        }

        bool isCreated = false;
        private void Control_LayoutUpdated(object sender, EventArgs e)
        {
             
        }

        private void Control_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            
        }

        private void CreateDisplay()
        {
            Cognex.VisionPro.CogRecordDisplay cogDisplay = new Cognex.VisionPro.CogRecordDisplay();
            control.Host.Child = cogDisplay;
            cogDisplay.CreateControl();

            cogDisplay.HorizontalScrollBar = false;
            cogDisplay.VerticalScrollBar = false;
            cogDisplay.BackColor = System.Drawing.Color.Gray;
            cogDisplay.AutoFit = true;

            this.cogDisplay = cogDisplay;
        }
    }
}
