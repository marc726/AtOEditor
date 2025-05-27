using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Xml;
using System.Runtime.Serialization;

#pragma warning disable SYSLIB0011

namespace SaveEditor
{
    public static class Encrypt
    {
        public static (GameData?, string?) SelectAndEncrypt()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Select JSON to encrypt",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog() != DialogResult.OK)
                return (null, null);

            GameData? gd;
            var serializer = new DataContractSerializer(typeof(GameData));
            using (var jsonFs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
            using (var reader = XmlDictionaryReader.CreateTextReader(jsonFs, new XmlDictionaryReaderQuotas()))
            {
                gd = serializer.ReadObject(reader) as GameData;
            }
            if (gd == null)
                throw new InvalidOperationException("Failed to parse JSON into GameData.");

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
