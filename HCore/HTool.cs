using Cognex.VisionPro.Display;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HCore
{
    public abstract class HTool : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        HCameraShot shot;
        HInspection inspection;
        HPart part;
        string toolName;
        public string ToolName { get { return toolName; } }
        string dir;
        public string Dir { get { return dir; } }
        public string ToolIniPath { get { return dir + "\\" + toolName + ".ini"; } }

        public HTool(HCameraShot shot, HInspection inspection, HPart part, string toolName)
        {
            this.shot = shot;
            this.inspection = inspection;
            this.part = part;
            this.toolName = toolName;

            dir = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Camera" + "\\" + inspection.Name;
            try
            {
                if (Directory.Exists(dir) == false)
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch
            {

            }
            
        }

        private bool isDrawResultGraphics = true;
        public bool IsDrawResultGraphics { get { return isDrawResultGraphics; } set { isDrawResultGraphics = value; } }

        public abstract UIElement LoadToolUI();
        public abstract void LoadTool();

        public abstract HResult Run();

        public abstract void SetDisplay(object display);

        public override string ToString()
        {
            return toolName;
        }

        public HResult LastResult { get; set; }

    }
}
