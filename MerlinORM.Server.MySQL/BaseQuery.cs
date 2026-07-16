using MerlinORM.Client;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MerlinORM.Server.MySQL
{
    /// <summary>
    /// Provides basic implementation for IMerlinProvider that allows you to extend from. see <see cref="GenericQuery"/> as an example.
    /// </summary>
    public class BaseQuery : IMerlinProvider
    {
        #region FIELDS
        /// <summary>
        /// List of Parameters for Paramertized Query in Generic Form
        /// </summary>
        public IEnumerable<IDataParameter> Parameters { get { return MyParams; } }

        /// <summary>
        /// List of MySQL Parameters for Paramertized Query
        /// </summary>
        public List<MySqlParameter> MyParams { get; protected set; }

        /// <summary>
        /// SQL Statement to be executed.
        /// </summary>
        public virtual string Query { get; set; }
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Builds empty instance
        /// </summary>
        public BaseQuery()
        {
            Query = "";
            MyParams = new List<MySqlParameter>();
        }
        #endregion


        #region PARAMETER MANIPULATIONS
        /// <summary>
        /// Creates a new parameter from given key pair
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
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

        /// <summary>
        /// Converts Int to Letter ie 1 = A, 2 = B, 27 = AA
        /// Similar to excel columns
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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
