using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AtOSaveEditor.Models;

namespace AtOSaveEditor.Helpers
{
    /// <summary>
    /// Custom SerializationBinder to handle type forwarding from old namespace to new namespace
    /// </summary>
    sealed class TypeForwardingBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            // If looking for the old namespace, redirect to new namespace
            if (typeName == "AtOSaveEditor.SaveData")
            {
                return typeof(AtOSaveEditor.Models.SaveData);
            }

            // For all other types, use default behavior
            return Type.GetType($"{typeName}, {assemblyName}");
        }
    }

    public static class CryptoHelper
    {
        private const int BufferSize = 4096;
        private static readonly SerializationBinder _binder = new TypeForwardingBinder();

        public static SaveData DecryptAndDeserialize(string filePath, byte[] key, byte[] iv)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (DES des = DES.Create())
            {
                des.Key = key;
                des.IV = iv;
                using (ICryptoTransform decryptor = des.CreateDecryptor())
                using (CryptoStream cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                {
#pragma warning disable SYSLIB0011 // Binary formatter is obsolete but needed for this app
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Binder = _binder;
                    var result = (SaveData)formatter.Deserialize(cs);
#pragma warning restore SYSLIB0011
                    return result;
                }
            }
        }

        public static void SerializeAndEncrypt(SaveData data, string filePath, byte[] key, byte[] iv)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (DES des = DES.Create())
            {
                des.Key = key;
                des.IV = iv;
                using (ICryptoTransform encryptor = des.CreateEncryptor())
                using (CryptoStream cs = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
                {
#pragma warning disable SYSLIB0011 // Binary formatter is obsolete but needed for this app
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Binder = _binder;
                    formatter.Serialize(cs, data);
#pragma warning restore SYSLIB0011
                    cs.FlushFinalBlock();
                }
            }
        }

        public static async Task<SaveData> DecryptAndDeserializeAsync(string filePath, byte[] key, byte[] iv,
            IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            progress?.Report(0);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                BufferSize, FileOptions.Asynchronous))
            using (DES des = DES.Create())
            {
                des.Key = key;
                des.IV = iv;
                using (ICryptoTransform decryptor = des.CreateDecryptor())
                using (CryptoStream cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                using (MemoryStream ms = new MemoryStream())
                {
                    // Read in chunks and report progress
                    byte[] buffer = new byte[BufferSize];
                    int bytesRead;
                    long totalBytesRead = 0;
                    long fileLength = fs.Length;

                    while ((bytesRead = await cs.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await ms.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                        totalBytesRead += bytesRead;
                        int progressValue = (int)((double)totalBytesRead / fileLength * 75); // 75% for reading
                        progress?.Report(progressValue);

                        // Give UI a chance to process
                        if (totalBytesRead % (BufferSize * 10) == 0)
                            await Task.Delay(1, cancellationToken);
                    }

                    ms.Position = 0;
#pragma warning disable SYSLIB0011 // Binary formatter is obsolete but needed for this app
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Binder = _binder;
                    progress?.Report(90);
                    var result = await Task.Run(() =>
                    {
                        var data = (SaveData)formatter.Deserialize(ms);

                        // Re-parse TeamAtO with proper JSON settings
                        if (!string.IsNullOrEmpty(data.TeamAtO))
                        {
                            var jsonOptions = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = false,
                                WriteIndented = true
                            };
                            var teamData = JsonSerializer.Deserialize<TeamAtO>(data.TeamAtO, jsonOptions);
                            if (teamData != null)
                            {
                                data.TeamAtO = JsonSerializer.Serialize(teamData, jsonOptions);
                            }
                        }
                        return data;
                    }, cancellationToken);
#pragma warning restore SYSLIB0011

                    progress?.Report(100);
                    return result;
                }
            }
        }

        public static async Task SerializeAndEncryptAsync(SaveData data, string filePath, byte[] key, byte[] iv,
            IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            progress?.Report(0);

            using (MemoryStream ms = new MemoryStream())
            {
#pragma warning disable SYSLIB0011 // Binary formatter is obsolete but needed for this app
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Binder = _binder;
                formatter.Serialize(ms, data);
#pragma warning restore SYSLIB0011

                progress?.Report(30);
                ms.Position = 0;

                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                        BufferSize, FileOptions.Asynchronous))
                using (DES des = DES.Create())
                {
                    des.Key = key;
                    des.IV = iv;
                    using (ICryptoTransform encryptor = des.CreateEncryptor())
                    using (CryptoStream cs = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
                    {
                        // Write in chunks and report progress
                        byte[] buffer = new byte[BufferSize];
                        int bytesRead;
                        long totalBytesRead = 0;
                        long dataLength = ms.Length;

                        while ((bytesRead = await ms.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            await cs.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                            totalBytesRead += bytesRead;
                            int progressValue = 30 + (int)((double)totalBytesRead / dataLength * 70); // 70% for writing
                            progress?.Report(progressValue);

                            // Give UI a chance to process
                            if (totalBytesRead % (BufferSize * 10) == 0)
                                await Task.Delay(1, cancellationToken);
                        }

                        await cs.FlushFinalBlockAsync(cancellationToken);
                        progress?.Report(100);
                    }
                }
            }
        }
    }
}