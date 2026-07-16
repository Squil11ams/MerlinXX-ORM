using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Server.MySQL
{
    /// <summary>
    /// Basic Query Provider, I use this 99% of the time.
    /// </summary>
    public class GenericQuery : BaseQuery
    {
        #region CONSTRUCTORS
        /// <summary>
        /// Creates empty instance... Not Helpful
        /// </summary>
        public GenericQuery() : base() { }


        /// <summary>
        /// Creates instance and sets the Query
        /// </summary>
        /// <param name="Query"></param>
        public GenericQuery(string Query) : base()
        {
            this.Query = Query;
        }

        /// <summary>
        /// Creates instance with SQL and creates 1 parameter with supplied key pair.
        /// </summary>
        /// <param name="Query"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public GenericQuery(string Query, string Key, string Value) : base()
        {
            this.Query = Query;

            AddParameter(Key, Value);
        }


        /// <summary>
        /// Creates an instance with no SQL but 1 parameter key pair
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public GenericQuery(string Key, string Value) : base()
        {
            AddParameter(Key, Value);
        }

        /// <summary>
        /// Creates an instance with SQL and builds list of paramters from supplied dictionary
        /// </summary>
        /// <param name="Query"></param>
        /// <param name="pairs"></param>
        public GenericQuery(string Query, Dictionary<string, string> pairs) : base()
        {
            this.Query = Query;

            foreach (var x in pairs)
            {
                AddParameter(x.Key, x.Value);
            }
        }

        /// <summary>
        /// Creates an instance with no SQL and builds list of paramters from supplied dictionary
        /// </summary>
        /// <param name="pairs"></param>
        public GenericQuery(Dictionary<string, string> pairs) : base()
        {
            foreach (var x in pairs)
            {
                AddParameter(x.Key, x.Value);
            }
        }

        /// <summary>
        /// Not sure why this is here?
        /// </summary>
        /// <param name="Query"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>

        public GenericQuery(string Query, string Key, int Value) : base()
        {
            this.Query = Query;

            AddParameter(Key, Value);
        }

        /// <summary>
        /// Not sure why this is here?
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public GenericQuery(string Key, int Value) : base()
        {
            AddParameter(Key, Value);
        }
        #endregion

        /// <summary>
        /// Gets a visualization of the query with parameters and values. For Debugging Purposes I guess?
        /// I havent used this in a long time.
        /// </summary>
        /// <returns></returns>
        public string GetQuery()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.Query);

            int i = 1;

            foreach (var p in Parameters)
            {
                sb.AppendLine($"[{i}] {p.ParameterName} => {p.Value}");
                i++;
            }

            return sb.ToString();
        }

        /// <summary>
        /// ToString just calls <see cref="GetQuery"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetQuery();
        }
    }
}
