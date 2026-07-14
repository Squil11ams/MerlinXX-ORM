using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MerlinORM.Client
{
    /// <summary>
    /// Used to mark a class as a Merlin object that can be populated from a database reader.
    /// </summary>
    public interface IMerlinObject
    {
        /// <summary>
        /// Populates the object using the current row from a database reader.
        /// </summary>
        /// <param name="data">Data reader positioned at the current record.</param>
        /// <param name="prefix">Optional column prefix used to resolve duplicate column names.</param>
        void SetDataObject(IDataReader data, string prefix = "");
    }
}
