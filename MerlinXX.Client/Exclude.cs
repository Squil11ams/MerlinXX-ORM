using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MerlinXX.Client
{
    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Field | AttributeTargets.Property)]
    public class Exclude : System.Attribute
    {
        private bool _excluded;

        public bool excluded { get { return _excluded; } }

        public Exclude(bool exclude = true)
        {
            this._excluded = exclude;
        }
    }
}
