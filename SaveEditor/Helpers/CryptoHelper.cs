using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace AtOSaveEditor.Helpers
{
    public static class CryptoHelper
    {
        public static SaveData DecryptAndDeserialize(string filePath, byte[] key, byte[] iv)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (CryptoStream cs = new CryptoStream(fs, CreateDecryptor(key, iv), CryptoStreamMode.Read))
            {
#pragma warning disable SYSLIB0011
                BinaryFormatter formatter = new BinaryFormatter();
                var result = (SaveData)formatter.Deserialize(cs);
#pragma warning restore SYSLIB0011
                return result;
            }
        }

        public static void SerializeAndEncrypt(SaveData data, string filePath, byte[] key, byte[] iv)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (CryptoStream cs = new CryptoStream(fs, CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
#pragma warning disable SYSLIB0011
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(cs, data);
#pragma warning restore SYSLIB0011
            }
        }

        private static ICryptoTransform CreateDecryptor(byte[] key, byte[] iv)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                return des.CreateDecryptor(key, iv);
            }
        }

        private static ICryptoTransform CreateEncryptor(byte[] key, byte[] iv)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                return des.CreateEncryptor(key, iv);
            }
        }
    }
}