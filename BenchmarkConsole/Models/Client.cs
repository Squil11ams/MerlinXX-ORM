using MerlinORM.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkConsole.Models
{
    public class Client : MerlinModelBase
    {
        public int client_id {  get; set; }

        public string client_code {  get; set; }

        public string client_name { get; set; }

        public string client_address { get; set; } 

        public string client_city { get; set; }    

        public string client_state { get; set; }   

        public string client_zip {  get; set; }

        public string client_phone { get; set; }  

        public string client_email { get; set; }

        public bool client_active { get; set; }

        public DateTime client_created { get; set; }
    }
}
