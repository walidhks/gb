using System;
using GbService.ASTM;

namespace GbService.Communication.Common
{
	public class Frame
	{
		public Frame(FrameKind kind, string value)
		{
			this.Kind = kind;
			this.Value = value;
		}

		public FrameKind Kind { get; set; }

		public string Value { get; set; }
	}
}
