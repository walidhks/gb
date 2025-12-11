using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GbService.Other
{
    public static class H9GraphHelper
    {
        /// <summary>
        /// Parse the current complex Graph string from H9 and
        /// extract a simpler list of points (X,Y).
        /// </summary>
        public static List<CurvePoint> ParseFullGraph(string graphText)
        {
            var points = new List<CurvePoint>();

            if (string.IsNullOrWhiteSpace(graphText))
                return points;

            // Split on TAB and SPACE
            string[] tokens = graphText
                .Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int idx = 0;

            // 1) Skip "Points:" label if present
            if (idx < tokens.Length &&
                tokens[idx].StartsWith("Points", StringComparison.OrdinalIgnoreCase))
            {
                idx++;
            }

            // 2) Skip point count if present (first number after "Points:")
            if (idx < tokens.Length && int.TryParse(tokens[idx], out _))
            {
                idx++;
            }

            // 3) Convert all remaining tokens to integers
            var numbers = new List<int>();
            for (int i = idx; i < tokens.Length; i++)
            {
                if (int.TryParse(tokens[i], out int n))
                {
                    numbers.Add(n);
                }
            }

            // 4) Numbers are in groups of 6:
            // [0, something, something, something, something, amplitude]
            // We use the LAST value as the Y (height).
            int x = 0;
            int pos = 0;

            while (pos + 5 < numbers.Count)
            {
                // Expect a group starting with 0
                if (numbers[pos] != 0)
                {
                    // If format changes or becomes misaligned, stop
                    break;
                }

                int amplitude = numbers[pos + 5];

                points.Add(new CurvePoint
                {
                    X = x,
                    Y = amplitude
                });

                x++;
                pos += 6;  // Move to next group
            }

            return points;
        }

        /// <summary>
        /// Build a simpler string for storage, e.g.:
        /// "Points:\t29\t30\t30\t36\t42\t47\t..."
        /// </summary>
        public static string BuildSimpleGraphString(string fullGraph)
        {
            var pts = ParseFullGraph(fullGraph);

            if (pts.Count == 0)
                return null;

            var sb = new StringBuilder();
            sb.Append("Points:\t");
            sb.Append(pts.Count);

            foreach (var p in pts)
            {
                sb.Append('\t');
                sb.Append(p.Y);
            }

            return sb.ToString();
        }
    }
}
