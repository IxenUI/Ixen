using Ixen.Core.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.IO;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class ImageDecodeSizeTests
    {
        private const int SIDE = 800;

        private sealed class JpegSource : IImageSource
        {
            private readonly byte[] _bytes;

            internal int Reads { get; private set; }

            internal JpegSource()
            {
                using var bitmap = new SKBitmap(SIDE, SIDE);
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.CornflowerBlue);
                }

                using SKData data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);

                _bytes = data.ToArray();
            }

            public Stream Open(string name)
            {
                Reads++;

                return new MemoryStream(_bytes);
            }
        }

        private static ImageStore Store(out JpegSource source)
        {
            source = new JpegSource();

            return new ImageStore { Source = source };
        }

        [TestMethod]
        public void MeasuringReadsTheHeaderAndDecodesNoPixels()
        {
            ImageStore store = Store(out JpegSource source);

            Assert.IsTrue(store.TryMeasure("photo.jpg", out float width, out float height));
            Assert.AreEqual(SIDE, width);
            Assert.AreEqual(SIDE, height);

            Assert.AreEqual(0, store.Bytes,
                "measuring has to answer how big the picture is, and that is in the header - "
                + "SKCodec reads it without touching a pixel. It used to decode the whole thing, "
                + "which is why a decode happened during the MEASURE pass, where the size it will "
                + "be drawn at is not known yet. Splitting the two questions is the whole feature: "
                + "the natural size comes from the header during measure, the pixels come at render "
                + "time where the drawn size is known.");
        }

        [TestMethod]
        public void ThePixelsComeAtTheSizeTheyAreDrawn()
        {
            ImageStore store = Store(out _);

            SKBitmap small = store.Get("photo.jpg", 100, 100);

            Assert.IsNotNull(small);
            Assert.AreEqual(SIDE / 8, small.Width,
                $"an 800px picture drawn at 100 is decoded at {SIDE / 8}, an eighth, because that "
                + "is the smallest power-of-two division that still covers the request. A JPEG "
                + "divides natively at decode time, so this is cheaper than decoding and scaling.");
            Assert.AreEqual(small.ByteCount, store.Bytes, "the accounting follows the real bitmap");
        }

        [TestMethod]
        public void ADivisionNeverGoesBelowWhatIsAsked()
        {
            ImageStore store = Store(out _);

            SKBitmap bitmap = store.Get("photo.jpg", 300, 300);

            Assert.IsTrue(bitmap.Width >= 300,
                $"{bitmap.Width} covers a request of 300 - a quarter of 800 is 200, which does "
                + "not, so the half is what is taken. Rounding the other way would upscale a "
                + "picture on screen, which is the one thing this must never do.");
            Assert.AreEqual(SIDE / 2, bitmap.Width);
        }

        [TestMethod]
        public void AskingBiggerDecodesAgain()
        {
            ImageStore store = Store(out JpegSource source);

            store.Get("photo.jpg", 100, 100);

            int reads = source.Reads;

            SKBitmap bigger = store.Get("photo.jpg", 700, 700);

            Assert.AreEqual(SIDE, bigger.Width, "a request the cached division cannot cover is "
                + "decoded again at one that can");
            Assert.AreEqual(reads + 1, source.Reads, "and that costs exactly one more read");
            Assert.AreEqual(bigger.ByteCount, store.Bytes,
                "the old bitmap's bytes left the total with it");
        }

        [TestMethod]
        public void AskingSmallerKeepsWhatIsAlreadyThere()
        {
            ImageStore store = Store(out JpegSource source);

            store.Get("photo.jpg", 700, 700);

            int reads = source.Reads;
            long bytes = store.Bytes;

            SKBitmap smaller = store.Get("photo.jpg", 50, 50);

            Assert.AreEqual(SIDE, smaller.Width);
            Assert.AreEqual(reads, source.Reads);
            Assert.AreEqual(bytes, store.Bytes,
                "a picture that shrank keeps the bigger bitmap rather than decoding a third time. "
                + "That is a deliberate wart: rotating a phone to a wide layout and back leaves "
                + "the wide decode in place until the entry is evicted. Downgrading on every "
                + "shrink would re-decode on every resize step of a drag.");
        }

        [TestMethod]
        public void ATileTakesTheNaturalSize()
        {
            ImageStore store = Store(out _);

            store.Get("photo.jpg", 40, 40);

            Assert.IsNotNull(store.GetTile("photo.jpg"));

            SKBitmap bitmap = store.Get("photo.jpg", 40, 40);

            Assert.AreEqual(SIDE, bitmap.Width,
                "a repeating tile has no useful size smaller than its own: the shader repeats the "
                + "pixels at their natural size, so asking for a tile upgrades the entry to the "
                + "whole picture. The demo's background texture is exactly this case, which is why "
                + "it is the one image in the demo this feature cannot shrink.");
        }

        [TestMethod]
        public void AMissingNameIsStillOnlyLookedUpOnce()
        {
            var store = new ImageStore { Source = new MissingSource() };

            Assert.IsFalse(store.TryMeasure("nope.jpg", out _, out _));
            Assert.IsNull(store.Get("nope.jpg", 100, 100));
            Assert.IsNull(store.Get("nope.jpg", 100, 100));

            Assert.AreEqual(1, ((MissingSource)store.Source).Opens,
                "the header read that failed is what caches the miss, so neither the second ask "
                + "nor a request for pixels goes back to the source");
        }

        [TestMethod]
        public void AWallOfThumbnailsIsNoLongerAWallOfFullDecodes()
        {
            ImageStore store = Store(out _);

            for (int index = 0; index < 40; index++)
            {
                store.Get($"photo{index}.jpg", 90, 90);
            }

            long full = 40L * SIDE * SIDE * 4;

            Assert.IsTrue(store.Bytes < full / 40,
                $"forty distinct {SIDE}x{SIDE} pictures shown as 90px thumbnails hold "
                + $"{store.Bytes / 1024} KB, against {full / (1024 * 1024)} MB decoded whole. "
                + "That figure is not hypothetical: the note that gave the image cache its byte "
                + "budget measured exactly this - forty 720x720 photos at 79 MB - and bounded it "
                + "rather than reduced it, so a gallery reached the budget and started evicting "
                + "pictures it was still showing. This is the same wall costing a fortieth.");
        }

        private sealed class MissingSource : IImageSource
        {
            internal int Opens { get; private set; }

            public Stream Open(string name)
            {
                Opens++;

                return null;
            }
        }
    }
}
