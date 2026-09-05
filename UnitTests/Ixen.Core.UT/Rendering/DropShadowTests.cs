using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class DropShadowTests
    {
        private const int VIEWPORT = 200;
        private const int FRAME = 120;
        private const int MARK = 20;
        private const int LEFT = 40;
        private const int TOP = 40;

        private VisualElement _root;
        private VisualElement _frame;
        private VisualElement _mark;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _frame = new VisualElement { Name = "frame" };
            _frame.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _frame.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = FRAME };
            _frame.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = FRAME };
            _frame.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = LEFT };
            _frame.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = TOP };

            _mark = new VisualElement { Name = "mark" };
            _mark.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = MARK };
            _mark.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = MARK };
            _mark.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _mark.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _mark.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };

            _frame.AddChild(_mark);
            _root.AddChild(_frame);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private static FilterStyleDescriptor Parse(string value)
        {
            var source = new XnsSource($"probe {{ filter: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return (FilterStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var source = new XnsSource($"probe {{ filter: {value} }}");
            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'filter: {value}' should have been rejected");
        }

        private void Declare(string value)
        {
            _frame.Styles.Filter = Parse(value);
            _frame.Invalidate();
        }

        private SKBitmap Render()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return _surface.RenderToBitmap();
        }

        private static SKColor At(SKBitmap bitmap, int x, int y) => bitmap.GetPixel(x, y);

        private static bool IsRed(SKColor color)
            => color.Red > 200 && color.Green < 80 && color.Blue < 80;

        private static bool IsWhite(SKColor color)
            => color.Red == 255 && color.Green == 255 && color.Blue == 255;

        [TestMethod]
        public void TheShadowFollowsTheContentAndNotTheBox()
        {
            Declare("drop-shadow(40px 0px 0px #FF0000)");

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(IsRed(At(rendered, LEFT + 40 + MARK / 2, TOP + MARK / 2)),
                    "the shadow lands where the mark is, offset by the shadow");

                Assert.IsTrue(IsWhite(At(rendered, LEFT + 40 + MARK / 2, TOP + FRAME - MARK / 2)),
                    "and nowhere else inside the frame, because a drop shadow follows the "
                    + "alpha of what was painted rather than the element's box");
            }
        }

        [TestMethod]
        public void TheOffsetIsHonouredOnBothAxes()
        {
            Declare("drop-shadow(0px 40px 0px #FF0000)");

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(IsRed(At(rendered, LEFT + MARK / 2, TOP + 40 + MARK / 2)),
                    "a vertical offset moves the shadow down");
            }
        }

        [TestMethod]
        public void TheShadowPaintsOutsideTheElementsOwnBox()
        {
            _mark.Styles.Filter = Parse("drop-shadow(40px 0px 0px #FF0000)");
            _mark.Invalidate();

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(IsRed(At(rendered, LEFT + 40 + MARK / 2, TOP + MARK / 2)),
                    "the shadow lands two mark-widths to the right, well outside the element's "
                    + "own box, and an element is not clipped by itself. Skia sizes the filter layer "
                    + "from the filter, so this passes without our own margin - that margin is "
                    + "for the damage region, see ADropShadowsOffsetIsCoveredByTheDamage");
            }
        }

        [TestMethod]
        public void ARadiusSoftensTheShadow()
        {
            Declare("drop-shadow(40px 0px 8px #FF0000)");

            using (SKBitmap rendered = Render())
            {
                SKColor edge = At(rendered, LEFT + 40 - 3, TOP + MARK / 2);

                Assert.IsTrue(edge.Red > edge.Green && !IsWhite(edge),
                    $"the blur spreads the shadow past its own edge; got {edge}");
            }
        }

        [TestMethod]
        public void TheRadiusIsABoxShadowRadiusRatherThanASigma()
        {
            Declare("drop-shadow(0px 0px 4px #000000)");

            bool shadowReaches;

            using (SKBitmap rendered = Render())
            {
                shadowReaches = !IsWhite(At(rendered, LEFT - 9, TOP + MARK / 2));
            }

            Declare("blur(4px)");

            using (SKBitmap rendered = Render())
            {
                bool blurReaches = !IsWhite(At(rendered, LEFT - 9, TOP + MARK / 2));

                Assert.IsTrue(blurReaches,
                    "blur(4px) is a sigma of 4, so it is still visible nine units out");

                Assert.IsFalse(shadowReaches,
                    "while drop-shadow's radius follows box-shadow's convention - sigma is half "
                    + "of it - so the same number reaches half as far");
            }
        }

        [TestMethod]
        public void ItComposesWithAnotherFunction()
        {
            Declare("drop-shadow(40px 0px 0px #FF0000) grayscale(1)");

            using (SKBitmap rendered = Render())
            {
                SKColor shadow = At(rendered, LEFT + 40 + MARK / 2, TOP + MARK / 2);

                Assert.IsFalse(IsRed(shadow),
                    "the grayscale runs on the result of the drop shadow, so the red is gone");

                Assert.IsFalse(IsWhite(shadow), $"but something is still painted there; got {shadow}");
            }
        }

        [TestMethod]
        public void TheLengthsAndTheColourComeInAnyOrder()
        {
            FilterStyleDescriptor filter = Parse("drop-shadow(#FF0000 2px 3px 4px)");
            Shadow shadow = filter.Operations[0].Shadow;

            Assert.AreEqual(FilterKind.DropShadow, filter.Operations[0].Kind);
            Assert.AreEqual(2f, shadow.OffsetX);
            Assert.AreEqual(3f, shadow.OffsetY);
            Assert.AreEqual(4f, shadow.Blur);
            Assert.AreEqual("#FF0000", shadow.Color);
        }

        [TestMethod]
        public void TheBlurIsOptional()
        {
            Shadow shadow = Parse("drop-shadow(2px 3px #FF0000)").Operations[0].Shadow;

            Assert.AreEqual(0f, shadow.Blur);
        }

        [TestMethod]
        public void AColourIsRequired()
        {
            AssertRejected("drop-shadow(2px 3px 4px)");
        }

        [TestMethod]
        public void ThereIsNoSpread()
        {
            AssertRejected("drop-shadow(2px 3px 4px 5px #FF0000)");
        }

        [TestMethod]
        public void ItRefusesInset()
        {
            AssertRejected("drop-shadow(inset 2px 3px #FF0000)");
        }

        [TestMethod]
        public void ItTakesOneShadowRatherThanAList()
        {
            AssertRejected("drop-shadow(2px 3px #FF0000, 4px 5px #00FF00)");
        }

        [TestMethod]
        public void AGoodEntryFollowedByRubbishIsStillRubbish()
        {
            AssertRejected("drop-shadow(2px 3px #FF0000, wobble)");
        }

        [TestMethod]
        public void ItRoundTripsThroughGeneratedSource()
        {
            string source = Parse("drop-shadow(2px 3px 4px #40102030)").ToSource();

            StringAssert.Contains(source, "FilterKind.DropShadow");
            StringAssert.Contains(source, "OffsetX = 2f");
            StringAssert.Contains(source, "Blur = 4f");
            StringAssert.Contains(source, "\"#40102030\"");
        }
    }
}
