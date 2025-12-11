using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GbService.Common.Cipher
{
    public static class FastCipher
    {
        // Cache the keys so we don't recalculate them 1000 times per second
        private static readonly byte[] CachedKey;
        private static readonly byte[] CachedIV;

        static FastCipher()
        {
            // HARDCODED CONFIGURATION (Derived once at startup)
            string password = "BmL@b2019$$";
            byte[] salt = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }; // "Ivan Medvedev"

            // 1. Expensive derivation happens ONLY ONCE here
            using (var pdb = new Rfc2898DeriveBytes(password, salt, 1000))
            {
                CachedKey = pdb.GetBytes(32);
                CachedIV = pdb.GetBytes(16);
            }
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            // 2. Fast Encryption (reuses cached keys)
            using (var aes = new RijndaelManaged())
            {
                aes.Key = CachedKey;
                aes.IV = CachedIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(inputBytes, 0, inputBytes.Length);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }
}