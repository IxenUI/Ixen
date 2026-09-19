using Ixen.Core;
using System;
using System.IO;

namespace Ixen.Platform.Linux
{
    public class LinuxImageSource : IImageSource
    {
        private readonly string _root;

        public LinuxImageSource()
            : this(AppContext.BaseDirectory)
        { }

        public LinuxImageSource(string root)
        {
            _root = root;
        }

        public Stream Open(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string path = Path.GetFullPath(Path.Combine(_root, name));

            if (!path.StartsWith(Path.GetFullPath(_root), StringComparison.Ordinal)
                || !File.Exists(path))
            {
                return null;
            }

            return File.OpenRead(path);
        }
    }
}
