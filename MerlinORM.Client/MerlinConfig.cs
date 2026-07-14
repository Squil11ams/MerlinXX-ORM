using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace MerlinORM.Client
{
    public static class MerlinConfig
    {
        /// <summary>
        /// Caches decrypted connection strings to avoid repeated decryption operations for the same key.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> _cached = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Default placeholder value used in the configuration file to indicate that a connection string has not yet been provided.
        /// </summary>
        private const string PlaceholderValue = "INSERT_YOUR_CONNECTION_STRING_HERE";

        /// <summary>
        /// Encrypted prefix used to identify connection strings that have been encrypted. This prefix is prepended to the encrypted value in the configuration file.
        /// </summary>
        private const string EncryptedPrefix = "Merlin:";

        /// <summary>
        /// Thread-safe lock object to ensure that configuration file access and modification is synchronized across multiple threads,
        /// preventing concurrent reads/writes during initialization and encryption updates.
        /// </summary>
        private static readonly System.Threading.Lock _configLock = new();

        /// <summary>
        /// Get the connection string associated with the provided key from the appsettings.json file. If the connection string is not found or is a placeholder, it will create an entry in the configuration file and throw an exception prompting the user to populate it. If the connection string is found but not encrypted, it will encrypt it and save it back to the file. If it is already encrypted, it will decrypt and return the plain text value.
        /// </summary>
        /// <param name="TargetKey"></param>
        /// <param name="appSettingsPath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetConnectionString(string TargetKey, string appSettingsPath = "appsettings.json")
        {
            if (_cached.TryGetValue(TargetKey, out var cachedValue))
            {
                return cachedValue;
            }

            // 1. Get Full Path to appsettings.json
            var file = ProcessFilePath(appSettingsPath);

            lock (_configLock)
            {
                if (_cached.TryGetValue(TargetKey, out var cachedValue2))
                {
                    return cachedValue2;
                }

                // 2. Load the JSON configuration file
                var rootNode = LoadRoot(file);

                // 3. Load the ConnectionStrings section (create if it doesn't exist)
                var connStringsSection = GetConnectionStringsSection(rootNode);

                // 4. Check for the target key and handle missing or placeholder scenarios
                // (Throws an exception if the key is missing or still has the placeholder value)
                EnsureConnectionStringExists(rootNode, connStringsSection, TargetKey, file);

                // 5. Retrieve the raw value for the target key
                string rawValue = connStringsSection[TargetKey]!.GetValue<string>();

                // 6. Check if string starts with the encrypted prefix. If not, encrypt it and save back to the file.
                if (!rawValue.StartsWith(EncryptedPrefix))
                {
                    string encryptedValue = EncryptString(rawValue);
                    connStringsSection[TargetKey] = $"{EncryptedPrefix}{encryptedValue}";
                    SaveJson(rootNode, file);

                    _cached[TargetKey] = rawValue;

                    return rawValue; // Return plain text for immediate use on this initial run
                }
                else
                {
                    try
                    {
                        string base64Cipher = rawValue.Substring(EncryptedPrefix.Length);
                        var decrypted = DecryptString(base64Cipher);

                        _cached[TargetKey] = decrypted;
                        return decrypted;
                    }
                    catch (Exception ex)
                    {
                        throw new MerlinException("MERLIN-CFG-1026", ex);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appSettingsPath"></param>
        /// <returns></returns>
        private static string ProcessFilePath(string appSettingsPath)
        {
            return Path.IsPathRooted(appSettingsPath)
                ? appSettingsPath
                : Path.Combine(AppContext.BaseDirectory, appSettingsPath);
        }

        /// <summary>
        /// Loads the root JSON object from the specified appsettings.json file. If the file does not exist, it returns an empty JSON object.
        /// </summary>
        /// <param name="appSettingsPath"></param>
        /// <returns></returns>
        private static JsonObject LoadRoot(string appSettingsPath)
        {
            string jsonText = File.Exists(appSettingsPath) ? File.ReadAllText(appSettingsPath) : "{}";
            return JsonNode.Parse(jsonText) as JsonObject ?? new JsonObject();
        }

        /// <summary>
        /// Gets the "ConnectionStrings" section from the root JSON object. If the section does not exist, it creates a new empty section and adds it to the root.
        /// </summary>
        /// <param name="rootNode"></param>
        /// <returns></returns>
        private static JsonObject GetConnectionStringsSection(JsonObject rootNode)
        {
            if (rootNode["ConnectionStrings"] is not JsonObject connStringsSection)
            {
                connStringsSection = new JsonObject();
                rootNode["ConnectionStrings"] = connStringsSection;
            }

            return connStringsSection;
        }

        /// <summary>
        /// Makes sure that the specified target key exists in the connection strings section. If it does not exist or has a placeholder value, it creates an entry with the placeholder and saves the configuration file, then throws an exception to prompt the user to populate it.
        /// </summary>
        /// <param name="rootNode"></param>
        /// <param name="connStringsSection"></param>
        /// <param name="targetKey"></param>
        /// <param name="file"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private static void EnsureConnectionStringExists(JsonObject rootNode, JsonObject connStringsSection, string targetKey, string file)
        {
            if (!connStringsSection.ContainsKey(targetKey) || connStringsSection[targetKey]?.ToString() == PlaceholderValue)
            {
                connStringsSection[targetKey] = PlaceholderValue;

                SaveJson(rootNode, file);

                var msg = $"The connection string key '{targetKey}' was missing. An empty slot has been generated in '{Path.GetFileName(file)}'. Please populate it.";

                throw new MerlinException("MERLIN-CFG-1027", msg);
            }
        }

        /// <summary>
        /// Encrypts the provided plain text string using the Windows Data Protection API (DPAPI) with machine-level scope, and returns the encrypted value as a Base64-encoded string. This ensures that the encrypted data can only be decrypted on the same machine.
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        private static string EncryptString(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Encrypts the provided Base64-encoded cipher text string using the Windows Data Protection API (DPAPI) with machine-level scope, and returns the decrypted plain text string. This ensures that the encrypted data can only be decrypted on the same machine.
        /// </summary>
        /// <param name="base64Cipher"></param>
        /// <returns></returns>
        private static string DecryptString(string base64Cipher)
        {
            byte[] cipherBytes = Convert.FromBase64String(base64Cipher);
            byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Writes the provided JSON node to the specified file path in a safe and efficient manner, ensuring that the directory exists and using a low-level UTF-8 JSON writer with indentation for readability.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="filePath"></param>
        private static void SaveJson(JsonNode node, string filePath)
        {
            // Configure safe, low-level writer options
            var writerOptions = new System.Text.Json.JsonWriterOptions
            {
                Indented = true
            };

            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Open a direct file write stream
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new System.Text.Json.Utf8JsonWriter(fileStream, writerOptions))
            {
                // Write the DOM node layout straight to disk without using reflection serializers
                node.WriteTo(writer);
                writer.Flush();
            }
        }
    }
}