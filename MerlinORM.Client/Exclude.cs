using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Tells the mapping system to ignore this property
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Field | AttributeTargets.Property)]
    public class Exclude : System.Attribute
    {
        private bool _excluded;

        /// <summary>
        /// <c>True</c> makes the mapper ignore that property.
        /// </summary>
        public bool excluded { get { return _excluded; } }

        /// <summary>
        /// Tells mapping system to ignore the property.
        /// </summary>
        /// <param name="exclude"></param>
        public Exclude(bool exclude = true)
        {
            this._excluded = exclude;
        }
    }
}
