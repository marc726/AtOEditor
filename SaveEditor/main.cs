using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable SYSLIB0011

namespace SaveEditor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            System.Console.Write("Enter 'decrypt' or 'encrypt': ");
            var choice = (System.Console.ReadLine() ?? "").Trim().ToLower();

            if (choice == "decrypt")
            {
                // Binary-decrypt into GameData
                var (gd, path) = Decrypt.SelectAndDecrypt();
                if (gd == null || path == null) return;

                // JSON-serialize with full type info & all members
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

                string json = JsonConvert.SerializeObject(gd, settings);
                var outJson = Path.ChangeExtension(path, ".json");
                File.WriteAllText(outJson, json);

                System.Console.WriteLine($"Decrypted JSON written to {outJson}");
            }
            else if (choice == "encrypt")
            {
                // Pick edited JSON
                using var dlg = new OpenFileDialog
                {
                    Title = "Select edited .json",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (dlg.ShowDialog() != DialogResult.OK) return;

                // Read & deserialize back to GameData
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
                string json = File.ReadAllText(dlg.FileName);
                var gd = JsonConvert.DeserializeObject<GameData>(json, settings)
                         ?? throw new InvalidOperationException("JSON to GameData failed");

                // Binary-re-encrypt
                string outAto = Path.ChangeExtension(dlg.FileName, ".ato");
                using var des = DES.Create();
                des.Key = Cryptography.Key;
                des.IV = Cryptography.IV;

                using var fsOut = new FileStream(outAto, FileMode.Create, FileAccess.Write);
                using var crypto = new CryptoStream(fsOut, des.CreateEncryptor(), CryptoStreamMode.Write);
                new BinaryFormatter().Serialize(crypto, gd);
                crypto.FlushFinalBlock();

                System.Console.WriteLine($"Encrypted save written to {outAto}");
            }
            else
            {
                System.Console.WriteLine("Invalid choice.");
            }
        }
    }
}
