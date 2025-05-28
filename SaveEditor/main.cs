using System;
using System.IO;

namespace SaveEditor
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {

                if (args.Length == 0)
                {
                    System.Console.WriteLine("Drag and drop a .ato file to decrypt or a .json file to encrypt onto this executable.");
                    System.Console.WriteLine("Press any key to exit...");
                    System.Console.ReadKey();
                    return;
                }

                string filePath = args[0];
                System.Console.WriteLine($"Input file: {filePath}");
                System.Console.WriteLine($"File exists: {File.Exists(filePath)}");

                if (!File.Exists(filePath))
                {
                    System.Console.WriteLine($"File not found: {filePath}");
                    System.Console.WriteLine("Press any key to exit...");
                    System.Console.ReadKey();
                    return;
                }

                string extension = Path.GetExtension(filePath).ToLower();
                System.Console.WriteLine($"File extension: '{extension}'");

                if (extension == ".ato")
                {
                    System.Console.WriteLine("Starting decryption...");
                    Decrypt.DecryptFile(filePath);
                    System.Console.WriteLine("Decryption completed.");
                }
                else if (extension == ".json")
                {
                    System.Console.WriteLine("Starting encryption...");
                    Encrypt.EncryptFile(filePath);
                    System.Console.WriteLine("Encryption completed.");
                }
                else
                {
                    System.Console.WriteLine($"Unsupported file extension: {extension}");
                    System.Console.WriteLine("Supported extensions: .ato (decrypt), .json (encrypt)");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"MAIN ERROR: {ex.GetType().Name}: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadKey();
        }
    }
}