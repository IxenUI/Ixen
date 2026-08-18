using Android.Content.Res;
using Ixen.Core;
using System.IO;

namespace Ixen.View.Android
{
    internal class AssetImageSource : IImageSource
    {
        private readonly AssetManager _assets;

        internal AssetImageSource(AssetManager assets)
        {
            _assets = assets;
        }

        public Stream Open(string name)
        {
            if (_assets == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            try
            {
                return _assets.Open(name.Replace('\\', '/'));
            }
            catch
            {
                return null;
            }
        }
    }
}
