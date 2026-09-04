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

        /// <summary>Controls whether the nested object is always created or only created for a populated row.</summary>
        public NestedObjectCreation Creation { get; }

        /// <summary>
        /// Marks item as a nested MerlinModel allowing the system to populate that object.
        /// </summary>
        /// <param name="prefix">Applies the prefix to all column names, incase of duplicate joins.</param>
        public MerlinObject(string prefix = "")
        {
            this._prefix = prefix;
            Creation = NestedObjectCreation.Always;
        }

        /// <summary>
        /// Marks a property as a nested Merlin model and configures when it is instantiated.
        /// </summary>
        /// <param name="prefix">Prefix applied to the nested model's column names.</param>
        /// <param name="creation">Nested object creation policy.</param>
        public MerlinObject(string prefix, NestedObjectCreation creation)
        {
            _prefix = prefix;
            Creation = creation;
        }
    }
}
