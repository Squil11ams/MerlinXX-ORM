using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Field | AttributeTargets.Property)]
    public class MerlinObject : System.Attribute
    {
        private string _prefix;

        public string prefix { get { return _prefix; } }

        public MerlinObject(string prefix)
        {
            this._prefix = prefix;
        }
    }
}
