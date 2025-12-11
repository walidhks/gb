using System;
using System.Security.Cryptography;

namespace GbService.Common.Cipher
{
	// Token: 0x020000DB RID: 219
	[Serializable]
	public class RSAParametersProj
	{
		// Token: 0x060004BA RID: 1210 RVA: 0x000323B8 File Offset: 0x000305B8
		public RSAParametersProj(RSAParameters ps)
		{
			this.Exponent = ps.Exponent;
			this.Modulus = ps.Modulus;
			this.P = ps.P;
			this.Q = ps.Q;
			this.DP = ps.DP;
			this.DQ = ps.DQ;
			this.InverseQ = ps.InverseQ;
			this.D = ps.D;
		}

		// Token: 0x060004BB RID: 1211 RVA: 0x0003242C File Offset: 0x0003062C
		public RSAParameters RSAParameters()
		{
			switch (517732071 == 517732071)
			{
			default:
			{
				int num = 0;
				break;
			}
			case true:
				break;
			}
			if (1 != 0)
			{
			}
			if (0 != 0)
			{
			}
			return new RSAParameters
			{
				Exponent = this.Exponent,
				Modulus = this.Modulus,
				P = this.P,
				Q = this.Q,
				DP = this.DP,
				DQ = this.DQ,
				InverseQ = this.InverseQ,
				D = this.D
			};
		}

		// Token: 0x04000378 RID: 888
		public byte[] Exponent;

		// Token: 0x04000379 RID: 889
		public byte[] Modulus;

		// Token: 0x0400037A RID: 890
		public byte[] P;

		// Token: 0x0400037B RID: 891
		public byte[] Q;

		// Token: 0x0400037C RID: 892
		public byte[] DP;

		// Token: 0x0400037D RID: 893
		public byte[] DQ;

		// Token: 0x0400037E RID: 894
		public byte[] InverseQ;

		// Token: 0x0400037F RID: 895
		public byte[] D;
	}
}
