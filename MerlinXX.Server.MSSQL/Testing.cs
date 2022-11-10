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
        #region FIELDS
        /// <summary>
        /// Default Hard Coded Entropy If User Doesn't Supply.
        /// Something better than nothing.
        /// </summary>
        private const string _EntropyString = "RR-V12-27L-SUPER-1,240HP";
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Gets the connection string associated to the provided key.
        /// 
        /// If non exists, it will create a new entry in the config with value of 'Not Provided'
        /// </summary>
        /// <param name="Key">Key to look for in the App.Config file</param>
        /// <returns>Connection String to connect to the database.</returns>
        public static string GetConnectionString(string Key = "Merlin.MySQL.Default")
        {
            var config = _OpenConfigFile();

            var connStr = _GetMySQLString(config, Key);

            var Entropy = _Entropy(config);

            var result = _decrypt(connStr, Entropy);

            if(!result.Item1)
            {
                var encrypted = _encrypt(connStr, Entropy);
                config.AppSettings.Settings[Key].Value = encrypted;
                config.Save();
            }

            return result.Item2;
        }
        #endregion

        #region CONFIGURATION METHODS
        /// <summary>
        /// Find the current executing applications App.Config file and return a reference to it.
        /// </summary>
        /// <returns>Ref. to active config file.</returns>
        private static Configuration _OpenConfigFile()
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        /// <summary>
        /// Find the entry in AppSettings for the specified key.
        /// Returns Null if key not found.
        /// </summary>
        /// <param name="config">Ref. to active config file.</param>
        /// <param name="Key">Key to lookup</param>
        /// <returns>Value related to the key or null if key not found.</returns>
        private static string _FetchConfigEntry(Configuration config, string key)
        {
            if (config.AppSettings.Settings[key] == null)
            {
                return null;
            }

            return config.AppSettings.Settings[key].Value;
        }

        /// <summary>
        /// Attempts to locate the connection string for the specified key.
        /// 
        /// If key not found, create it with the default value 'Not Provided'.
        /// </summary>
        /// <remarks>Return could/should be encrypted and will most likely require decryption.</remarks>
        /// <param name="config">Ref. to active config file.</param>
        /// <param name="key">Key to lookup connection string.</param>
        /// <returns>Connection string related to the key</returns>
        private static string _GetMySQLString(Configuration config, string key)
        {
            var value = _FetchConfigEntry(config, key);

            if (value == null)
            {
                config.AppSettings.Settings.Add(key, "Not Provided");
                config.Save();

                return "Not Provided";
            }

            return value;
        }

        #endregion

        #region ENCRYPTION METHODS
        /// <summary>
        /// Encrypts using the ProtectedData @ LocalMachine Level with either Hardcoded or User Supplied Entropy.
        /// </summary>
        /// <param name="targetString">String to be encrypted.</param>
        /// <param name="Entropy">Entropy to be used when encrypting.</param>
        /// <returns>Base64 Encoded String with special !==B64==! Tag prepended to result so we know this entry was encrypted.</returns>
        private static string _encrypt(string targetString, Byte[] Entropy)
        {
            var data = Encoding.Default.GetBytes(targetString);
            var encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.LocalMachine);
            return "!==B64==!" + Convert.ToBase64String(encrypted);
        }


        /// <summary>
        /// Checks to see if supplied string needs to be decrypted or not. String must contain the prepended tag in order to be decrypted.
        /// </summary>
        /// <param name="text">String to be decrypted.</param>
        /// <param name="Entropy">Entropy to be used when decrypting.</param>
        /// <returns>Tuple(bool,string)
        /// Bool - <b>True</b> if string was actually decrypted otherwise <b>false</b>
        /// String - The decrypted string or original string if not needed. 
        /// </returns>
        private static Tuple<bool, string> _decrypt(string text, byte[] Entropy)
        {
            // STRING IS DEFAULT CONTENT AND DOES NOT NEED TO BE ENCRYPTED.
            if (text == "Not Provided") { return new Tuple<bool, string>(true, text); }

            // STRING LENGTH IS WAY TOO SHORT TO BE A VALID CONN STRING AND THUS OMIT ENCRYPTION
            if (text.Length < 9) { return  new Tuple<bool,string>(true, text); }

            // STRING IS MISSING THE TAG, ENCRYPT AND SAVE
            if (text.Substring(0, 9) != "!==B64==!") { return new Tuple<bool, string>(false, text); }


            var data = Convert.FromBase64String(text.Substring(9));

            var clean = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.LocalMachine);

            return new Tuple<bool, string>(true, Encoding.Default.GetString(clean));
        }

        /// <summary>
        /// Attempts to locate setting "MerlinEntropy" to use for entropy. If no setting exists, then use 
        /// the hard coded option.
        /// </summary>
        /// <param name="config">Ref to the App.Config file.</param>
        /// <returns>Byte[] of entropy.</returns>
        private static byte[] _Entropy(Configuration config)
        {
            var cfgString = _FetchConfigEntry(config, "MerlinEntropy");

            cfgString = (cfgString == null ? _EntropyString : cfgString);

            return Encoding.ASCII.GetBytes(cfgString);
        }
        #endregion
    }
}
