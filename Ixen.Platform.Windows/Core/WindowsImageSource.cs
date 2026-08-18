using Ixen.Core;
using System;
using System.IO;

namespace Ixen.Platform.Windows
{
    public class WindowsImageSource : IImageSource
    {
        private readonly string _root;

        public WindowsImageSource()
            : this(AppContext.BaseDirectory)
        { }

        public WindowsImageSource(string root)
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

            if (!path.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase)
                || !File.Exists(path))
            {
                return null;
            }

            return File.OpenRead(path);
        }
    }
}
