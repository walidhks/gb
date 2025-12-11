using System;

namespace GbService
{
    internal class Program
    {
        private static void Main()
        {
            // [REMOVED] EncryptLayoutRendererWrapper.Register(); 
            // We removed this because we are not using encryption.

            BasicServiceStarter.Run<GbService>("BMService");
        }
    }
}