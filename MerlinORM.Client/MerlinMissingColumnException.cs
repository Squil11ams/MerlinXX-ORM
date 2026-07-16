using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Indicates the an expected column was not found in the dataset.
    /// </summary>
    public class MerlinMissingColumnException : MerlinException
    {
        /// <summary>
        ///  Name of the Object
        /// </summary>
        public string ObjectName { get; set; }

        /// <summary>
        /// Name of the Missing Column
        /// </summary>
        public string MissingColumn { get; set; }

        /// <summary>
        /// Indicates the an expected column was not found in the dataset.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="objectName"></param>
        /// <param name="missingColumn"></param>
        /// <param name="inner"></param>
        public MerlinMissingColumnException(string code, string objectName, string missingColumn, Exception inner) 
            : base(code, $"Unable to load '{objectName}' data set missing column '{missingColumn}'", inner)
        {
            ObjectName = objectName;
            MissingColumn = missingColumn;
        }
    }
}
