using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.IO;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class ImageStoreBudgetTests
    {
        private const int SIDE = 200;
        private const long ONE = SIDE * SIDE * 4;

        private sealed class MemorySource : IImageSource
        {
            private readonly byte[] _png;

            internal int Reads { get; private set; }
            internal int Opens { get; private set; }

            internal MemorySource()
            {
                using (var bitmap = new SKBitmap(SIDE, SIDE))
                {
                    using (var canvas = new SKCanvas(bitmap))
                    {
                        canvas.Clear(SKColors.CornflowerBlue);
                    }

                    using (SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        _png = data.ToArray();
                    }
                }
            }

            public Stream Open(string name)
            {
                Opens++;

                if (name.StartsWith("missing"))
                {
                    return null;
                }

                Reads++;

                return new MemoryStream(_png);
            }
        }

        [TestMethod]
        public void TheStoreStopsGrowingOnceItIsOverBudget()
        {
            var store = new ImageStore { Source = new MemorySource(), Budget = ONE * 3 };

            for (int index = 0; index < 30; index++)
            {
                store.Get($"photo{index}.png", SIDE, SIDE);
                store.Trim();
            }

            Assert.IsTrue(store.Bytes <= ONE * 3,
                $"the store holds {store.Bytes / 1024} KB against a budget of {ONE * 3 / 1024} KB. "
                + "Decoded bitmaps used to be cached forever: 40 photos of 720x720 came to 79 MB "
                + "with nothing ever evicted.");
        }

        [TestMethod]
        public void TheLeastRecentlyUsedGoesFirst()
        {
            var source = new MemorySource();
            var store = new ImageStore { Source = source, Budget = ONE * 2 };

            store.Get("a.png", SIDE, SIDE);
            store.Get("b.png", SIDE, SIDE);

            store.Trim();

            int reads = source.Reads;

            store.Get("a.png", SIDE, SIDE);

            Assert.AreEqual(reads, source.Reads, "both still fit, so nothing was reloaded");

            store.Get("c.png", SIDE, SIDE);
            store.Trim();

            store.Get("a.png", SIDE, SIDE);

            Assert.AreEqual(reads + 2, source.Reads,
                "a was touched more recently than b, so b is the one that left, and the two reads "
                + "are c alone: one for its header and one for its pixels, which are two separate "
                + "opens now that measuring never decodes");

            store.Get("b.png", SIDE, SIDE);

            Assert.AreEqual(reads + 4, source.Reads,
                "and b, having been evicted, costs its header and its pixels over again");
        }

        [TestMethod]
        public void AMissingNameIsNeverEvicted()
        {
            var source = new MemorySource();
            var store = new ImageStore { Source = source, Budget = ONE };

            Assert.IsFalse(store.TryMeasure("missing.png", out _, out _));

            for (int index = 0; index < 10; index++)
            {
                store.Get($"photo{index}.png", SIDE, SIDE);
                store.Trim();
            }

            int opens = source.Opens;

            Assert.IsFalse(store.TryMeasure("missing.png", out _, out _));

            Assert.AreEqual(opens, source.Opens,
                "a missing name costs nothing to keep, so it stays cached and is still only "
                + "looked up once - the eviction only ever considers entries that hold bytes");
        }

        [TestMethod]
        public void EvictingDropsTheTileWithTheBitmap()
        {
            var store = new ImageStore { Source = new MemorySource(), Budget = ONE };

            Assert.IsNotNull(store.GetTile("a.png"), "a tile shader is built from the bitmap");

            store.Get("b.png", SIDE, SIDE);
            store.Trim();

            SKPaint tile = store.GetTile("a.png");

            Assert.IsNotNull(tile, "asking again rebuilds it");

            Assert.IsNotNull(tile.Shader,
                "and its shader is live, which is the point: a tile holds a shader made from the "
                + "bitmap, so evicting one without the other would leave a shader over freed pixels");
        }

        [TestMethod]
        public void TheBudgetIsReachableFromTheSurface()
        {
            var surface = new IxenSurface();

            Assert.AreEqual(64L * 1024 * 1024, surface.ImageCacheBudget,
                "64 MB is a default, not a policy the host is stuck with");

            surface.ImageCacheBudget = 8 * 1024 * 1024;

            Assert.AreEqual(8L * 1024 * 1024, surface.ImageCacheBudget);
        }

        [TestMethod]
        public void ChangingTheSourceStillClearsEverything()
        {
            var store = new ImageStore { Source = new MemorySource(), Budget = ONE * 100 };

            store.Get("a.png", SIDE, SIDE);

            Assert.IsTrue(store.Bytes > 0);

            store.Source = new MemorySource();

            Assert.AreEqual(0, store.Bytes, "the running total has to be reset with the entries");
        }
    }
}
