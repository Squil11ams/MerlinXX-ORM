using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Marks item as a nested MerlinModel allowing the system to populate that object
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Field | AttributeTargets.Property)]
    public class MerlinObject : System.Attribute
    {
        private string _prefix;

        /// <summary>
        /// Prefix to apply to all columns for this class.
        /// </summary>
        public string prefix { get { return _prefix; } }

        /// <summary>
        /// Marks item as a nested MerlinModel allowing the system to populate that object.
        /// </summary>
        /// <param name="prefix">Applies the prefix to all column names, incase of duplicate joins.</param>
        public MerlinObject(string prefix = "")
        {
            this._prefix = prefix;
        }
    }
}
