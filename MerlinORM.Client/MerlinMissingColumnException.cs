using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    public class MerlinMissingColumnException : MerlinException
    {
        public string ObjectName { get; set; }

        public string MissingColumn { get; set; }

        public MerlinMissingColumnException(string code, string objectName, string missingColumn, Exception inner) 
            : base(code, $"Unable to load '{objectName}' data set missing column '{missingColumn}'", inner)
        {
            ObjectName = objectName;
            MissingColumn = missingColumn;
        }
    }
}
