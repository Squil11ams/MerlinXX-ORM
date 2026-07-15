
using MerlinORM.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace GridBeacon.Common.Models
{
    public enum PermTypeEnum
    {
        NA,
        Group,
        Region,
        Location
    }

    public enum PermLevelEnum
    {
        NA,
        Read,
        Write
    }

    public class Permissions : MerlinModelBase
    {
        public int perm_id { get; set; }

        public PermTypeEnum perm_type { get; set; }

        public int perm_user { get; set; }

        public int perm_group { get; set; } 

        public PermLevelEnum perm_level {  get; set; }
    }
}
