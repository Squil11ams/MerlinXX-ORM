using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
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
