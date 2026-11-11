using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class ImageShrinkTests
    {
        private const int SIDE = 1200;
        private const long MEGABYTE = 1024 * 1024;

        private sealed class Covers : IImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
            internal int Opened;

            public Stream Open(string name)
            {
                Opened++;

                return Files.TryGetValue(name, out byte[] bytes) ? new MemoryStream(bytes) : null;
            }
        }

        private static byte[] Encoded(SKColor color, SKEncodedImageFormat format)
        {
            using (var bitmap = new SKBitmap(SIDE, SIDE))
            {
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(color);
                }

                using (SKImage image = SKImage.FromBitmap(bitmap))
                using (SKData data = image.Encode(format, 90))
                {
                    return data.ToArray();
                }
            }
        }

        private Covers _covers;
        private ImageStore _store;

        [TestInitialize]
        public void Setup()
        {
            _covers = new Covers();

            byte[] cover = Encoded(SKColors.Teal, SKEncodedImageFormat.Jpeg);

            for (int i = 0; i < 6; i++)
            {
                _covers.Files["album" + i + ".png"] = cover;
            }

            _covers.Files["poster.png"] = Encoded(SKColors.Orange, SKEncodedImageFormat.Png);

            _store = new ImageStore { Source = _covers, Budget = 8 * MEGABYTE };
        }

        private void Frame(int big, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float side = i == big ? 750 : 200;

                Assert.IsNotNull(_store.Get("album" + i + ".png", side, side));
            }

            _store.Trim();
        }

        [TestMethod]
        public void CoversSeenLargeAndBackInTheGridGiveTheirLargePixelsBack()
        {
            for (int album = 0; album < 6; album++)
            {
                Frame(album, 6);
            }

            Frame(-1, 6);
            Frame(-1, 6);

            Assert.IsTrue(_store.Bytes <= 8 * MEGABYTE,
                $"every album page was visited and every cover is still on screen in the grid: {_store.Bytes / MEGABYTE} MB");

            Bitmap("album0.png", out SKBitmap small);

            Assert.IsTrue(small.Width <= 300, "the grid's copy is a grid-sized decode: " + small.Width);
        }

        [TestMethod]
        public void AnAlbumPageStillOpenKeepsItsLargeCover()
        {
            _store.Budget = 4 * MEGABYTE;

            Frame(2, 6);
            Frame(2, 6);

            int opened = _covers.Opened;

            Frame(2, 6);
            Frame(2, 6);

            Assert.AreEqual(opened, _covers.Opened, "over budget, but nothing on screen is bigger than it is drawn");

            SKBitmap shown = _store.Get("album2.png", 750, 750);

            Assert.AreEqual(SIDE, shown.Width, "the cover on screen at 750 keeps the decode it needs");
        }

        [TestMethod]
        public void UnderTheBudgetNothingIsDecodedAgain()
        {
            _store.Budget = 256 * MEGABYTE;

            for (int album = 0; album < 6; album++)
            {
                Frame(album, 6);
            }

            int opened = _covers.Opened;

            Frame(-1, 6);
            Frame(-1, 6);

            Assert.AreEqual(opened, _covers.Opened, "memory is cheaper than decoding while there is room");
        }

        [TestMethod]
        public void APngDrawnSmallIsKeptSmallToo()
        {
            SKBitmap drawn = _store.Get("poster.png", 200, 200);

            Assert.IsNotNull(drawn);
            Assert.AreEqual(300, drawn.Width, "Skia's PNG codec cannot decode at a scale, so it is resized after");
            Assert.AreEqual(SKColors.Orange, drawn.GetPixel(150, 150), "and it is still the picture");
        }

        private void Bitmap(string name, out SKBitmap bitmap)
        {
            bitmap = _store.Get(name, 200, 200);

            Assert.IsNotNull(bitmap);
        }
    }
}
