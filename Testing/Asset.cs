using MerlinORM.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Testing
{
    public class Asset : MerlinModelBase
    {
        public int asset_id { get; set; }

        public string asset_uuid { get; set; }

        public string asset_name { get; set; }
    }
}
