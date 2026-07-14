using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MerlinXX.Client
{
    public class MerlinConfgurationManager
    {

        public static string GetConnectionString(string key = "Merlin.MySQL.Default", IConfiguration configuration = null)
        {
            var cfg = configuration ?? LoadConfig();

            var conString = cfg.GetConnectionString(key);

            if (conString == null)
            {
                
            }

            return "";
        }



        private static IConfiguration LoadConfig()
        {
            string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            // 1. Create a configuration builder and set the base path to the application's directory
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();
            
        }

        private void UpdateConnectionString(string newConnString, IConfiguration configuration)
        {
            // 1. Cast to IConfigurationRoot to get access to providers
            if (configuration is IConfigurationRoot configRoot)
            {
                // 2. Set the value in memory
                configRoot["ConnectionStrings:DefaultConnection"] = newConnString;

                // 3. (Optional) Force providers like JSON to write back to disk if needed,
                // or manually serialize back to appsettings.json:
                var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

                // Note: Directly modifying raw JSON strings programmatically using a library 
                // like System.Text.Json is usually safer than wiping the whole file.
            }
            else
            {
                throw new Exception("Class is not Root Configuration. Cannot update connection string.");
            }
        }

        #region GOOGLE AI 
        private readonly string _filePath;
        private const string TargetKey = "DefaultConnection";
        private const string PlaceholderValue = "INSERT_YOUR_CONNECTION_STRING_HERE";
        private const string EncryptedPrefix = "DPAPI:";

        public LibraryConfigManager(string appSettingsPath = "appsettings.json")
        {
            // Resolve absolute path relative to the executing library/application assembly
            _filePath = Path.IsPathRooted(appSettingsPath)
                ? appSettingsPath
                : Path.Combine(AppContext.BaseDirectory, appSettingsPath);
        }

        public string GetAPIKey(string Key, string ConfigFile = "appsettings.json")
        {
            // 1. Read existing JSON file completely
            string jsonText = File.Exists(ConfigFile) ? File.ReadAllText(ConfigFile) : "{}";

            var rootNode = JsonNode.Parse(jsonText) ?? new JsonObject();

            // Ensure the "ConnectionStrings" section exists
            if (rootNode["ConnectionStrings"] is not JsonObject connStringsSection)
            {
                connStringsSection = new JsonObject();
                rootNode["ConnectionStrings"] = connStringsSection;
            }

            // 2. Scenario A: Key is entirely missing or untouched placeholder
            if (!connStringsSection.ContainsKey(TargetKey) ||
                connStringsSection[TargetKey]?.ToString() == PlaceholderValue)
            {
                connStringsSection[TargetKey] = PlaceholderValue;
                SaveJson(rootNode);

                throw new InvalidOperationException(
                    $"The connection string key '{TargetKey}' was missing. An empty slot has been generated in '{Path.GetFileName(_filePath)}'. Please populate it.");
            }

            string rawValue = connStringsSection[TargetKey]!.ToString();

            // 3. Scenario B: Key is unencrypted (First Read Auto-Encryption)
            if (!rawValue.StartsWith(EncryptedPrefix))
            {
                string encryptedValue = EncryptString(rawValue);
                connStringsSection[TargetKey] = $"{EncryptedPrefix}{encryptedValue}";
                SaveJson(rootNode);

                return rawValue; // Return plain text for immediate use on this initial run
            }

            // 4. Scenario C: Key is already securely encrypted
            try
            {
                string base64Cipher = rawValue.Substring(EncryptedPrefix.Length);
                return DecryptString(base64Cipher);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to decrypt the connection string. Ensure the string is decrypted on the same machine/user context it was encrypted on.", ex);
            }
        }

        private string EncryptString(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            // DataProtectionScope.CurrentUser ties encryption to the OS user profile account
            // DataProtectionScope.LocalMachine allows any process on the machine to decrypt it
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        private string DecryptString(string base64Cipher)
        {
            byte[] cipherBytes = Convert.FromBase64String(base64Cipher);
            byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private void SaveJson(JsonNode node)
        {
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_filePath, node.ToJsonString(options));
        }
        #endregion
    }
}
