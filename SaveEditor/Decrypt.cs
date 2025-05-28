using System;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable SYSLIB0011

namespace SaveEditor
{
    public static class Decrypt
    {
        public static void DecryptFile(string atoPath)
        {
            try
            {
                System.Console.WriteLine($"Decrypting: {Path.GetFileName(atoPath)}");
                System.Console.WriteLine($"Full path: {atoPath}");

                using var fs = new FileStream(atoPath, FileMode.Open, FileAccess.Read);
                System.Console.WriteLine($"File size: {fs.Length} bytes");

                if (fs.Length == 0)
                {
                    System.Console.WriteLine("File is empty.");
                    return;
                }

                using var des = DES.Create();
                des.Key = Cryptography.Key;
                des.IV = Cryptography.IV;
                System.Console.WriteLine("DES crypto initialized");

                using var crypto = new CryptoStream(fs, des.CreateDecryptor(), CryptoStreamMode.Read);
                System.Console.WriteLine("Starting binary deserialization...");
                var obj = new BinaryFormatter().Deserialize(crypto);
                System.Console.WriteLine($"Deserialized object type: {obj.GetType().FullName}");

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.All,
                    ContractResolver = new DefaultContractResolver
                    {
                        DefaultMembersSearchFlags =
                            System.Reflection.BindingFlags.Public
                          | System.Reflection.BindingFlags.NonPublic
                          | System.Reflection.BindingFlags.Instance
                    }
                };

                System.Console.WriteLine("Starting JSON serialization...");
                string json = JsonConvert.SerializeObject(obj, settings);
                System.Console.WriteLine($"JSON length: {json.Length} characters");

                var outJson = Path.ChangeExtension(atoPath, ".json");
                System.Console.WriteLine($"Output JSON path: {outJson}");

                File.WriteAllText(outJson, json);
                System.Console.WriteLine($"JSON file written successfully");
                System.Console.WriteLine($"File exists after write: {File.Exists(outJson)}");

                System.Console.WriteLine($"Decrypted JSON written to: {Path.GetFileName(outJson)}");
                System.Console.WriteLine($"Object type: {obj.GetType().Name}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DECRYPT ERROR: {ex.GetType().Name}: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}