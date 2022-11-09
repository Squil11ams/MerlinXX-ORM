using MerlinXX.Client;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MerlinXX.Server.MySQL
{
    public class BaseQuery : IMerlinProvider
    {
        #region FIELDS
        public IEnumerable<IDataParameter> Parameters { get { return MyParams; } }

        public List<MySqlParameter> MyParams { get; protected set; }

        public virtual string Query { get; set; }
        #endregion


        #region CONSTRUCTOR
        public BaseQuery()
        {
            MyParams = new List<MySqlParameter>();
        }
        #endregion


        #region PARAMETER MANIPULATIONS
        public void AddParameter(string key, object value)
        {
            MyParams.Add(new MySqlParameter(key, value));
        }
        #endregion


        #region StoredProcedure ShortHand
        /// <summary>
        /// Short Hand for Calling Store Procedures
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="values"></param>
        public void SetSP(string cmd, params object[] values)
        {
            Query = _GetSPQuery(cmd, values);

            int x = 1;

            foreach (object o in values)
            {
                AddParameter(_IntToLetters(x), o);

                x++;
            }
        }


        /// <summary>
        /// Create the sql statement for the command and parameter count.
        /// 
        /// Dynamically creates placeholders for parameters.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private static string _GetSPQuery(string cmd, object[] values)
        {
            StringBuilder sb = new StringBuilder("CALL ");
            sb.Append(cmd);
            sb.Append("( ");

            for (int i = 1; i <= values.Length; i++)
            {
                string le = (i != values.Length ? "," : "");

                sb.AppendFormat(" @{0}{1} ", _IntToLetters(i), le);
            }

            sb.Append(");");

            return sb.ToString();
        }

        private static string _IntToLetters(int value)
        {
            string result = string.Empty;

            while (--value >= 0)
            {
                result = (char)('A' + value % 26) + result;
                value /= 26;
            }

            return "@" + result;
        }
        #endregion
    }
}
