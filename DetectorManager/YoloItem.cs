using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectorManager
{
    public class YoloItem
    {
        public string Type { get; set; }
        public double Confidence { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Point Center()
        {
            return new Point((double)(X + Width / 2), (double)(Y + Height / 2));
        }
    }
}
