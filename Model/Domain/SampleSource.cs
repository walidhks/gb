using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbService.Model.Domain
{
	public class SampleSource
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long SampleTypeId { get; set; }

		public string SampleTypeName { get; set; }

		public long SampleSourceCode { get; set; }

		public bool IsBloodSource { get; set; }
	}
}
