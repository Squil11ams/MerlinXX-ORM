using MerlinORM.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole
{
    public class ClientEntity : MerlinModelBase
    {
        public int client_id { get; set; }

        public string client_name { get; set; }

        public string client_add1 { get; set; }

        public string client_add2 { get; set; }

        public string client_city { get; set; }

        public string client_state { get; set; }

        public string client_zip { get; set; }

        public string client_status { get; set; }
    }
}
