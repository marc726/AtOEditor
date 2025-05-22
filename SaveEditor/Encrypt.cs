// Encrypt.cs
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.Serialization;
using SaveEditor;  // for Cryptography.Key/IV and GameData

#pragma warning disable SYSLIB0011

namespace SaveEditor
{
    public static class Encrypt
    {
        // Read XML → GameData → encrypt → .ato
        public static (GameData?, string?) SelectAndEncrypt()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Select XML to encrypt",
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog() != DialogResult.OK)
                return (null, null);

            // 1) Deserialize GameData from XML
            GameData? gd;
            var serializer = new DataContractSerializer(typeof(GameData));
            using (var xmlFs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
            using (var reader = XmlDictionaryReader.CreateTextReader(xmlFs, new XmlDictionaryReaderQuotas()))
            {
                gd = serializer.ReadObject(reader) as GameData;
            }
            if (gd == null)
                throw new InvalidOperationException("Failed to parse XML into GameData.");

            // 2) Encrypt + BinaryFormatter → .ato
            string outPath = Path.ChangeExtension(dlg.FileName, ".ato");
            using var des = DES.Create();
            des.Key = Cryptography.Key;
            des.IV = Cryptography.IV;

            using var fsOut = new FileStream(outPath, FileMode.Create, FileAccess.Write);
            using var crypto = new CryptoStream(fsOut, des.CreateEncryptor(), CryptoStreamMode.Write);
            new BinaryFormatter().Serialize(crypto, gd);
            crypto.FlushFinalBlock();

            return (gd, outPath);
        }
    }
}
