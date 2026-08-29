using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class FilterFunctionsTests
    {
        private const int SIZE = 40;

        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _box.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIZE };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIZE };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _bitmap = new SKBitmap(SIZE, SIZE);
            _canvas = new SKCanvas(_bitmap);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
        }

        private SKColor Paint(string colour, params FilterOperation[] operations)
        {
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = colour };
            _box.Styles.Filter = new FilterStyleDescriptor
            {
                Operations = new List<FilterOperation>(operations)
            };

            _box.Invalidate();

            _surface.ComputeLayout(SIZE, SIZE);
            _surface.Render(_canvas);

            return _bitmap.GetPixel(SIZE / 2, SIZE / 2);
        }

        private static FilterOperation Op(FilterKind kind, float value)
            => new FilterOperation { Kind = kind, Value = value };

        private static FilterStyleDescriptor Parse(string value)
        {
            var source = new XnsSource("box { filter: " + value + " }");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, value);

            foreach (StyleDescriptor descriptor in set.Classes[0].Styles)
            {
                if (descriptor is FilterStyleDescriptor filter)
                {
                    return filter;
                }
            }

            Assert.Fail("no filter came out of " + value);

            return null;
        }

        [TestMethod]
        public void GrayscaleFlattensAColourToItsLuminance()
        {
            SKColor result = Paint("#FF0000", Op(FilterKind.Grayscale, 1));

            Assert.AreEqual(result.Red, result.Green);
            Assert.AreEqual(result.Green, result.Blue);
            Assert.IsTrue(result.Red > 40 && result.Red < 70,
                "red weighs 0.213 of the luminance, so full red greys to about 54 and not to 85 "
                + "- a plain average would be the wrong answer and this is what tells them apart");
        }

        [TestMethod]
        public void AndHalfwayIsHalfway()
        {
            SKColor half = Paint("#FF0000", Op(FilterKind.Grayscale, 0.5f));
            SKColor full = Paint("#FF0000", Op(FilterKind.Grayscale, 1));

            Assert.IsTrue(half.Red > full.Red && half.Red < 255);
            Assert.IsTrue(half.Green > full.Green - 60 && half.Green < 255);
        }

        [TestMethod]
        public void SaturateIsGrayscaleRunBackwards()
        {
            SKColor grey = Paint("#FF0000", Op(FilterKind.Grayscale, 1));
            SKColor none = Paint("#FF0000", Op(FilterKind.Saturate, 0));

            Assert.AreEqual(grey, none,
                "grayscale(1) and saturate(0) are the same matrix, which is why one method "
                + "builds both");
        }

        [TestMethod]
        public void AndAboveOneItPushesTheColourOut()
        {
            SKColor plain = Paint("#CC8888");
            SKColor loud = Paint("#CC8888", Op(FilterKind.Saturate, 3));

            Assert.IsTrue(loud.Red - loud.Blue > plain.Red - plain.Blue,
                "saturate has no upper bound, unlike the amounts that clamp at 1");
        }

        [TestMethod]
        public void InvertTurnsWhiteToBlack()
        {
            SKColor result = Paint("#FFFFFF", Op(FilterKind.Invert, 1));

            Assert.IsTrue(result.Red < 8 && result.Green < 8 && result.Blue < 8);
        }

        [TestMethod]
        public void AndHalfInvertLandsInTheMiddle()
        {
            SKColor result = Paint("#FFFFFF", Op(FilterKind.Invert, 0.5f));

            Assert.IsTrue(result.Red > 120 && result.Red < 136,
                "1 - 2 * 0.5 is a scale of zero, so everything collapses onto the 0.5 offset");
        }

        [TestMethod]
        public void BrightnessScalesAndContrastScalesAroundTheMiddle()
        {
            SKColor dim = Paint("#808080", Op(FilterKind.Brightness, 0.5f));

            Assert.IsTrue(dim.Red > 55 && dim.Red < 75, "half of mid grey");

            SKColor flat = Paint("#FFFFFF", Op(FilterKind.Contrast, 0));

            Assert.IsTrue(flat.Red > 120 && flat.Red < 136,
                "contrast(0) collapses everything onto mid grey, which is what the offset of "
                + "(1 - k) / 2 is for - a plain scale would have given black");
        }

        [TestMethod]
        public void SepiaWarmsAGrey()
        {
            SKColor result = Paint("#808080", Op(FilterKind.Sepia, 1));

            Assert.IsTrue(result.Red > result.Green && result.Green > result.Blue,
                "sepia is a fixed matrix, so a neutral grey comes out warm and ordered");
        }

        [TestMethod]
        public void HueRotateMovesRedTowardsGreen()
        {
            SKColor result = Paint("#FF0000", Op(FilterKind.HueRotate, 120));

            Assert.IsTrue(result.Green > result.Red && result.Green > result.Blue,
                "a third of the way round the wheel from red is green");
        }

        [TestMethod]
        public void OpacityFadesTowardsWhatIsBehind()
        {
            SKColor result = Paint("#000000", Op(FilterKind.Opacity, 0.5f));

            Assert.IsTrue(result.Red > 110 && result.Red < 145,
                "black at half alpha over the white root");
        }

        [TestMethod]
        public void TheyComposeInOrder()
        {
            SKColor greyThenInverted = Paint("#FF0000",
                Op(FilterKind.Grayscale, 1), Op(FilterKind.Invert, 1));

            Assert.AreEqual(greyThenInverted.Red, greyThenInverted.Blue);
            Assert.IsTrue(greyThenInverted.Red > 185,
                "grey first then inverted gives a light grey; inverting first would have given "
                + "cyan, so the order really is respected");
        }

        [TestMethod]
        public void EveryFunctionParses()
        {
            Assert.AreEqual(FilterKind.Grayscale, Parse("grayscale(0.4)").Operations[0].Kind);
            Assert.AreEqual(FilterKind.Sepia, Parse("sepia(60%)").Operations[0].Kind);
            Assert.AreEqual(FilterKind.Saturate, Parse("saturate(2)").Operations[0].Kind);
            Assert.AreEqual(FilterKind.Invert, Parse("invert(100%)").Operations[0].Kind);
            Assert.AreEqual(FilterKind.Brightness, Parse("brightness(1.2)").Operations[0].Kind);
            Assert.AreEqual(FilterKind.Contrast, Parse("contrast(150%)").Operations[0].Kind);
            Assert.AreEqual(FilterKind.HueRotate, Parse("hue-rotate(-90deg)").Operations[0].Kind);
            Assert.AreEqual(FilterKind.Opacity, Parse("opacity(0.25)").Operations[0].Kind);
            Assert.AreEqual(2, Parse("grayscale(1) blur(2px)").Operations.Count);
        }

        [TestMethod]
        public void APercentageIsAFraction()
        {
            Assert.AreEqual(0.6f, Parse("sepia(60%)").Operations[0].Value, 0.0001f);
            Assert.AreEqual(1.5f, Parse("contrast(150%)").Operations[0].Value, 0.0001f);
        }

        [TestMethod]
        public void AnAmountThatClampsClampsAndOneThatDoesNotDoesNot()
        {
            Assert.AreEqual(1f, Parse("grayscale(3)").Operations[0].Value,
                "CSS clamps the amounts that mean a proportion, so grayscale(3) is grayscale(1)");
            Assert.AreEqual(3f, Parse("saturate(3)").Operations[0].Value,
                "and leaves alone the ones that do not, because saturate(3) is meaningful");
        }

        [TestMethod]
        public void TheArgumentShapesAreNotInterchangeable()
        {
            Assert.AreEqual(2f, Parse("blur(2)").Operations[0].Value,
                "a bare length is pixels, which blur already allowed and this does not change");

            Refuse("hue-rotate(90)");
            Refuse("grayscale(40deg)");
            Refuse("saturate(2px)");
            Refuse("frost(2px)");
        }

        private static void Refuse(string value)
        {
            var source = new XnsSource("box { filter: " + value + " }");

            source.Compile();

            Assert.IsTrue(source.HasErrors, value + " should be a diagnostic");
        }
    }
}
