using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Threading
{
    [TestClass]
    public class CoverGridTests
    {
        private const int COVERS = 40;
        private const int COLUMNS = 8;
        private const int SIDE = 1200;
        private const int SHOWN = 160;

        private sealed class Store : IAsyncImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
            internal int Asked;

            public Stream Open(string name) => null;

            public Task<Stream> OpenAsync(string name)
            {
                Interlocked.Increment(ref Asked);

                byte[] bytes = Files[name];

                return Task.Run(async () =>
                {
                    await Task.Delay(20);

                    return (Stream)new MemoryStream(bytes);
                });
            }
        }

        private static readonly Dictionary<int, byte[]> _covers = new Dictionary<int, byte[]>();

        private static byte[] Cover(int seed)
        {
            lock (_covers)
            {
                if (_covers.TryGetValue(seed, out byte[] cached))
                {
                    return cached;
                }

                var random = new Random(seed);

                using (var bitmap = new SKBitmap(SIDE, SIDE))
                {
                    using (var canvas = new SKCanvas(bitmap))
                    using (var paint = new SKPaint { IsAntialias = true })
                    {
                        canvas.Clear(new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));

                        for (int i = 0; i < 40; i++)
                        {
                            paint.Color = new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                            canvas.DrawCircle(random.Next(SIDE), random.Next(SIDE), random.Next(40, 240), paint);
                        }
                    }

                    using (SKImage image = SKImage.FromBitmap(bitmap))
                    using (SKData data = image.Encode(SKEncodedImageFormat.Jpeg, 85))
                    {
                        byte[] bytes = data.ToArray();

                        _covers[seed] = bytes;

                        return bytes;
                    }
                }
            }
        }

        private Store _store;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;
        private long _encoded;
        private readonly List<VisualElement> _rows = new List<VisualElement>();

        [TestInitialize]
        public void Setup()
        {
            _store = new Store();

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            VisualElement row = null;

            for (int i = 0; i < COVERS; i++)
            {
                string name = "cover" + i + ".jpg";

                _store.Files[name] = Cover(i);
                _encoded += _store.Files[name].Length;

                if (i % COLUMNS == 0)
                {
                    row = new VisualElement();
                    row.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
                    row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = SHOWN };
                    root.AddChild(row);
                    _rows.Add(row);
                }

                var cover = new Image { Source = name };
                cover.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SHOWN };
                cover.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = SHOWN };
                row.AddChild(cover);
            }

            _surface = new IxenSurface(root) { Styles = new StyleRegistry(), ImageSource = _store };
            _bitmap = new SKBitmap(COLUMNS * SHOWN, (COVERS / COLUMNS) * SHOWN);
            _canvas = new SKCanvas(_bitmap);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas.Dispose();
            _bitmap.Dispose();
        }

        private void Frame()
        {
            _surface.ComputeLayout(_bitmap.Width, _bitmap.Height);
            _surface.Render(_canvas);
        }

        private void Settle()
        {
            System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

            Frame();

            while (_surface.ImagesPending > 0 && clock.ElapsedMilliseconds < 20000)
            {
                Thread.Sleep(5);
                Frame();
            }

            Frames(2);

            Assert.AreEqual(0, _surface.ImagesPending,
                "every cover has to have arrived before anything is measured, or an empty grid passes for a light one");
        }

        private void Frames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                Frame();
            }
        }

        private bool Drawn(int cover)
        {
            int x = (cover % COLUMNS) * SHOWN + SHOWN / 2;
            int y = (cover / COLUMNS) * SHOWN + SHOWN / 2;

            return _bitmap.GetPixel(x, y).Alpha == 255;
        }

        private string State(int cover)
        {
            int x = (cover % COLUMNS) * SHOWN + SHOWN / 2;
            int y = (cover / COLUMNS) * SHOWN + SHOWN / 2;

            return $"cover {cover} reads {_bitmap.GetPixel(x, y)}, {_surface.ImageBytes} bytes held, "
                + $"{_store.Asked} downloads asked, {_bitmap.ColorType} bitmap";
        }

        [TestMethod]
        public void ForCoversDrawnSmallItHoldsWhatTheyShowRatherThanWhatTheyWeigh()
        {
            Settle();

            long perCover = 2 * SHOWN * 2 * SHOWN * 4;

            Assert.IsTrue(_surface.ImageBytes <= _encoded + COVERS * perCover,
                $"{_surface.ImageBytes} bytes held for {COVERS} covers of {SIDE}px drawn at {SHOWN}: decoded whole "
                    + $"they were {COVERS * SIDE * SIDE * 4L} bytes, so the budget threw out covers still on screen");
            Assert.IsTrue(Drawn(0) && Drawn(COVERS - 1), "and every one of them is on screen: " + State(0) + "; " + State(COVERS - 1));
        }

        [TestMethod]
        public void EachCoverIsFetchedOnceAndNeverAgainWhileItStaysOnScreen()
        {
            Settle();

            Assert.AreEqual(COVERS, _store.Asked, "one download per cover");

            Frames(10);

            Assert.AreEqual(COVERS, _store.Asked,
                "a cover evicted while on screen was fetched again on the next frame, forever");
        }

        [TestMethod]
        public void AScreenfulBiggerThanTheBudgetStaysRatherThanThrashing()
        {
            _surface.ImageCacheBudget = 1024 * 1024;

            Settle();
            Frames(10);

            Assert.AreEqual(COVERS, _store.Asked, "what is on screen is never evicted, even over budget");
            Assert.IsTrue(Drawn(0) && Drawn(COVERS - 1), State(0) + "; " + State(COVERS - 1));
        }

        [TestMethod]
        public void IdleFramesThatPaintNothingDoNotExposeWhatIsOnScreen()
        {
            _surface.ImageCacheBudget = 1024 * 1024;

            Settle();
            Frames(10);

            _surface.InvalidateVisual();
            Frame();

            Assert.AreEqual(COVERS, _store.Asked,
                "a frame with nothing to repaint draws nothing, so it touches no picture; the trim of "
                + "the frames after it must still treat what is on screen as on screen, or the next "
                + "real repaint fetches every cover again");
            Assert.IsTrue(Drawn(0) && Drawn(COVERS - 1), State(0) + "; " + State(COVERS - 1));
        }

        [TestMethod]
        public void FramesThatRepaintOneCornerDoNotExposeTheRest()
        {
            _surface.ImageCacheBudget = 1024 * 1024;

            Settle();

            for (int frame = 0; frame < 10; frame++)
            {
                _surface.InvalidateVisual(_rows[0].ChildElements[0]);
                Frame();
            }

            _surface.InvalidateVisual();
            Frame();

            Assert.AreEqual(COVERS, _store.Asked,
                "a frame clipped to one cover draws only that one; the others are still on screen");
            Assert.IsTrue(Drawn(0) && Drawn(COVERS - 1), State(0) + "; " + State(COVERS - 1));
        }

        [TestMethod]
        public void ABackgroundPictureOnScreenIsKeptToo()
        {
            for (int row = 0; row < _rows.Count; row++)
            {
                foreach (VisualElement element in _rows[row].ChildElements)
                {
                    var cover = (Image)element;

                    cover.Styles.Background = new BackgroundStyleDescriptor { ImageUrl = cover.Source };
                    cover.Source = null;
                    cover.Invalidate();
                }
            }

            _surface.ImageCacheBudget = 1024 * 1024;

            Settle();

            Assert.AreEqual(COVERS, _store.Asked, "one download per background");

            Frames(10);

            _surface.InvalidateVisual();
            Frame();

            Assert.AreEqual(COVERS, _store.Asked,
                "a background is a picture on screen like an <Image>: idle frames paint it not, and "
                + "the trim must not take that for being off screen");
        }

        [TestMethod]
        public void ACoverEvictedOffScreenComesBackWithoutBeingFetchedAgain()
        {
            Settle();

            foreach (VisualElement row in _rows.GetRange(1, _rows.Count - 1))
            {
                row.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
                row.Invalidate();
            }

            Frames(2);

            _surface.ImageCacheBudget = _encoded + COLUMNS * 2 * SHOWN * 2 * SHOWN * 4;

            Frames(2);

            long hidden = _surface.ImageBytes;

            Assert.IsTrue(hidden < _encoded + COLUMNS * 2 * SHOWN * 2 * SHOWN * 4,
                "the hidden covers gave their pixels back");

            _surface.ImageCacheBudget = 64 * 1024 * 1024;

            foreach (VisualElement row in _rows)
            {
                row.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Visible };
                row.Invalidate();
            }

            Frames(3);

            Assert.AreEqual(COVERS, _store.Asked, "they kept their compressed bytes, so nothing is downloaded again");
            Assert.IsTrue(Drawn(COVERS - 1), "and they are decoded again from those bytes: " + State(COVERS - 1));
        }
    }
}
