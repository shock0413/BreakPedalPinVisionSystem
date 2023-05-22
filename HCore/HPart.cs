using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCore
{
    public class HPart
    {
        private string name;

        public HPart(string name)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
