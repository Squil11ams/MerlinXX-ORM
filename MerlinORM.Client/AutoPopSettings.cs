using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Allows you specify a custom column, and to set a default value instead of throwing exception.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Field | AttributeTargets.Property)]
    public class AutoPopSettings : System.Attribute
    {
        private string _key;
        private bool _throwError;
        private string? _defaultValue;

        /// <summary>
        /// Column name 
        /// </summary>
        public string key { get { return _key; } }
        
        /// <summary>
        /// True if a default value is <b>NOT</b> set, otherwise False
        /// </summary>
        public bool throwError { get { return _throwError; } }

        /// <summary>
        /// Specify a string with the default value, the Converter will convert from string to required Type.
        /// </summary>
        /// <example>
        /// For int "0"
        /// For DateTime "2026-01-01 00:00:00"
        /// </example>
        public string defaultValue
        {
            get
            {
                return _defaultValue ?? "";
            }
        }

        /// <summary>
        /// Builds an instance of AutoPopSettings with supplied values.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValueSet"></param>
        public AutoPopSettings(string key, string? defaultValueSet = null)
        {
            _key = key;
            _defaultValue = defaultValueSet;
            _throwError = defaultValueSet == null;
        }

        
    }
}