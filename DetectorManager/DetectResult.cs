using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectorManager
{
    public class DetectResult
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool Result { get; set; }
        public double Score { get; set; }
        public string Name { get; set; }

        private List<YoloItem> items = new List<YoloItem>();
        public List<YoloItem> Items
        {
            get
            {
                return items;
            }
            set
            {
                items = value;
            }
        }
    }
}
