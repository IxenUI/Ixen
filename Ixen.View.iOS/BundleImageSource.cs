using Foundation;
using Ixen.Core;
using System;
using System.IO;

namespace Ixen.View.iOS
{
    internal class BundleImageSource : IImageSource
    {
        private readonly string _root;

        internal BundleImageSource(string root)
        {
            _root = root == null ? null : Path.GetFullPath(root);
        }

        internal static BundleImageSource ForMainBundle() => new BundleImageSource(NSBundle.MainBundle?.BundlePath);

        public Stream Open(string name)
        {
            if (_root == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            try
            {
                string path = Path.GetFullPath(Path.Combine(_root, name.Replace('\\', '/')));
                string prefix = _root.EndsWith("/", StringComparison.Ordinal) ? _root : _root + "/";

                if (!path.StartsWith(prefix, StringComparison.Ordinal) || !File.Exists(path))
                {
                    return null;
                }

                return File.OpenRead(path);
            }
            catch
            {
                return null;
            }
        }
    }
}
