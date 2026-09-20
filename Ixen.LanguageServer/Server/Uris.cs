using System;
using System.IO;
using Ixen.Core.Language.Xnl;
using Ixen.Core.Language.Xns;

namespace Ixen.LanguageServer.Server
{
    internal static class Uris
    {
        public static string ToPath(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return null;
            }

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri parsed) || !parsed.IsFile)
            {
                return null;
            }

            try
            {
                return Path.GetFullPath(parsed.LocalPath);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
            catch (PathTooLongException)
            {
                return null;
            }
        }

        public static string FromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return new Uri(Path.GetFullPath(path)).AbsoluteUri;
            }
            catch (UriFormatException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static bool IsXnl(string path)
        {
            return HasExtension(path, XnlInfos.EXTENSION);
        }

        public static bool IsXns(string path)
        {
            return HasExtension(path, XnsInfos.EXTENSION);
        }

        private static bool HasExtension(string path, string extension)
        {
            return path != null && path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
