using System;
using System.ComponentModel.DataAnnotations;

namespace GbService.Model.Domain
{
	public class ParamDict
	{
		public int ParamDictID { get; set; }

		[Required]
		public ParamDictName ParamDictNom { get; set; }

		public string ParamDictValeur { get; set; }
	}
}
