using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BaitapBaoMat
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== BÀI TẬP BẢO MẬT THÔNG TIN ===");
            Console.WriteLine("1. Bài 1: Mã hóa AES (Đối xứng)");
            Console.WriteLine("2. Bài 2: Mã hóa RSA, Băm SHA256 & Chữ ký số");
            Console.Write("Chọn chức năng (1 hoặc 2): ");

            string choice = Console.ReadLine();
            Console.WriteLine("\n=======================================\n");

            if (choice == "1") RunBai1();
            else if (choice == "2") RunBai2();
            else Console.WriteLine("Lựa chọn không hợp lệ.");

            Console.ReadLine();
        }

        static void RunBai1()
        {
            Console.WriteLine("=== BÀI 1: MÀ HÓA AES (ĐỐI XỨNG) ===");
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();
            File.WriteAllBytes("aes_key.bin", aes.Key);
            File.WriteAllBytes("aes_iv.bin", aes.IV);

            byte[] key = File.ReadAllBytes("aes_key.bin");
            byte[] iv = File.ReadAllBytes("aes_iv.bin");

            string originalText = "Day la doan van ban bi mat can ma hoa.";
            byte[] encryptedText = EncryptStringToBytes_Aes(originalText, key, iv);
            Console.WriteLine("Mã hóa văn bản (Base64): " + Convert.ToBase64String(encryptedText));
            Console.WriteLine("Giải mã văn bản: " + DecryptStringFromBytes_Aes(encryptedText, key, iv));

            string sourceFile = "source.dat";
            string encryptedFile = "encrypted.dat";
            string decryptedFile = "decrypted.dat";

            if (!File.Exists(sourceFile))
            {
                byte[] dummyData = new byte[1024 * 1024 * 50];
                new Random().NextBytes(dummyData);
                File.WriteAllBytes(sourceFile, dummyData);
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            EncryptFile(sourceFile, encryptedFile, key, iv);
            sw.Stop();
            Console.WriteLine($"Thời gian mã hóa file 50MB: {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            DecryptFile(encryptedFile, decryptedFile, key, iv);
            sw.Stop();
            Console.WriteLine($"Thời gian giải mã file 50MB: {sw.ElapsedMilliseconds} ms");

            bool isMatch = CompareFilesByHash(sourceFile, decryptedFile);
            Console.WriteLine($"Kết quả so sánh byte: {(isMatch ? "KHỚP NHAU HOÀN TOÀN" : "SAI LỆCH")}");
        }

        static void RunBai2()
        {
            Console.WriteLine("=== BÀI 2: MÃ HÓA RSA, BĂM SHA256 & CHỮ KÝ SỐ ===");
            using RSA rsa = RSA.Create(2048);
            File.WriteAllText("public.pem", rsa.ExportRSAPublicKeyPem());
            File.WriteAllText("private.pem", rsa.ExportRSAPrivateKeyPem());

            using RSA rsaPub = RSA.Create();
            rsaPub.ImportFromPem(File.ReadAllText("public.pem"));
            using RSA rsaPriv = RSA.Create();
            rsaPriv.ImportFromPem(File.ReadAllText("private.pem"));

            byte[] dataToEncrypt = new byte[32];
            new Random().NextBytes(dataToEncrypt);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            byte[] encryptedData = rsaPub.Encrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA256);
            sw.Stop();
            long encTimeRsa = sw.ElapsedTicks;

            sw.Restart();
            byte[] decryptedData = rsaPriv.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
            sw.Stop();

            Console.WriteLine($"Mã hóa RSA thành công. Data gốc == Data giải mã: {dataToEncrypt.SequenceEqual(decryptedData)}");
            Console.WriteLine($"Tốc độ: Mã hóa tốn {encTimeRsa} ticks | Giải mã tốn {sw.ElapsedTicks} ticks.");

            Console.WriteLine("\n--- Hiệu ứng tuyết lở SHA256 ---");
            Console.WriteLine($"Hash 'Hello World1': {Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("Hello World1")))}");
            Console.WriteLine($"Hash 'Hello World2': {Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("Hello World2")))}");

            Console.WriteLine("\n--- Chữ ký số động (RSA-PSS) ---");
            byte[] dataToSign = Encoding.UTF8.GetBytes("Van ban quan trong");
            byte[] sig1 = rsaPriv.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            byte[] sig2 = rsaPriv.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

            Console.WriteLine($"Ký lần 1: {Convert.ToBase64String(sig1).Substring(0, 20)}...");
            Console.WriteLine($"Ký lần 2: {Convert.ToBase64String(sig2).Substring(0, 20)}...");
            Console.WriteLine($"Chữ ký 1 giống chữ ký 2 không? : {sig1.SequenceEqual(sig2)}");
            Console.WriteLine($"Verify 1: {rsaPub.VerifyData(dataToSign, sig1, HashAlgorithmName.SHA256, RSASignaturePadding.Pss)}");
            Console.WriteLine($"Verify 2: {rsaPub.VerifyData(dataToSign, sig2, HashAlgorithmName.SHA256, RSASignaturePadding.Pss)}");
        }

        // --- Các hàm hỗ trợ AES ---
        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            using Aes aesAlg = Aes.Create();
            aesAlg.Key = Key; aesAlg.IV = IV;
            using MemoryStream msEncrypt = new MemoryStream();
            using CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write);
            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) swEncrypt.Write(plainText);
            return msEncrypt.ToArray();
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            using Aes aesAlg = Aes.Create();
            aesAlg.Key = Key; aesAlg.IV = IV;
            using MemoryStream msDecrypt = new MemoryStream(cipherText);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, aesAlg.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }

        static void EncryptFile(string inputFile, string outputFile, byte[] key, byte[] iv)
        {
            using FileStream fsIn = new FileStream(inputFile, FileMode.Open);
            using FileStream fsOut = new FileStream(outputFile, FileMode.Create);
            using Aes aes = Aes.Create();
            aes.Key = key; aes.IV = iv;
            using CryptoStream cs = new CryptoStream(fsOut, aes.CreateEncryptor(), CryptoStreamMode.Write);
            fsIn.CopyTo(cs);
        }

        static void DecryptFile(string inputFile, string outputFile, byte[] key, byte[] iv)
        {
            using FileStream fsIn = new FileStream(inputFile, FileMode.Open);
            using FileStream fsOut = new FileStream(outputFile, FileMode.Create);
            using Aes aes = Aes.Create();
            aes.Key = key; aes.IV = iv;
            using CryptoStream cs = new CryptoStream(fsIn, aes.CreateDecryptor(), CryptoStreamMode.Read);
            cs.CopyTo(fsOut);
        }

        static bool CompareFilesByHash(string file1, string file2)
        {
            using var sha = SHA256.Create();
            using var fs1 = File.OpenRead(file1);
            using var fs2 = File.OpenRead(file2);
            return sha.ComputeHash(fs1).SequenceEqual(sha.ComputeHash(fs2));
        }
    }
}