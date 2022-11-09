using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MerlinXX.Client
{
    public interface IMerlinObject
    {
        /// <summary>
        /// Sets object state using data from MySql
        /// </summary>
        /// <param name="data">DataReader to populate object with.</param>
        void SetDataObject(IDataReader data, int Flag, string prefix = "");
    }
}
