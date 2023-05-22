using HCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Xml;

namespace HVisionPro.UI
{
    /// <summary>
    /// HVidiTool.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HVidiToolUI : TabItem, IToolUI
    {


        private HToolUIStateConstant state = HToolUIStateConstant.Normal;

        private HVIDITool tool;
        public HVIDITool VidiTool { get { return tool; } }


        public HVidiToolUI(HVIDITool tool)
        {
            this.tool = tool;
            InitializeComponent();
            this.DataContext = this;
             
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

        private void Button_SaveParams_Click(object sender, RoutedEventArgs e)
        {
            tool.SaveDBPath();
        }
         

        private void Button_SaveRegex_Click(object sender, RoutedEventArgs e)
        {
            XmlDocument xmlDocument = new XmlDocument();
            XmlNode root = xmlDocument.CreateElement("Regex");
            xmlDocument.AppendChild(root);

            for (int i = 0; i < VidiTool.Regices.Count; i++)
            {
                if(VidiTool.Regices[i].ToolName != "")
                {
                    XmlNode regex = xmlDocument.CreateElement("Regex");
                    XmlAttribute toolName = xmlDocument.CreateAttribute("ToolName");
                    toolName.Value = VidiTool.Regices[i].ToolName;
                    XmlAttribute term = xmlDocument.CreateAttribute("Term");
                    term.Value = VidiTool.Regices[i].Term;
                    XmlAttribute value = xmlDocument.CreateAttribute("Value");
                    value.Value = VidiTool.Regices[i].Value;

                    regex.Attributes.Append(toolName);
                    regex.Attributes.Append(term);
                    regex.Attributes.Append(value);

                    root.AppendChild(regex);
                }
            }

            xmlDocument.Save(VidiTool.VidiRegexPath);
        }

        private void Button_RestoreRegex_Click(object sender, RoutedEventArgs e)
        {
            VidiTool.LoadRegex();
        }
    }
}
