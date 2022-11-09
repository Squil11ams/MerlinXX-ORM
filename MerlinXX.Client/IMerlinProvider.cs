using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MerlinXX.Client
{
    public interface IMerlinProvider
    {
        string Query { get; }

        IEnumerable<IDataParameter> Parameters { get; }
    }
}
