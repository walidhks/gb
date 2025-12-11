using System;

namespace GbService.Other
{
    // Represents one point on the curve
    public class CurvePoint
    {
        public int X { get; set; }  // Point index: 0, 1, 2, ...
        public int Y { get; set; }  // Height / intensity
    }
}
