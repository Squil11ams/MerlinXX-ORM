using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MerlinXX.Server.MySQL
{
    public class GenericQuery : BaseQuery
    {
        #region CONSTRUCTORS
        public GenericQuery() : base() { }

        public GenericQuery(string Query) : base()
        {
            this.Query = Query;
        }

        public GenericQuery(string Query, string Key, string Value) : base()
        {
            this.Query = Query;

            AddParameter(Key, Value);
        }

        public GenericQuery(string Key, string Value) : base()
        {
            AddParameter(Key, Value);
        }

        public GenericQuery(string Query, Dictionary<string, string> pairs) : base()
        {
            this.Query = Query;

            foreach (var x in pairs)
            {
                AddParameter(x.Key, x.Value);
            }
        }

        public GenericQuery(Dictionary<string, string> pairs) : base()
        {
            foreach (var x in pairs)
            {
                AddParameter(x.Key, x.Value);
            }
        }


        public GenericQuery(string Query, string Key, int Value) : base()
        {
            this.Query = Query;

            AddParameter(Key, Value);
        }

        public GenericQuery(string Key, int Value) : base()
        {
            AddParameter(Key, Value);
        }
        #endregion

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

        public override string ToString()
        {
            return GetQuery();
        }
    }
}
