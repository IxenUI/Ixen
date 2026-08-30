using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace Ixen.Core.UT.Rendering
{
    internal class StripeImageSource : IImageSource
    {
        internal const int WIDTH = 40;
        internal const int HEIGHT = 40;

        public Stream Open(string name)
        {
            using var bitmap = new SKBitmap(WIDTH, HEIGHT);
            using var canvas = new SKCanvas(bitmap);

            canvas.Clear(SKColors.White);

            using var paint = new SKPaint { Color = SKColors.Red };

            canvas.DrawRect(new SKRect(0, 0, WIDTH, 4), paint);

            using var blue = new SKPaint { Color = SKColors.Blue };

            canvas.DrawRect(new SKRect(0, HEIGHT - 4, WIDTH, HEIGHT), blue);

            using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);

            return new MemoryStream(data.ToArray());
        }
    }

    [TestClass]
    public class ObjectPositionTests
    {
        private const int SIZE = 40;
        private const int BOX = 20;

        private VisualElement _root;
        private Image _image;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#00FF00" };

            _image = new Image { Name = "picture", Source = "stripes.png" };
            _image.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _image.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _image.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            _image.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            _image.Styles.ObjectFit = new ObjectFitStyleDescriptor { Value = ObjectFit.None };

            _root.AddChild(_image);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ImageSource = new StripeImageSource();

            _bitmap = new SKBitmap(SIZE, SIZE);
            _canvas = new SKCanvas(_bitmap);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
        }

        private void Position(float x, float y)
        {
            _image.Styles.ObjectPosition = new ObjectPositionStyleDescriptor { X = x, Y = y };
            _image.Invalidate();
        }

        private void Frame()
        {
            _surface.ComputeLayout(SIZE, SIZE);
            _surface.Render(_canvas);
        }

        private SKColor At(int x, int y) => _bitmap.GetPixel(x, y);

        private static bool IsRed(SKColor c) => c.Red > 200 && c.Green < 80 && c.Blue < 80;

        private static bool IsBlue(SKColor c) => c.Blue > 200 && c.Red < 80 && c.Green < 80;

        [TestMethod]
        public void ByDefaultAPictureIsCentred()
        {
            Frame();

            Assert.IsFalse(IsRed(At(10, 1)),
                "the picture is twice the box, so with object-fit: none the middle band shows "
                + "and the red stripe at its top is cropped away");
            Assert.IsFalse(IsBlue(At(10, 18)));
        }

        [TestMethod]
        public void TopShowsTheTopOfThePicture()
        {
            Position(0.5f, 0f);
            Frame();

            Assert.IsTrue(IsRed(At(10, 1)),
                "anchoring the picture at the top brings its red stripe into the box - which is "
                + "the whole point: a fitted picture that is always centred cannot show a face "
                + "that is not in the middle");
        }

        [TestMethod]
        public void BottomShowsTheBottom()
        {
            Position(0.5f, 1f);
            Frame();

            Assert.IsTrue(IsBlue(At(10, 18)));
            Assert.IsFalse(IsRed(At(10, 1)));
        }

        [TestMethod]
        public void APercentageLandsBetweenTheKeywords()
        {
            Position(0.5f, 0.25f);
            Frame();

            bool top = IsRed(At(10, 1));

            Position(0.5f, 0f);
            Frame();

            Assert.IsTrue(IsRed(At(10, 1)));
            Assert.IsFalse(top, "25% is not the top, so the red stripe has already gone past it");
        }

        [TestMethod]
        public void TheKeywordsParse()
        {
            Assert.AreEqual(0f, Parse("left top").X);
            Assert.AreEqual(0f, Parse("left top").Y);
            Assert.AreEqual(1f, Parse("right bottom").X);
            Assert.AreEqual(1f, Parse("right bottom").Y);
            Assert.AreEqual(0.5f, Parse("center middle").X);
        }

        [TestMethod]
        public void TheOrderDoesNotMatterAndOneValueSetsOneAxis()
        {
            Assert.AreEqual(0f, Parse("top left").X, "classified by shape, like border and text-align");
            Assert.AreEqual(0f, Parse("top left").Y);

            ObjectPositionStyleDescriptor one = Parse("right");

            Assert.AreEqual(1f, one.X);
            Assert.AreEqual(0.5f, one.Y, "the axis nobody named keeps the centre");
        }

        [TestMethod]
        public void PercentagesWorkWhereBackgroundPositionCannotHaveThem()
        {
            ObjectPositionStyleDescriptor position = Parse("30% 70%");

            Assert.AreEqual(0.3f, position.X, 0.0001f);
            Assert.AreEqual(0.7f, position.Y, 0.0001f,
                "background-position is keyword-only because IsImageName reads an interior dot "
                + "as a file name - object-position shares its value with nothing, so it can");
        }

        [TestMethod]
        public void NonsenseIsRefused()
        {
            Refuse("left right");
            Refuse("top bottom");
            Refuse("left center");
            Refuse("left top right");
            Refuse("30px");
            Refuse("middle-ish");
            Refuse("");
        }

        private static ObjectPositionStyleDescriptor Parse(string value)
        {
            var parser = new ObjectPositionStyleParser(value);

            Assert.IsTrue(parser.IsValid, value);

            return parser.Descriptor;
        }

        private static void Refuse(string value)
        {
            Assert.IsFalse(new ObjectPositionStyleParser(value).IsValid,
                value + " should not parse");
        }

        [TestMethod]
        public void ItComesFromXnsEndToEnd()
        {
            var source = new XnsSource("picture { object-position: center top }");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors);

            var registry = new StyleRegistry();

            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();

            Frame();

            Assert.IsTrue(IsRed(At(10, 1)),
                "the ApplyStyle arm is only ever reached by a rule from a stylesheet");
        }

        [TestMethod]
        public void TheDefaultAllocatesNoHandler()
        {
            Frame();

            Assert.AreSame(VisualElementStylesHandlers.DefaultObjectPosition,
                _image.StylesHandlers.ObjectPosition,
                "an element that says nothing about it shares the one handler, the Unset idiom "
                + "again - and so does one that writes center middle out in full");
        }
    }
}
