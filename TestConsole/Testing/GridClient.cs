using MerlinORM.Client;

using System;
using System.Collections.Generic;
using System.Text;

namespace GridBeacon.Common.Models
{
    public class GridClient : MerlinModelBase
    {
        [AutoPopSettings("client_id")]
        public int ID { get; set; }

        [AutoPopSettings("client_name")]
        public string Name { get; set; }

        [AutoPopSettings("client_add1")]
        public string Address_1 { get; set; }

        [AutoPopSettings("client_add2")]
        public string Address_2 { get; set; }

        [AutoPopSettings("client_city")]
        public string City { get; set; }

        [AutoPopSettings("client_state")]
        public string State { get; set; }

        [AutoPopSettings("client_zip")]
        public string ZipCode { get; set; }

        [AutoPopSettings("client_status")]
        public string Status { get; set; }
    }
}
