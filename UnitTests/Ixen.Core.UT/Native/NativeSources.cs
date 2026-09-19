using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Ixen.Core.UT.Native
{
    internal static class NativeSources
    {
        public static string Read(string relative, string why)
        {
            string path = relative.Replace('\\', Path.DirectorySeparatorChar);
            var directory = new DirectoryInfo(System.AppContext.BaseDirectory);

            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, path);

                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                directory = directory.Parent;
            }

            Assert.Fail($"could not find {relative} by walking up from "
                + $"{System.AppContext.BaseDirectory}. {why}");

            return null;
        }
    }
}
