using Ixen.Core.Language.Xns;
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
    public class ImageStatusTests
    {
        private const string COVER = "cover.png";
        private const string OTHER = "other.png";

        private sealed class Gated : IAsyncImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
            internal readonly Dictionary<string, TaskCompletionSource<Stream>> Gates
                = new Dictionary<string, TaskCompletionSource<Stream>>();

            public Stream Open(string name) => null;

            public Task<Stream> OpenAsync(string name)
            {
                var gate = new TaskCompletionSource<Stream>();

                Gates[name] = gate;

                return gate.Task;
            }

            internal void Deliver(string name)
            {
                TaskCompletionSource<Stream> gate = Gates[name];
                Stream stream = Files.TryGetValue(name, out byte[] bytes) ? new MemoryStream(bytes) : null;

                new Thread(() => gate.SetResult(stream)).Start();
            }
        }

        private sealed class Plain : IImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

            public Stream Open(string name)
                => Files.TryGetValue(name, out byte[] bytes) ? new MemoryStream(bytes) : null;
        }

        private VisualElement _root;
        private Image _cover;
        private IxenSurface _surface;
        private Gated _store;
        private readonly List<string> _events = new List<string>();

        private static byte[] Png()
        {
            using (var bitmap = new SKBitmap(20, 20))
            {
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Red);
                }

                using (SKImage image = SKImage.FromBitmap(bitmap))
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _store = new Gated();
            _store.Files[COVER] = Png();
            _store.Files[OTHER] = Png();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _cover = new Image { Name = "cover", Source = COVER };
            _cover.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _cover.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _cover.Loaded += (sender, e) => _events.Add("loaded");
            _cover.Failed += (sender, e) => _events.Add("failed");

            _root.AddChild(_cover);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), ImageSource = _store };
        }

        private void Layout() => _surface.ComputeLayout(200, 200);

        private void Pump(Func<bool> until)
        {
            for (int frame = 0; frame < 400 && !until(); frame++)
            {
                Layout();
                Thread.Sleep(1);
            }

            Layout();
        }

        [TestMethod]
        public void APictureOnItsWaySaysSoAndThenSaysItArrived()
        {
            Layout();

            Assert.IsTrue(_cover.IsLoading);
            Assert.IsTrue(_cover.HasState(Image.LOADING), "a stylesheet can dim or hide it meanwhile");

            _store.Deliver(COVER);
            Pump(() => !_cover.IsLoading);

            Assert.IsFalse(_cover.HasState(Image.LOADING));
            Assert.IsFalse(_cover.HasFailed);
            Assert.AreEqual("loaded", string.Join(",", _events), "once, on the frame after it arrived");
        }

        [TestMethod]
        public void APictureThatNeverComesFails()
        {
            _cover.Source = "missing.png";
            Layout();

            _store.Deliver("missing.png");
            Pump(() => _cover.HasFailed);

            Assert.IsTrue(_cover.HasState(Image.FAILED), "a stylesheet can show a placeholder");
            Assert.IsFalse(_cover.HasState(Image.LOADING));
            Assert.AreEqual("failed", string.Join(",", _events));
        }

        [TestMethod]
        public void AnotherSourceStartsOver()
        {
            Layout();
            _store.Deliver(COVER);
            Pump(() => !_cover.IsLoading);

            _cover.Source = OTHER;
            Layout();

            Assert.IsTrue(_cover.IsLoading, "the next track's cover is on its way in turn");

            _store.Deliver(OTHER);
            Pump(() => !_cover.IsLoading);

            Assert.AreEqual("loaded,loaded", string.Join(",", _events));
        }

        [TestMethod]
        public void ALocalPictureIsReadyAtOnce()
        {
            var plain = new Plain();
            plain.Files[COVER] = Png();
            _surface.ImageSource = plain;

            Layout();

            Assert.IsFalse(_cover.IsLoading);
            Assert.IsFalse(_cover.HasState(Image.LOADING), "never seen loading, so nothing flashes");

            Layout();

            Assert.AreEqual("loaded", string.Join(",", _events), "and its arrival is still reported");
        }

        [TestMethod]
        public void ALoadingRuleFromAStylesheetAppliesAndLifts()
        {
            var source = new XnsSource("cover:loading {\r\n    opacity: 0\r\n}\r\n");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);
            _surface.Styles = registry;

            Layout();

            Assert.AreEqual(0f, _cover.StylesHandlers.Opacity.Descriptor.Value, "hidden while it loads");

            _store.Deliver(COVER);
            Pump(() => !_cover.IsLoading);

            Assert.AreEqual(1f, _cover.StylesHandlers.Opacity.Descriptor.Value, "and shown in the frame it arrives");
        }
    }
}
