using SkiaSharp;

namespace Ixen.Core
{
    public sealed class CursorImage
    {
        internal CursorImage(string name, SKBitmap bitmap, int hotspotX, int hotspotY)
        {
            Name = name;
            Bitmap = bitmap;
            HotspotX = hotspotX;
            HotspotY = hotspotY;
        }

        public string Name { get; }

        public SKBitmap Bitmap { get; }

        public int HotspotX { get; }

        public int HotspotY { get; }
    }
}
