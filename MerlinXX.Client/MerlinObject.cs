using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MerlinXX.Client
{

    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Field | AttributeTargets.Property)]
    public class MerlinObject : System.Attribute
    {
        private int _flag;
        private string _prefix;

        public int flag { get { return _flag; } }
        public string prefix { get { return _prefix; } }

        public MerlinObject(int Flag = 0)
        {
            this._flag = Flag;
            this._prefix = "";
        }

        public MerlinObject(string prefix)
        {
            this._flag = 0;
            this._prefix = prefix;
        }

        public MerlinObject(int Flag, string prefix)
        {
            this._flag = Flag;
            this._prefix = prefix;
        }
    }
}
