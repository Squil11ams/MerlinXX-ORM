using MerlinORM.Client;

using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using TestConsole;

namespace GridBeacon.Common.Models
{
    public class CurrentUser : GridUser
    {
        [MerlinObject]
        public GridClient Client { get; set; }

     

        public string session_key { get; set; }

        public int session_user { get; set; }

        public DateTime session_created { get; set; }

        public DateTime session_sighted { get; set; }

        [Exclude]
        public List<Permissions> Permissions { get; set; } = new List<Permissions>();

        #region READ ONLY FIELDS
        [Exclude]
        public bool IsExpired
        {
            get
            {
                var start = session_sighted.ToUniversalTime();

                var now = DateTime.Now.ToUniversalTime();

                return (start.AddHours(8) <= now);
            }
        }
        #endregion

        
    }
}
