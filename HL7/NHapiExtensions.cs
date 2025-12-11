using System;
using NHapi.Base.Model;
using NHapi.Base.Parser;

namespace GbService.HL7
{
	public static class NHapiExtensions
	{
		public static bool SegmentExists(this PipeParser parser, IMessage message, string segment)
		{
			return parser.Encode(message).Contains(segment + "|");
		}
	}
}
