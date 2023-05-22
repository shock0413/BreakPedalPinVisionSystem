using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViDi2;
using Cognex.VisionPro;

namespace HViDi
{
    public class ViDiResult
    {
        public List<ICogRecord> record = new List<ICogRecord>();
        public string toolName;
        public List<IFeature> features = new List<IFeature>();
        public List<IMatch> matches = new List<IMatch>();
        public List<ITag> tags = new List<ITag>();
    }
}
