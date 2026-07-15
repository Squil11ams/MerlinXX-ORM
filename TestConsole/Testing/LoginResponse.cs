using MerlinORM.Client;

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TestConsole;

namespace GridBeacon.Common.Models
{
    public class LoginResponse : MerlinModelBase
    {
        [Exclude]
        public LoginStatusEnum AuthResult { get; set; }

        public int audit_id { get; set; }

        [AutoPopSettings("u_id")]
        public int UserID { get; set; }

        [AutoPopSettings("u_email")]
        public string Username { get; set; }

        [AutoPopSettings("u_pass")]
        public string Password { get; set; }

        [AutoPopSettings("u_status")]
        public ActInacEnum UserStatus { get; set; }

        [AutoPopSettings("client_status")]
        public ActInacEnum ClientStatus { get; set; }


        public override void SetDataObject(IDataReader data, string prefix = "")
        {
            var temp = data["auth_result"].ToString();

            if(temp == "pending")
            {
                AuthResult = LoginStatusEnum.Pending;

                base.SetDataObject(data, prefix);
            }
            else
            {
                AuthResult = LoginStatusEnum.Denied;
            }
        }

    }
}
