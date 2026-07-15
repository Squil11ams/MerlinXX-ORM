using MerlinORM.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole
{
    public enum ActInacEnum
    {
        Na,
        Active,
        Inactive
    }

    public enum LoginStatusEnum
    {
        Na,
        Pending,
        Approved,
        Denied
    }

    public enum UserLevelEnum
    {
        Na,
        Basic,
        Adv,
        Role,
        Admin
    }

    public class User : MerlinModelBase
    {
        [MerlinObject()]
        public ClientEntity client { get; set; }

        public int u_id { get; set; }

        public int u_client { get; set; }

        public string? u_name { get; set; }

        public string? u_email { get; set; }

        public string? u_phone { get; set; }

        public ActInacEnum u_status { get; set; }

        public UserLevelEnum u_level { get; set; }

        public string? u_key { get; set; }

        public DateTime u_lastlogin { get; set; }
    }
}
