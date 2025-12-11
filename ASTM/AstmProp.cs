using System;
using System.Collections.Generic;
using GbService.Communication;

namespace GbService.ASTM
{
    public class AstmProp
    {
        public static AstmProp Create(Jihas kind)
        {

            if (kind == Jihas.Indiko)
            {
                // Default ASTM delimiters: Field=| Repeat=\ Component=^ Escape=&
                return new AstmProp(new string[] { "|", "\\", "^", "&" });
            }

            string[] x;
            AstmProp._dict.TryGetValue(kind, out x);
            // If not found in dict (e.g. new instrument), return default prop
            return new AstmProp(x);
        }

        public AstmProp(string[] x = null)
        {
            bool flag = x == null;
            if (flag)
            {
                this.Version = " ";
                this.Sender = " ";
                this.Receiver = " ";

                this.Separator = '\\'; // Default Repeat Separator
                this.Repeater = '^';   // Default Component Separator
                this.Delimiter = '&';  // Default Escape Separator
            }
            else
            {
                this.Receiver = x[0];
                this.Version = x[1];
                this.Separator = x.Length >= 3 ? Convert.ToChar(x[2]) : '\\';
                this.Sender = x.Length >= 4 ? x[3] : "5555";
                this.Repeater = x.Length >= 5 ? Convert.ToChar(x[4]) : '^';
                this.Delimiter = x.Length >= 6 ? Convert.ToChar(x[5]) : '&';
            }

        }

        public char Delimiter; // Escape (&)
        public string Receiver;
        public string Sender;
        public string Version;
        public char Separator; // Repeat (\)
        public char Repeater;  // Component (^)
        // Note: Field Separator (|) is usually hardcoded in AstmHigh.cs or derived from context

        private static Dictionary<Jihas, string[]> _dict = new Dictionary<Jihas, string[]>
        {
            // -----------------------------------------------------
            // AUTOBIO ASTM CONFIGURATION
            // Manual Page 37: Version "1394-97", Delimiters | \ ^ &
            // -----------------------------------------------------
            {
                Jihas.Autobio,
                new string[]
                {
                    "Host",      // x[0] Receiver
                    "1394-97",   // x[1] Version (No 'E')
                    "\\",        // x[2] Separator (Repeat)
                    "Autobio",   // x[3] Sender
                    "^",         // x[4] Repeater (Component)
                    "&"          // x[5] Delimiter (Escape)
                }
            },
            // -----------------------------------------------------

            {
                Jihas.Access2,
                new string[] { "Access2_1", "E 1394-97", "\\" }
            },
            {
                Jihas.Acl,
                new string[] { "ACL9000", "1", "\\" }
            },
            {
                Jihas.Indiko,
                new string[] 
                {
                    "LIS",      // x[0] Receiver
                    "Indiko",   // x[1] Version (No 'E')
                    "1394-97",        // x[2] Separator (Repeat)
                    "\\",   // // Standard repeat separator     
                }
            },
            {
                Jihas.Aia360,
                new string[] { "AIA360_1", "E 1394-97", "\\" }
            },
            {
                Jihas.Arkray,
                new string[] { "Arkray", "1", "$" }
            },
            {
                Jihas.Biorad10,
                new string[] { "Arkray", "1", "$" }
            },
            {
                Jihas.Maglumi,
                new string[] { "Lis", "E1394-97", "\\", "Maglumi User" }
            },
            {
                Jihas.Response,
                new string[] { "A1", "E 1394-97", "`" }
            },
            {
                Jihas.SelectraProM,
                new string[] { "1111", "LIS2-A", "\\" }
            },
            {
                Jihas.SysmexCA600,
                new string[] { "CA-600", "", "\\" }
            },
            {
                Jihas.Acl9000,
                new string[] { "ACL9000", "1", "\\" }
            },
            {
                Jihas.SysmexXS_XN,
                new string[] { "XS", "", "\\" }
            },
            {
                Jihas.VitrosEciq,
                new string[] { "VitrosECQ", "E 1394-97", "\\" }
            },
            {
                Jihas.DXH800,
                new string[] { "DX800", "1", "\\", null, "!", "~" }
            },
            {
                Jihas.LiaisonXL,
                new string[] { "123", "", "\\" }
            },
            {
                Jihas.CobasE411,
                new string[] { "Cobas411", "E 1394-97", "\\" }
            },
            {
                Jihas.Bioflash,
                new string[] { "Alba00000403", "1394-97", "@", "BMLab", "^", "\\" }
            },
            {
                Jihas.Iris,
                new string[] { "IrisIQ200", "E 1394-97", "`" }
            },
            {
                Jihas.Spa,
                new string[] { "Prestige24i^SYSTEM1", "1", "\\", "HOST^P_1" }
            },
            {
                Jihas.StaSatelit,
                new string[] { "", "1.00", "\\", "99^2.00" }
            },
            {
                Jihas.StagoStaMax,
                new string[] { "", "LIS2-A2", "\\", "STA-Compact MAX^99^3.00" }
            },
            {
                Jihas.Architect,
                new string[] { "ARCHITECT^7.00^F5260044500^H1P1O1R1C1Q1L1", "1" }
            },
            {
                Jihas.Vitros3600,
                new string[] { "Vitros3600", "E 1394-97", "\\" }
            },
            {
                Jihas.Vitros4600,
                new string[] { "Vitros4600", "E 1394-97", "\\" }
            },
            {
                Jihas.CobasC311,
                new string[] { "c", "", "\\", "host" }
            },
            {
                Jihas.CobasC111,
                new string[] { "c", "", "\\", "host" }
            }
        };
    }
}