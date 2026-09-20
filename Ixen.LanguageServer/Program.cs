using System;
using Ixen.LanguageServer.Protocol;

namespace Ixen.LanguageServer
{
    internal static class Program
    {
        private static int Main(string[] arguments)
        {
            Connection connection = new Connection(Console.OpenStandardInput(), Console.OpenStandardOutput());

            return new Server.Server(connection).Run();
        }
    }
}
