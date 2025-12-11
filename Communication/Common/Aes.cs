using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NLog;

namespace GbService.Communication.Common
{
	public class Aes
	{
		public static string Test()
		{
			string result;
			try
			{
				string text = "Here is some data to encrypt!";
				using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
				{
					byte[] cipherText = Aes.EncryptStringToBytes_Aes(text, aes.Key, aes.IV);
					string str = Aes.DecryptStringFromBytes_Aes(cipherText, aes.Key, aes.IV);
					result = "Original:   " + text + ", Round Trip: " + str;
				}
			}
			catch (Exception arg)
			{
				result = string.Format("Error: {0}", arg);
			}
			return result;
		}

		public static string Test1()
		{
			string result;
			try
			{
				string text = "Here is some data to encrypt!";
				string key = "1455887ddfdfkd,à1455887ddfdfkd,à";
				string iv = "hsgdudè879;,sr'&";
				string plainText = Aes.EncryptString_Aes(text, key, iv);
				string str = Aes.DecryptString_Aes(plainText, key, iv);
				result = "Original:   " + text + ", Round Trip: " + str;
			}
			catch (Exception arg)
			{
				result = string.Format("Error: {0}", arg);
			}
			return result;
		}

		private static string EncryptString_Aes(string plainText, string key, string iv)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(key);
			byte[] bytes2 = Encoding.ASCII.GetBytes(iv);
			byte[] bytes3 = Aes.EncryptStringToBytes_Aes(plainText, bytes, bytes2);
			return Encoding.ASCII.GetString(bytes3);
		}

		private static string DecryptString_Aes(string plainText, string key, string iv)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(key);
			byte[] bytes2 = Encoding.ASCII.GetBytes(iv);
			byte[] bytes3 = Encoding.ASCII.GetBytes(plainText);
			return Aes.DecryptStringFromBytes_Aes(bytes3, bytes, bytes2);
		}

		private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] iv)
		{
			bool flag = plainText == null || plainText.Length <= 0;
			if (flag)
			{
				throw new ArgumentNullException("plainText");
			}
			bool flag2 = key == null || key.Length == 0;
			if (flag2)
			{
				throw new ArgumentNullException("key");
			}
			bool flag3 = iv == null || iv.Length == 0;
			if (flag3)
			{
				throw new ArgumentNullException("iv");
			}
			byte[] result;
			using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
			{
				aes.Key = key;
				aes.IV = iv;
				ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
				using (MemoryStream memoryStream = new MemoryStream())
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
					{
						using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
						{
							streamWriter.Write(plainText);
						}
						result = memoryStream.ToArray();
					}
				}
			}
			return result;
		}

		private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key, byte[] iv)
		{
			bool flag = cipherText == null || cipherText.Length == 0;
			if (flag)
			{
				throw new ArgumentNullException("cipherText");
			}
			bool flag2 = key == null || key.Length == 0;
			if (flag2)
			{
				throw new ArgumentNullException("key");
			}
			bool flag3 = iv == null || iv.Length == 0;
			if (flag3)
			{
				throw new ArgumentNullException("iv");
			}
			string result;
			using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
			{
				aes.Key = key;
				aes.IV = iv;
				ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
				using (MemoryStream memoryStream = new MemoryStream(cipherText))
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read))
					{
						using (StreamReader streamReader = new StreamReader(cryptoStream))
						{
							result = streamReader.ReadToEnd();
						}
					}
				}
			}
			return result;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
