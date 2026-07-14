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

        public object defaultValueObj { get { return _key; } }


        public AutoPopSettings(string key, string? defaultValueSet = null)
        {
            _key = key;
            _defaultValue = defaultValueSet;
            _throwError = defaultValueSet == null;
        }

        
    }
}