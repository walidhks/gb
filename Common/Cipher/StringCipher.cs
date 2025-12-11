using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GbService.Common.Cipher
{
	// Token: 0x020000DC RID: 220
	public static class StringCipher
	{
		// Token: 0x060004BC RID: 1212 RVA: 0x000324E8 File Offset: 0x000306E8
		public static string Encrypt(string plainText, string passPhrase)
		{
			switch (0)
			{
			default:
			{
				int num = 0;
				byte[] array = StringCipher.a();
				byte[] array2 = StringCipher.a();
				byte[] bytes = Encoding.UTF8.GetBytes(plainText);
				Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(passPhrase, array, 1000);
				string result;
				try
				{
					byte[] bytes2 = rfc2898DeriveBytes.GetBytes(32);
					RijndaelManaged rijndaelManaged = new RijndaelManaged();
					try
					{
						rijndaelManaged.BlockSize = 256;
						rijndaelManaged.Mode = CipherMode.CBC;
						rijndaelManaged.Padding = PaddingMode.PKCS7;
						ICryptoTransform cryptoTransform = rijndaelManaged.CreateEncryptor(bytes2, array2);
						try
						{
							MemoryStream memoryStream = new MemoryStream();
							try
							{
								CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
								try
								{
									cryptoStream.Write(bytes, 0, bytes.Length);
									cryptoStream.FlushFinalBlock();
									byte[] inArray = array.Concat(array2).ToArray<byte>().Concat(memoryStream.ToArray()).ToArray<byte>();
									memoryStream.Close();
									cryptoStream.Close();
									result = Convert.ToBase64String(inArray);
								}
								finally
								{
									int num2 = 2;
									for (;;)
									{
										switch (num2)
										{
										case 0:
											((IDisposable)cryptoStream).Dispose();
											num2 = 1;
											continue;
										case 1:
											goto IL_120;
										}
										if (cryptoStream == null)
										{
											break;
										}
										num2 = 0;
									}
									IL_120:;
								}
							}
							finally
							{
								int num2 = 1;
								for (;;)
								{
									switch (num2)
									{
									case 0:
										((IDisposable)memoryStream).Dispose();
										num2 = 2;
										continue;
									case 2:
										goto IL_163;
									}
									if (memoryStream == null)
									{
										break;
									}
									num2 = 0;
								}
								IL_163:;
							}
						}
						finally
						{
							int num2 = 1;
							for (;;)
							{
								switch (num2)
								{
								case 0:
									goto IL_1A6;
								case 2:
									cryptoTransform.Dispose();
									num2 = 0;
									continue;
								}
								if (cryptoTransform == null)
								{
									break;
								}
								num2 = 2;
							}
							IL_1A6:;
						}
					}
					finally
					{
						int num2 = 0;
						for (;;)
						{
							switch (num2)
							{
							case 0:
								switch ((1465338928 == 1465338928) ? 1 : 0)
								{
								case 0:
								case 2:
									goto IL_1F2;
								default:
									if (0 != 0)
									{
									}
									break;
								}
								break;
							case 1:
								goto IL_20F;
							case 2:
								((IDisposable)rijndaelManaged).Dispose();
								num2 = 1;
								continue;
							}
							goto IL_1EE;
							IL_1F2:
							num2 = 2;
							continue;
							IL_1EE:
							if (rijndaelManaged != null)
							{
								goto IL_1F2;
							}
							break;
						}
						IL_20F:;
					}
				}
				finally
				{
					int num2 = 0;
					for (;;)
					{
						switch (num2)
						{
						case 1:
							goto IL_250;
						case 2:
							((IDisposable)rfc2898DeriveBytes).Dispose();
							num2 = 1;
							continue;
						}
						if (rfc2898DeriveBytes == null)
						{
							break;
						}
						num2 = 2;
					}
					IL_250:
					if (1 != 0)
					{
					}
				}
				return result;
			}
			}
		}

		// Token: 0x060004BD RID: 1213 RVA: 0x000327D0 File Offset: 0x000309D0
		public static string Decrypt(string cipherText, string passPhrase)
		{
			switch (0)
			{
			default:
			{
				if (1 != 0)
				{
				}
				int num = 0;
				byte[] array = Convert.FromBase64String(cipherText);
				byte[] salt = array.Take(32).ToArray<byte>();
				byte[] rgbIV = array.Skip(32).Take(32).ToArray<byte>();
				byte[] array2 = array.Skip(64).Take(array.Length - 64).ToArray<byte>();
				Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(passPhrase, salt, 1000);
				string @string;
				try
				{
					byte[] bytes = rfc2898DeriveBytes.GetBytes(32);
					RijndaelManaged rijndaelManaged = new RijndaelManaged();
					try
					{
						rijndaelManaged.BlockSize = 256;
						rijndaelManaged.Mode = CipherMode.CBC;
						rijndaelManaged.Padding = PaddingMode.PKCS7;
						ICryptoTransform cryptoTransform = rijndaelManaged.CreateDecryptor(bytes, rgbIV);
						try
						{
							MemoryStream memoryStream = new MemoryStream(array2);
							try
							{
								CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read);
								try
								{
									byte[] array3 = new byte[array2.Length];
									int count = cryptoStream.Read(array3, 0, array3.Length);
									memoryStream.Close();
									cryptoStream.Close();
									@string = Encoding.UTF8.GetString(array3, 0, count);
								}
								finally
								{
									int num2 = 2;
									for (;;)
									{
										switch (num2)
										{
										case 0:
											goto IL_14C;
										case 1:
											((IDisposable)cryptoStream).Dispose();
											num2 = 0;
											continue;
										}
										if (cryptoStream == null)
										{
											break;
										}
										num2 = 1;
									}
									IL_14C:;
								}
							}
							finally
							{
								int num2 = 0;
								for (;;)
								{
									switch (num2)
									{
									case 1:
										goto IL_18F;
									case 2:
										((IDisposable)memoryStream).Dispose();
										num2 = 1;
										continue;
									}
									if (memoryStream == null)
									{
										break;
									}
									num2 = 2;
								}
								IL_18F:;
							}
						}
						finally
						{
							int num2 = 2;
							for (;;)
							{
								switch (num2)
								{
								case 0:
									goto IL_1D2;
								case 1:
									cryptoTransform.Dispose();
									num2 = 0;
									continue;
								}
								if (cryptoTransform == null)
								{
									break;
								}
								num2 = 1;
							}
							IL_1D2:;
						}
					}
					finally
					{
						int num2 = 0;
						for (;;)
						{
							switch (num2)
							{
							case 1:
								goto IL_215;
							case 2:
								((IDisposable)rijndaelManaged).Dispose();
								num2 = 1;
								continue;
							}
							if (rijndaelManaged == null)
							{
								break;
							}
							num2 = 2;
						}
						IL_215:;
					}
				}
				finally
				{
					int num2 = 0;
					for (;;)
					{
						switch (num2)
						{
						case 0:
							switch ((1799613882 == 1799613882) ? 1 : 0)
							{
							case 0:
							case 2:
								goto IL_274;
							default:
								if (0 != 0)
								{
								}
								break;
							}
							break;
						case 1:
							goto IL_27E;
						case 2:
							((IDisposable)rfc2898DeriveBytes).Dispose();
							goto IL_274;
						}
						if (rfc2898DeriveBytes != null)
						{
							num2 = 2;
							continue;
						}
						break;
						IL_274:
						num2 = 1;
					}
					IL_27E:;
				}
				return @string;
			}
			}
		}

		// Token: 0x060004BE RID: 1214 RVA: 0x00032ADC File Offset: 0x00030CDC
		private static byte[] a()
		{
			byte[] array;
			switch ((2124859744 == 2124859744) ? 1 : 0)
			{
			case 0:
			case 2:
				break;
			default:
			{
				if (0 != 0)
				{
				}
				if (1 != 0)
				{
				}
				array = new byte[32];
				RNGCryptoServiceProvider rngcryptoServiceProvider = new RNGCryptoServiceProvider();
				try
				{
					rngcryptoServiceProvider.GetBytes(array);
				}
				finally
				{
					int num = 2;
					for (;;)
					{
						switch (num)
						{
						case 0:
							((IDisposable)rngcryptoServiceProvider).Dispose();
							num = 1;
							continue;
						case 1:
							goto IL_83;
						}
						if (rngcryptoServiceProvider == null)
						{
							break;
						}
						num = 0;
					}
					IL_83:;
				}
				break;
			}
			}
			int num2 = 0;
			return array;
		}

		// Token: 0x060004BF RID: 1215 RVA: 0x00032B8C File Offset: 0x00030D8C
		public static string GenerateKey()
		{
			switch (983362458 == 983362458)
			{
			}
			if (1 != 0)
			{
			}
			if (0 != 0)
			{
			}
			int num = 0;
			return Guid.NewGuid().ToString();
		}
	}
}
