using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class CursorImageTests
    {
        private const int VIEWPORT = 200;
        private const int SIZE = 24;
        private const string CROSSHAIR = "crosshair.png";
        private const string MISSING = "nowhere.png";

        private sealed class FakeImages : IImageSource
        {
            private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

            internal void Add(string name, byte[] bytes) => _files[name] = bytes;

            public Stream Open(string name)
                => _files.TryGetValue(name, out byte[] bytes) ? new MemoryStream(bytes) : null;
        }

        private sealed class SlowImages : IAsyncImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
            internal readonly List<TaskCompletionSource<Stream>> Waiting
                = new List<TaskCompletionSource<Stream>>();

            public Stream Open(string name) => null;

            public Task<Stream> OpenAsync(string name)
            {
                var gate = new TaskCompletionSource<Stream>();

                Waiting.Add(gate);

                return gate.Task;
            }

            internal void Deliver(string name)
            {
                TaskCompletionSource<Stream> gate = Waiting[Waiting.Count - 1];

                Waiting.RemoveAt(Waiting.Count - 1);

                Stream stream = Files.TryGetValue(name, out byte[] bytes)
                    ? new MemoryStream(bytes)
                    : null;

                new Thread(() => gate.SetResult(stream)).Start();
            }
        }

        private static byte[] Picture(int size)
        {
            using var bitmap = new SKBitmap(size, size);

            using (var canvas = new SKCanvas(bitmap))
            using (var paint = new SKPaint { Color = new SKColor(0xE8, 0xEC, 0xF5) })
            {
                canvas.DrawRect(0, 0, size, size, paint);
            }

            using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);

            return data.ToArray();
        }

        private List<CursorKind> _kinds;
        private List<CursorImage> _pictures;
        private VisualElement _root;
        private VisualElement _pad;
        private VisualElement _label;
        private IxenSurface _surface;

        private static VisualElement Box(string name, float width, float height)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        private void Build(FakeImages images, CursorStyleDescriptor cursor)
        {
            _kinds = new List<CursorKind>();
            _pictures = new List<CursorImage>();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _pad = Box("pad", 100, 60);
            _pad.Styles.Cursor = cursor;

            _label = Box("label", 60, 20);
            _pad.AddChild(_label);

            _root.AddChildren(_pad, Box("plain", 100, 60));

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            if (images != null)
            {
                _surface.ImageSource = images;
            }

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.CursorSetter = (kind, picture) =>
            {
                _kinds.Add(kind);
                _pictures.Add(picture);
            };
            _kinds.Clear();
            _pictures.Clear();
        }

        private static CursorStyleDescriptor Parsed(string value)
        {
            var xns = new XnsSource("box { cursor: " + value + " }");
            ClassesSet set = xns.Compile();

            if (xns.HasErrors)
            {
                return null;
            }

            var registry = new StyleRegistry();

            registry.Add(set);

            var element = new VisualElement { Name = "box" };
            var root = new VisualElement { Name = "parent" };

            root.AddChild(element);

            var surface = new IxenSurface(root) { Styles = registry };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return element.StylesHandlers.Cursor.Descriptor;
        }

        [TestMethod]
        public void ABareImageNameIsACursor()
        {
            CursorStyleDescriptor parsed = Parsed(CROSSHAIR);

            Assert.IsNotNull(parsed, "an image name did not compile");
            Assert.AreEqual(CursorKind.Image, parsed.Value);
            Assert.AreEqual(CROSSHAIR, parsed.Image);
            Assert.AreEqual(0, parsed.HotspotX);
            Assert.AreEqual(0, parsed.HotspotY);
        }

        [TestMethod]
        public void TwoNumbersAfterTheNameAreTheHotspot()
        {
            CursorStyleDescriptor parsed = Parsed(CROSSHAIR + " 6 8");

            Assert.IsNotNull(parsed, "a hotspot did not compile");
            Assert.AreEqual(6, parsed.HotspotX);
            Assert.AreEqual(8, parsed.HotspotY);
        }

        [TestMethod]
        public void AKeywordStillParsesOnItsOwn()
        {
            CursorStyleDescriptor parsed = Parsed("nwse-resize");

            Assert.IsNotNull(parsed, "a keyword did not compile");
            Assert.AreEqual(CursorKind.ResizeDiagonalDown, parsed.Value);
            Assert.IsNull(parsed.Image);
        }

        [TestMethod]
        public void AKeywordAndAnImageTogetherAreRefused()
            => Assert.IsNull(Parsed(CROSSHAIR + " hand"), "a keyword beside an image was accepted");

        [TestMethod]
        public void TwoKeywordsAreRefused()
            => Assert.IsNull(Parsed("hand move"), "two keywords were accepted");

        [TestMethod]
        public void OneHotspotNumberIsRefused()
            => Assert.IsNull(Parsed(CROSSHAIR + " 6"), "half a hotspot was accepted");

        [TestMethod]
        public void ThreeHotspotNumbersAreRefused()
            => Assert.IsNull(Parsed(CROSSHAIR + " 1 2 3"), "a third number was accepted");

        [TestMethod]
        public void AHotspotWithNoImageIsRefused()
            => Assert.IsNull(Parsed("hand 6 6"), "a keyword took a hotspot");

        [TestMethod]
        public void ANegativeHotspotIsRefused()
            => Assert.IsNull(Parsed(CROSSHAIR + " -1 2"), "a negative hotspot was accepted");

        [TestMethod]
        public void TwoImagesAreRefused()
            => Assert.IsNull(Parsed(CROSSHAIR + " other.png"), "two images were accepted");

        [TestMethod]
        public void AnExtensionOfDigitsIsNotAnImage()
            => Assert.IsNull(Parsed("hand.2"), "a numeric extension was read as an image");

        [TestMethod]
        public void TheHostIsHandedThePixelsAndTheHotspot()
        {
            var images = new FakeImages();

            images.Add(CROSSHAIR, Picture(SIZE));

            Build(images, new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = CROSSHAIR,
                HotspotX = 11,
                HotspotY = 12
            });

            _surface.PointerMove(20, 20);

            Assert.AreEqual(1, _kinds.Count, "the host was not told once");
            Assert.AreEqual(CursorKind.Image, _kinds[0]);
            Assert.IsNotNull(_pictures[0], "no picture reached the host");
            Assert.AreEqual(CROSSHAIR, _pictures[0].Name);
            Assert.AreEqual(SIZE, _pictures[0].Bitmap.Width);
            Assert.AreEqual(11, _pictures[0].HotspotX);
            Assert.AreEqual(12, _pictures[0].HotspotY);
        }

        [TestMethod]
        public void AMissingImageFallsBackToTheArrow()
        {
            Build(new FakeImages(), new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = MISSING
            });

            _surface.PointerMove(20, 20);

            Assert.AreEqual(CursorKind.Default, _surface.Cursor);
            Assert.IsNull(_surface.CursorPicture);
        }

        [TestMethod]
        public void AnImageFromFarAwayArrivesAndReplacesTheArrow()
        {
            var images = new SlowImages();

            images.Files[CROSSHAIR] = Picture(SIZE);

            Build(null, new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = CROSSHAIR
            });

            _surface.ImageSource = images;
            _surface.PointerMove(20, 20);

            Assert.AreEqual(CursorKind.Default, _surface.Cursor,
                "a picture still in flight did not fall back");

            images.Deliver(CROSSHAIR);

            for (int frame = 0; frame < 400 && _surface.ImageBytes == 0; frame++)
            {
                _surface.ComputeLayout(VIEWPORT, VIEWPORT);
                Thread.Sleep(1);
            }

            _surface.PointerMove(21, 21);

            Assert.AreEqual(CursorKind.Image, _surface.Cursor, "the arrived picture was not picked up");
        }

        [TestMethod]
        public void MovingInsideOneElementKeepsTheSamePicture()
        {
            var images = new FakeImages();

            images.Add(CROSSHAIR, Picture(SIZE));

            Build(images, new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = CROSSHAIR
            });

            _surface.PointerMove(20, 20);

            CursorImage first = _surface.CursorPicture;

            _surface.PointerMove(30, 30);

            Assert.AreSame(first, _surface.CursorPicture, "the picture was rebuilt");
            Assert.AreEqual(1, _kinds.Count, "the host was told twice for one cursor");
        }

        [TestMethod]
        public void MovingBetweenTwoImageCursorsTellsTheHostTwice()
        {
            var images = new FakeImages();

            images.Add(CROSSHAIR, Picture(SIZE));
            images.Add("other.png", Picture(SIZE + 8));

            Build(images, new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = CROSSHAIR
            });

            VisualElement plain = _root.ChildElements[1];

            plain.Styles.Cursor = new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = "other.png"
            };
            plain.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.PointerMove(20, 40);
            _surface.PointerMove(20, 80);

            Assert.AreEqual(2, _kinds.Count, "the second picture was not reported");
            Assert.AreEqual(SIZE + 8, _pictures[1].Bitmap.Width, "the wrong picture reached the host");
        }

        [TestMethod]
        public void AHotspotOutsideThePictureIsClamped()
        {
            var images = new FakeImages();

            images.Add(CROSSHAIR, Picture(SIZE));

            Build(images, new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = CROSSHAIR,
                HotspotX = 400,
                HotspotY = 400
            });

            _surface.PointerMove(20, 20);

            Assert.AreEqual(SIZE - 1, _pictures[0].HotspotX);
            Assert.AreEqual(SIZE - 1, _pictures[0].HotspotY);
        }

        [TestMethod]
        public void AnImageCursorIsInherited()
        {
            var images = new FakeImages();

            images.Add(CROSSHAIR, Picture(SIZE));

            Build(images, new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = CROSSHAIR
            });

            _surface.PointerMove(10, 10);

            Assert.AreSame(_label, _surface.HoveredElement, "the child was not the hit");
            Assert.AreEqual(CursorKind.Image, _surface.Cursor, "the child did not inherit the image");
        }

        [TestMethod]
        public void AKeywordOnAChildBeatsTheInheritedImage()
        {
            var images = new FakeImages();

            images.Add(CROSSHAIR, Picture(SIZE));

            Build(images, new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = CROSSHAIR
            });

            _label.Styles.Cursor = new CursorStyleDescriptor { Value = CursorKind.Hand };
            _label.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.PointerMove(10, 10);

            Assert.AreEqual(CursorKind.Hand, _surface.Cursor);
            Assert.IsNull(_surface.CursorPicture, "a keyword left a picture behind");
        }

        [TestMethod]
        public void LeavingTheSurfaceGoesBackToTheArrow()
        {
            var images = new FakeImages();

            images.Add(CROSSHAIR, Picture(SIZE));

            Build(images, new CursorStyleDescriptor
            {
                Value = CursorKind.Image,
                Image = CROSSHAIR
            });

            _surface.PointerMove(20, 20);
            _surface.PointerLeaveSurface();

            Assert.AreEqual(CursorKind.Default, _surface.Cursor);
            Assert.IsNull(_surface.CursorPicture, "the picture outlived the pointer");
        }

        [TestMethod]
        public void AHandlerCanChangeTheCursorWithinOneElement()
        {
            Build(null, new CursorStyleDescriptor { Value = CursorKind.Crosshair });

            _label.Styles.Cursor = new CursorStyleDescriptor { Value = CursorKind.Crosshair };
            _label.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _pad.PointerMove += (sender, e) =>
            {
                _pad.Styles.Cursor.Value = e.X < 50 ? CursorKind.ResizeHorizontal : CursorKind.Move;
            };

            _surface.PointerMove(20, 40);

            Assert.AreEqual(CursorKind.ResizeHorizontal, _surface.Cursor,
                "the handler's cursor was not read in the same event");

            _surface.PointerMove(80, 40);

            Assert.AreEqual(CursorKind.Move, _surface.Cursor,
                "the cursor did not follow the pointer inside one element");
        }
    }
}
