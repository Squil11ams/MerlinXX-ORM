using MerlinORM.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkConsole.Models
{
    public class User : MerlinModelBase
    {
        public int user_id {  get; set; }

        public int user_client { get; set; }

        public string user_username { get; set; }

        public string user_first_name { get; set; } 

        public string user_last_name { get; set; }

        public string user_email { get; set; }

        public string user_phone { get; set; }

        public int user_status { get; set; }

        public int user_level { get; set; }

        public DateTime user_last_login { get; set; }

        public DateTime user_created { get; set; }
    }
}
