using GridBeacon.Common.Models;
using MerlinORM.Server.MySQL;
using System;
using System.Collections.Generic;
using System.Text;
using TestConsole;

namespace GridBeacon.Databases.GRID
{
    public static class UserDB
    {
        private static readonly QueryEngine DB = new("Default");

        public static LoginResponse Authenticate(string User, string ip, string location, string country, string continent)
        {
            var q = new GenericQuery("CALL volt_db.p_user_authenticate(@A,@C,@D,@E,@F);");

            q.AddParameter("@A", User.ToLower());
            q.AddParameter("@C", ip);
            q.AddParameter("@D", location);
            q.AddParameter("@E", country);
            q.AddParameter("@F", continent);

            var user = DB.GetObject<LoginResponse>(q);

            return user;
        }

        public static void LogLoginAttempt(int auditRecord, LoginStatusEnum status, string reason)
        {
            var q = new GenericQuery("CALL volt_db.p_user_audit(@A,@B,@C);");

            q.AddParameter("@A", auditRecord);
            q.AddParameter("@B", status);
            q.AddParameter("@C", reason);

            DB.ExecuteNonQuery(q);
        }

        public static CurrentUser LoadUser(LoginResponse loginResponse)
        {
            var q = new GenericQuery("CALL volt_db.p_user_load(@A,@B);");

            q.AddParameter("@A", loginResponse.audit_id);
            q.AddParameter("@B", loginResponse.UserID);

            var user = DB.GetObject<CurrentUser>(q);

            RefreshPermissions(user);

            return user;
        }

        public static CurrentUser LoadUser(LoginResponse loginResponse, string hashedPassword)
        {
            var q = new GenericQuery("CALL volt_db.p_user_load_update(@A,@B,@C);");

            q.AddParameter("@A", loginResponse.audit_id);
            q.AddParameter("@B", loginResponse.UserID);
            q.AddParameter("@C", hashedPassword);

            var user = DB.GetObject<CurrentUser>(q);

            RefreshPermissions(user);

            return user;
        }

        public static void RefreshPermissions(CurrentUser user)
        {
            var q = new GenericQuery("SELECT * FROM volt_db.user_perms WHERE perm_user = @A;");
            q.AddParameter("@A", user.ID);
            user.Permissions = DB.GetList<Permissions>(q);
        }

        public static void Logout(CurrentUser user)
        {
            var q = new GenericQuery("DELETE FROM volt_db.sessions WHERE session_key = @A;");

            q.AddParameter("@A", user.session_key);

            DB.ExecuteNonQuery(q);
        }
    }
}
