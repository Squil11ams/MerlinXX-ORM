using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Field | AttributeTargets.Property)]
    public class AutoPopSettings : System.Attribute
    {
        private string _key;
        private bool _throwError;
        private Type _type;
        private string? _defaultValue;

        public string key { get { return _key; } }
        public bool throwError { get { return _throwError; } }

        public string defaultValue
        {
            get
            {
                return _defaultValue ?? "";
            }
        }

        public Type type { get { return _type; } }
        public object defaultValueObj { get { return _key; } }


        public AutoPopSettings(string key, bool throwError, Type type, string defaultValue)
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