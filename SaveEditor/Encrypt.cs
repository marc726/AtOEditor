using System;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable SYSLIB0011

namespace SaveEditor
{
    public static class Encrypt
    {
        public static void EncryptFile(string jsonPath)
        {
            System.Console.WriteLine($"Encrypting: {Path.GetFileName(jsonPath)}");

            var settings = new JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All,
                ContractResolver = new DefaultContractResolver
                {
                    DefaultMembersSearchFlags =
                        System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                }
            };

            string json = File.ReadAllText(jsonPath);
            var obj = JsonConvert.DeserializeObject(json, settings)
                     ?? throw new InvalidOperationException("JSON deserialization failed");

            string outAto = Path.ChangeExtension(jsonPath, ".ato");
            using var des = DES.Create();
            des.Key = Cryptography.Key;
            des.IV = Cryptography.IV;

            using var fsOut = new FileStream(outAto, FileMode.Create, FileAccess.Write);
            using var crypto = new CryptoStream(fsOut, des.CreateEncryptor(), CryptoStreamMode.Write);
            new BinaryFormatter().Serialize(crypto, obj);
            crypto.FlushFinalBlock();

            System.Console.WriteLine($"Encrypted save written to: {Path.GetFileName(outAto)}");
            System.Console.WriteLine($"Object type: {obj.GetType().Name}");
        }
    }
}