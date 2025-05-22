using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;


#pragma warning disable SYSLIB0011

namespace SaveEditor
{
    public static class Decrypt
    {
        public static (GameData?, string?) SelectAndDecrypt()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Select .ato to decrypt",
                Filter = "ATO files (*.ato)|*.ato|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog() != DialogResult.OK)
                return (null, null);

            using var fs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
            if (fs.Length == 0)
                return (null, dlg.FileName);

            using var des = DES.Create();
            des.Key = Cryptography.Key;
            des.IV = Cryptography.IV;

            using var crypto = new CryptoStream(fs, des.CreateDecryptor(), CryptoStreamMode.Read);
            var obj = new BinaryFormatter().Deserialize(crypto);
            if (obj is GameData gd)
                return (gd, dlg.FileName);

            throw new InvalidCastException($"Expected GameData, got {obj.GetType()}");
        }
    }
}
