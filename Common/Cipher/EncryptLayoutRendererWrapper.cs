using System;
using System.Text;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.LayoutRenderers.Wrappers;

namespace GbService.Common.Cipher
{
    [LayoutRenderer("Encrypt")]
    [ThreadAgnostic] // Allows NLog to optimize threading
    public class EncryptLayoutRendererWrapper : WrapperLayoutRendererBase
    {
        protected override string Transform(string text)
        {
            // Use the Optimized FastCipher
            return FastCipher.Encrypt(text);
        }

        public static void Register()
        {
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("Encrypt", typeof(EncryptLayoutRendererWrapper));
        }
    }
}