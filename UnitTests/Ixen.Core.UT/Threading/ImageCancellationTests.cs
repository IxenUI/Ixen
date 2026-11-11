using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Threading
{
    [TestClass]
    public class ImageCancellationTests
    {
        private const int COUNT = 10;
        private const int SIDE = 160;

        private sealed class Gated : IAsyncImageSource
        {
            private readonly object _lock = new object();
            private readonly Dictionary<string, int> _asked = new Dictionary<string, int>();
            private readonly HashSet<string> _cancelled = new HashSet<string>();
            private readonly List<KeyValuePair<string, TaskCompletionSource<Stream>>> _waiting
                = new List<KeyValuePair<string, TaskCompletionSource<Stream>>>();

            internal bool HonoursCancellation = true;

            internal static readonly byte[] PICTURE = Encode();

            private static byte[] Encode()
            {
                using (var bitmap = new SKBitmap(16, 16))
                {
                    bitmap.Erase(new SKColor(200, 40, 40));

                    using (SKImage image = SKImage.FromBitmap(bitmap))
                    using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        return data.ToArray();
                    }
                }
            }

            public Stream Open(string name) => null;

            public Task<Stream> OpenAsync(string name, CancellationToken cancellation)
            {
                var answer = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);

                lock (_lock)
                {
                    _asked[name] = Asked(name) + 1;
                    _waiting.Add(new KeyValuePair<string, TaskCompletionSource<Stream>>(name, answer));
                }

                cancellation.Register(() =>
                {
                    lock (_lock)
                    {
                        _cancelled.Add(name);
                    }

                    if (HonoursCancellation)
                    {
                        answer.TrySetCanceled();
                    }
                });

                return answer.Task;
            }

            internal int Asked(string name)
            {
                lock (_lock)
                {
                    return _asked.TryGetValue(name, out int count) ? count : 0;
                }
            }

            internal bool WasCancelled(string name)
            {
                lock (_lock)
                {
                    return _cancelled.Contains(name);
                }
            }

            internal int CancelledCount
            {
                get
                {
                    lock (_lock)
                    {
                        return _cancelled.Count;
                    }
                }
            }

            internal void Answer()
            {
                List<KeyValuePair<string, TaskCompletionSource<Stream>>> waiting;

                lock (_lock)
                {
                    waiting = _waiting.ToList();
                    _waiting.Clear();
                }

                foreach (KeyValuePair<string, TaskCompletionSource<Stream>> one in waiting)
                {
                    one.Value.TrySetResult(new MemoryStream(PICTURE));
                }
            }
        }

        private Gated _source;
        private IxenSurface _surface;
        private VisualElement _scroller;
        private readonly List<Image> _images = new List<Image>();
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        [TestInitialize]
        public void Setup()
        {
            _source = new Gated();

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _scroller = new VisualElement { Scrollable = true };
            _scroller.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _scroller.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _scroller.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIDE };
            root.AddChild(_scroller);

            for (int i = 0; i < COUNT; i++)
            {
                var image = new Image { Source = Name(i) };
                image.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIDE };
                image.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIDE };
                _scroller.AddChild(image);
                _images.Add(image);
            }

            _surface = new IxenSurface(root) { Styles = new StyleRegistry(), ImageSource = _source };
            _bitmap = new SKBitmap(200, SIDE);
            _canvas = new SKCanvas(_bitmap);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas.Dispose();
            _bitmap.Dispose();
        }

        private static string Name(int index) => "cover" + index + ".png";

        private void Frame()
        {
            _surface.ComputeLayout(_bitmap.Width, _bitmap.Height);
            _surface.Render(_canvas);
        }

        private void Frames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                Frame();
            }
        }

        private void AnswerAndSettle(Image awaited)
        {
            System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

            _source.Answer();
            Frame();

            while (awaited.IsLoading && clock.ElapsedMilliseconds < 5000)
            {
                Thread.Sleep(2);
                _source.Answer();
                Frame();
            }

            Frames(2);
        }

        private void PumpUntil(Func<bool> done)
        {
            System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

            while (!done() && clock.ElapsedMilliseconds < 5000)
            {
                Thread.Sleep(2);
                Frame();
            }
        }

        private void ScrollTo(int index)
        {
            _scroller.ScrollY = index * SIDE;
            _scroller.InvalidateLayout();
        }

        private void Hide(Image image, bool hidden)
        {
            image.Styles.Visibility = new VisibilityStyleDescriptor { Value = hidden ? Visibility.Hidden : Visibility.Visible };
            image.Invalidate();
        }

        [TestMethod]
        public void AnImageBelowTheFoldKeepsDownloading()
        {
            Frames(3);

            for (int i = 0; i < COUNT; i++)
            {
                Assert.AreEqual(1, _source.Asked(Name(i)), "measuring an image asks for it once");
                Assert.IsFalse(_source.WasCancelled(Name(i)),
                    $"image {i} is in the tree and shown, only scrolled away: fetching it ahead is what an app "
                    + "asks for by not virtualising, and what a virtual list's overscan rows are for");
            }
        }

        [TestMethod]
        public void AnImageThatChangesItsPictureDropsTheOldDownload()
        {
            Frames(2);

            _images[5].Source = "other.png";
            Frames(2);

            Assert.IsTrue(_source.WasCancelled(Name(5)), "nothing shows that picture any more: a recycled row");
            Assert.AreEqual(1, _source.Asked("other.png"));
            Assert.IsFalse(_source.WasCancelled("other.png"), "its new picture is wanted");
            Assert.IsFalse(_source.WasCancelled(Name(4)), "and its neighbours are left alone");
        }

        [TestMethod]
        public void ADetachedImageDropsItsDownload()
        {
            Frames(2);

            _scroller.RemoveChild(_images[5]);
            Frames(2);

            Assert.IsTrue(_source.WasCancelled(Name(5)));
            Assert.AreEqual(1, _source.CancelledCount, "and only that one");
        }

        [TestMethod]
        public void AHiddenImageDropsItsDownloadAndStaysLoadingRatherThanFailed()
        {
            Frames(2);

            Hide(_images[0], true);
            Frames(2);

            for (int frame = 0; frame < 20; frame++)
            {
                Thread.Sleep(2);
                Frame();
            }

            Assert.IsTrue(_source.WasCancelled(Name(0)), "a hidden tab's pictures are not worth the network");
            Assert.IsTrue(_images[0].IsLoading, "it has not arrived");
            Assert.IsFalse(_images[0].HasFailed, "a cancellation is not a failure: :failed would show a broken picture");
        }

        [TestMethod]
        public void ShownAgainItAsksAgainAndArrives()
        {
            Frames(2);
            Hide(_images[0], true);
            Frames(2);

            Assert.IsTrue(_source.WasCancelled(Name(0)));

            Hide(_images[0], false);
            Frames(2);

            Assert.AreEqual(2, _source.Asked(Name(0)), "wanted again, the picture is asked for again");

            AnswerAndSettle(_images[0]);

            Assert.IsFalse(_images[0].IsLoading || _images[0].HasFailed, "and it arrives");
            Assert.AreEqual(255, _bitmap.GetPixel(80, 80).Alpha, "drawn");
            Assert.AreEqual(200, _bitmap.GetPixel(80, 80).Red, "drawn: it is the picture");
        }

        [TestMethod]
        public void AnAnswerThatLandsDespiteTheCancellationIsKept()
        {
            _source.HonoursCancellation = false;

            Frames(2);
            Hide(_images[0], true);
            Frames(2);

            Assert.IsTrue(_source.WasCancelled(Name(0)));

            _source.Answer();
            PumpUntil(() => _surface.ImagesPending == 0);

            Assert.AreEqual(0, _surface.ImagesPending, "the bytes came anyway, so the store keeps them");

            Hide(_images[0], false);
            Frames(2);

            Assert.AreEqual(1, _source.Asked(Name(0)), "and showing it again fetches nothing");
            Assert.AreEqual(200, _bitmap.GetPixel(80, 80).Red);
        }

        [TestMethod]
        public void AHiddenImageLaidOutAgainAndAgainIsNotFetchedAgain()
        {
            Hide(_images[0], true);

            for (int frame = 0; frame < 10; frame++)
            {
                _images[0].Invalidate();
                Frame();
                Frame();
            }

            Assert.IsTrue(_source.WasCancelled(Name(0)), "hidden, it is not wanted");
            Assert.AreEqual(1, _source.Asked(Name(0)),
                "measuring and the prepass ask the store about it at every layout; only being wanted restarts "
                + "a download, or every layout would start one and the next idle frame cancel it");
        }

        [TestMethod]
        public void ARecycledRowFlungThroughThirtyPicturesKeepsOnlyTheLast()
        {
            const int PASSED = 30;

            for (int frame = 0; frame < PASSED; frame++)
            {
                _images[0].Source = "row" + frame + ".png";
                Frame();
            }

            AnswerAndSettle(_images[0]);

            Assert.IsFalse(_images[0].IsLoading, "where the fling stopped, the picture arrived");
            Assert.IsFalse(_source.WasCancelled("row" + (PASSED - 1) + ".png"));

            for (int frame = 0; frame < PASSED - 1; frame++)
            {
                Assert.IsTrue(_source.WasCancelled("row" + frame + ".png"), $"picture {frame} was passed over and dropped");
            }
        }

        [TestMethod]
        public void ChangingTheSourceCancelsEveryDownload()
        {
            Frames(1);

            _surface.ImageSource = null;

            Assert.IsTrue(_source.WasCancelled(Name(0)), "even the one on screen: the store forgot it");
            Assert.AreEqual(COUNT, _source.CancelledCount);
        }
    }
}

