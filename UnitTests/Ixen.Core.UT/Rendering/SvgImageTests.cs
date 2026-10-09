using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ixen.Core.UT.Rendering
{
    internal sealed class SvgSource : IImageSource
    {
        internal readonly Dictionary<string, string> Files = new Dictionary<string, string>();

        internal int Reads;

        public Stream Open(string name)
        {
            Reads++;

            return Files.TryGetValue(name, out string source)
                ? new MemoryStream(Encoding.UTF8.GetBytes(source))
                : null;
        }
    }

    internal sealed class PngSource : IImageSource
    {
        private readonly byte[] _bytes;

        internal int Reads;

        internal PngSource()
        {
            using var bitmap = new SKBitmap(32, 16);

            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.CornflowerBlue);
            }

            using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);

            _bytes = data.ToArray();
        }

        public Stream Open(string name)
        {
            Reads++;

            return new MemoryStream(_bytes);
        }
    }

    [TestClass]
    public class SvgImageTests
    {
        private const int SIZE = 40;
        private const string ICON = "icon.svg";

        private SvgSource _source;
        private VisualElement _root;
        private Image _image;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        [TestInitialize]
        public void Setup()
        {
            _source = new SvgSource();
            _source.Files[ICON] =
                "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 20 10\">"
                + "<rect x=\"0\" y=\"0\" width=\"20\" height=\"10\" fill=\"#0000FF\"/></svg>";

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _image = new Image { Name = "icon", Source = ICON };
            _image.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _image.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            _root.AddChild(_image);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                ImageSource = _source
            };

            _bitmap = new SKBitmap(SIZE, SIZE);
            _canvas = new SKCanvas(_bitmap);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
        }

        private void Size(float width, float height)
        {
            _image.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            _image.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            _image.Invalidate();
        }

        private void Frame()
        {
            _surface.ComputeLayout(SIZE, SIZE);
            _surface.Render(_canvas);
        }

        private SKColor At(int x, int y) => _bitmap.GetPixel(x, y);

        private static bool IsBlue(SKColor c) => c.Blue > 200 && c.Red < 80 && c.Green < 80;

        private static bool IsPaper(SKColor c) => c.Red > 200 && c.Green > 200 && c.Blue > 200;

        [TestMethod]
        public void TheViewBoxIsTheNaturalSize()
        {
            _image.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            _image.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            _image.Invalidate();

            Frame();

            Assert.AreEqual(20f, _image.ActualWidth,
                "an SVG measures like any other picture - its header is its view box");
            Assert.AreEqual(10f, _image.ActualHeight);
        }

        [TestMethod]
        public void ItReachesTheScreen()
        {
            Size(20, 10);
            Frame();

            Assert.IsTrue(IsBlue(At(10, 5)), "the rectangle is painted");
            Assert.IsTrue(IsPaper(At(30, 30)), "and nothing outside it is");
        }

        [TestMethod]
        public void AShapeWithNoFillPaintsNothing()
        {
            _source.Files[ICON] =
                "<svg viewBox=\"0 0 20 10\"><rect width=\"20\" height=\"10\" fill=\"none\"/></svg>";

            Size(20, 10);
            Frame();

            Assert.IsTrue(IsPaper(At(10, 5)));
        }

        [TestMethod]
        public void AStrokeIsPaintedToo()
        {
            _source.Files[ICON] =
                "<svg viewBox=\"0 0 20 10\">"
                + "<rect x=\"2\" y=\"2\" width=\"16\" height=\"6\" fill=\"none\" "
                + "stroke=\"#0000FF\" stroke-width=\"2\"/></svg>";

            Size(20, 10);
            Frame();

            Assert.IsTrue(IsBlue(At(2, 5)), "the left edge is drawn");
            Assert.IsTrue(IsPaper(At(10, 5)), "and the inside is left alone");
        }

        [TestMethod]
        public void ANamedColourIsResolved()
        {
            _source.Files[ICON] =
                "<svg viewBox=\"0 0 20 10\"><rect width=\"20\" height=\"10\" fill=\"blue\"/></svg>";

            Size(20, 10);
            Frame();

            Assert.IsTrue(IsBlue(At(10, 5)),
                "SKColor.TryParse takes hex and nothing else, so the names come from the table "
                + "Color already declares");
        }

        [TestMethod]
        public void ATransformMovesTheShape()
        {
            _source.Files[ICON] =
                "<svg viewBox=\"0 0 20 10\"><g transform=\"translate(10 0)\">"
                + "<rect width=\"10\" height=\"10\" fill=\"#0000FF\"/></g></svg>";

            Size(20, 10);
            Frame();

            Assert.IsTrue(IsPaper(At(2, 5)), "the bar started on the left and was pushed right");
            Assert.IsTrue(IsBlue(At(15, 5)));
        }

        [TestMethod]
        public void SomethingThatIsNotAPictureAtAllDrawsNothing()
        {
            _source.Files[ICON] = "<html><body/></html>";

            Size(20, 10);
            Frame();

            Assert.IsTrue(IsPaper(At(10, 5)),
                "a bad file is a picture that is not there, not an exception");
        }

        private static ImageStore Store(out SvgSource source, string body)
        {
            source = new SvgSource();
            source.Files[ICON] = "<svg viewBox=\"0 0 20 10\">" + body + "</svg>";

            return new ImageStore { Source = source };
        }

        [TestMethod]
        public void AVectorIsRasterisedAtTheSizeItIsDrawn()
        {
            ImageStore store = Store(out SvgSource source,
                "<rect width=\"20\" height=\"10\" fill=\"#0000FF\"/>");

            SKBitmap small = store.Get(ICON, 20, 10);

            Assert.AreEqual(20, small.Width);

            SKBitmap large = store.Get(ICON, 200, 100);

            Assert.AreEqual(200, large.Width,
                "a vector has no pixels of its own, so asking for it big does not upscale it - "
                + "that is the whole difference from a PNG, which can only ever divide");
            Assert.AreEqual(100, large.Height);

            Assert.IsTrue(IsBlue(large.GetPixel(100, 50)),
                "and the shape is drawn at that size rather than in the corner of a bigger "
                + "canvas - the document is scaled to the bitmap it is asked for");
            Assert.IsTrue(IsBlue(large.GetPixel(195, 95)));
        }

        [TestMethod]
        public void AViewBoxWithAnOriginDecidesWhatIsShown()
        {
            var source = new SvgSource();

            source.Files[ICON] =
                "<svg viewBox=\"10 10 20 10\">"
                + "<rect x=\"10\" y=\"10\" width=\"20\" height=\"10\" fill=\"#0000FF\"/></svg>";

            var store = new ImageStore { Source = source };

            SKBitmap drawn = store.Get(ICON, 20, 10);

            Assert.IsTrue(IsBlue(drawn.GetPixel(10, 5)),
                "the window starts at 10,10 and the rectangle sits exactly in it, so it fills "
                + "the picture rather than hanging off the bottom right of it");
            Assert.IsTrue(IsBlue(drawn.GetPixel(1, 1)));
        }

        [TestMethod]
        public void AskingSmallerKeepsWhatIsAlreadyThere()
        {
            ImageStore store = Store(out SvgSource source,
                "<rect width=\"20\" height=\"10\" fill=\"#0000FF\"/>");

            store.Get(ICON, 200, 100);
            SKBitmap again = store.Get(ICON, 20, 10);

            Assert.AreEqual(200, again.Width,
                "the same wart a decoded picture has: shrinking would re-rasterise on every step "
                + "of a resize drag");
        }

        [TestMethod]
        public void TheFileIsReadTwiceAtMostAndThenNotAgain()
        {
            ImageStore store = Store(out SvgSource source,
                "<rect width=\"20\" height=\"10\" fill=\"#0000FF\"/>");

            store.TryMeasure(ICON, out _, out _);

            Assert.AreEqual(2, source.Reads,
                "once for the codec, which refuses it, and once for the reader that takes it");

            store.Get(ICON, 20, 10);
            store.Get(ICON, 20, 10);

            Assert.AreEqual(2, source.Reads,
                "and the parsed document is kept, so drawing it never opens the file again");
        }

        [TestMethod]
        public void AVectorIsCountedAgainstTheBudgetLikeAnythingElse()
        {
            ImageStore store = Store(out SvgSource source,
                "<rect width=\"20\" height=\"10\" fill=\"#0000FF\"/>");

            store.Get(ICON, 100, 50);

            Assert.AreEqual(100 * 50 * 4, store.Bytes);
        }

        [TestMethod]
        public void ARasterIsStillReadWithOneOpen()
        {
            var raster = new PngSource();
            var store = new ImageStore { Source = raster };

            Assert.IsTrue(store.TryMeasure("photo.png", out _, out _));

            Assert.AreEqual(1, raster.Reads,
                "the second open only happens when the codec refuses the file, so nothing that "
                + "was already working pays for this");
        }
    }
}
