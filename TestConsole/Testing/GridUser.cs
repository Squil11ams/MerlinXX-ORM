using MerlinORM.Client;

using System;
using System.Collections.Generic;
using System.Text;
using TestConsole;

namespace GridBeacon.Common.Models
{
    public enum UserLevelEnum
    {
        Na,
        Basic,
        Adv,
        Role,
        Admin
    }

    public class GridUser : MerlinModelBase
    {
        [AutoPopSettings("u_id")]
        public int ID { get; set; }

        [AutoPopSettings("u_client")]
        public int Client {  get; set; }

        [AutoPopSettings("u_name")]
        public string? Name { get; set; }

        [AutoPopSettings("u_email")]
        public string? Email { get; set; }

        [AutoPopSettings("u_phone")]
        public string? Phone { get; set; }

        [AutoPopSettings("u_status")]
        public ActInacEnum Status { get; set; }

        [AutoPopSettings("u_level")]
        public UserLevelEnum Level { get; set; }

        [AutoPopSettings("u_key")]
        public string? Key { get; set; }

        [AutoPopSettings("u_lastlogin")]
        public DateTime LastLogin { get; set; }
    }
}
