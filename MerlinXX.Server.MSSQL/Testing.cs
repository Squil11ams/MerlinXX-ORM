using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;

namespace MerlinXX.Server.MSSQL
{
    public class Testing
    {
        public static string Test()
        {
            var config = _OpenConfigFile();

            var connStr = _GetMySQLString(config);


            var data = Encoding.Default.GetBytes(connStr);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);
            var encStr = Encoding.Default.GetString(encrypted);

            config.AppSettings.Settings.Add("Merlin.MySQL.Default", "!==ENC==!"+ encStr);
            config.Save();


            return connStr;
        }


        private static Configuration _OpenConfigFile()
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        private static string _GetMySQLString(Configuration config)
        {
            if (config.AppSettings.Settings["Merlin.MySQL.Default"] == null)
            {
                config.AppSettings.Settings.Add("Merlin.MySQL.Default", "Not Provided");
                config.Save();

                return "Not Provided";
            }

            return config.AppSettings.Settings["Merlin.MySQL.Default"].Value;
        }
    }
}
