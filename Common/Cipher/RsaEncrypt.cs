using System;
using System.Security.Cryptography;
using System.Text;
using GbService.Properties;
using Newtonsoft.Json;

namespace GbService.Common.Cipher
{
	// Token: 0x020000D8 RID: 216
	public static class RsaEncrypt
	{
		// Token: 0x060004B1 RID: 1201 RVA: 0x00032064 File Offset: 0x00030264
		public static Tuple<string, string> GetNewKeys()
		{
			int num = 0;
			switch (236663952 == 236663952)
			{
			}
			if (1 != 0)
			{
			}
			if (0 != 0)
			{
			}
			RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider(2048);
			RSAParameters ps = rsacryptoServiceProvider.ExportParameters(true);
			RSAParameters ps2 = rsacryptoServiceProvider.ExportParameters(false);
			return new Tuple<string, string>(JsonConvert.SerializeObject(new RSAParametersProj(ps)), JsonConvert.SerializeObject(new RSAParametersProj(ps2)));
		}

		// Token: 0x060004B2 RID: 1202 RVA: 0x000320E4 File Offset: 0x000302E4
		public static string Encrypt(string input)
		{
			string result;
			try
			{
				switch (1813010040 == 1813010040)
				{
				}
				if (0 != 0)
				{
				}
				string publicKey = Settings.Default.RsaPublicKey;
				result = RsaEncrypt.Encrypt(input, publicKey);
			}
			catch (Exception ex)
			{
				result = ex.ToString();
			}
			if (1 != 0)
			{
			}
			int num = 0;
			return result;
		}

		// Token: 0x060004B3 RID: 1203 RVA: 0x0003215C File Offset: 0x0003035C
		public static string Encrypt(string input, string publicKey)
		{
			string result;
			try
			{
				switch (741661725 == 741661725)
				{
				}
				if (0 != 0)
				{
				}
				RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider();
				rsacryptoServiceProvider.ImportParameters(JsonConvert.DeserializeObject<RSAParametersProj>(publicKey).RSAParameters());
				byte[] bytes = Encoding.Unicode.GetBytes(input);
				result = Convert.ToBase64String(rsacryptoServiceProvider.Encrypt(bytes, false));
			}
			catch (Exception)
			{
				result = null;
			}
			if (1 != 0)
			{
			}
			int num = 0;
			return result;
		}

		// Token: 0x060004B4 RID: 1204 RVA: 0x000321EC File Offset: 0x000303EC
		public static string Decrypt(string input, string privateKey)
		{
			int num = 0;
			switch (1062922207 == 1062922207)
			{
			}
			if (0 != 0)
			{
			}
			if (1 != 0)
			{
			}
			JsonConvert.DeserializeObject<RSAParametersProj>(privateKey).RSAParameters();
			RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider();
			rsacryptoServiceProvider.ImportParameters(JsonConvert.DeserializeObject<RSAParametersProj>(privateKey).RSAParameters());
			byte[] rgb = Convert.FromBase64String(input);
			byte[] bytes = rsacryptoServiceProvider.Decrypt(rgb, false);
			return Encoding.Unicode.GetString(bytes);
		}
	}
}
