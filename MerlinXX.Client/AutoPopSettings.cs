using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MerlinXX.Client
{
    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Field | AttributeTargets.Property)]
    public class AutoPopSettings : System.Attribute
    {
        private string _key;
        private bool _throwError;
        private Type _type;
        private object _defaultValue;

        public string key { get { return _key; } }
        public bool throwError { get { return _throwError; } }

        public dynamic defaultValue
        {
            get
            {
                return Convert.ChangeType(_defaultValue, _type);
            }
        }

        public Type type { get { return _type; } }
        public object defaultValueObj { get { return _key; } }


        public AutoPopSettings(string key, bool throwError, Type type, object defaultValue)
        {
            _key = key;
            _throwError = throwError;
            _type = type;
            _defaultValue = defaultValue;
        }

        public AutoPopSettings(string key)
        {
            _key = key;
            _throwError = true;
        }
    }
}
