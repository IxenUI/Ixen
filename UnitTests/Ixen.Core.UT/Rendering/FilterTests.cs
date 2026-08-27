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
    public class FilterTests
    {
        private const int VIEWPORT = 160;
        private const int BOX = 60;
        private const int LEFT = 50;
        private const int TOP = 50;

        private VisualElement _root;
        private VisualElement _card;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _card = new VisualElement { Name = "card" };
            _card.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            _card.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            _card.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = LEFT };
            _card.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = TOP };
            _card.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };

            _root.AddChild(_card);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private static FilterStyleDescriptor Parse(string value)
        {
            var source = new XnsSource($"probe {{ filter: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return (FilterStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var source = new XnsSource($"probe {{ filter: {value} }}");
            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'filter: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, source.Diagnostics[0].Code);
        }

        private void Declare(string value)
        {
            _card.Styles.Filter = Parse(value);
            _card.Invalidate();
        }

        private SKBitmap Render()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return _surface.RenderToBitmap();
        }

        private static int Grey(SKBitmap bitmap, int x, int y) => bitmap.GetPixel(x, y).Red;

        [TestMethod]
        public void WithNoFilterTheEdgeIsHard()
        {
            using (SKBitmap rendered = Render())
            {
                Assert.AreEqual(0, Grey(rendered, LEFT + 1, TOP + BOX / 2), "inside is black");
                Assert.AreEqual(255, Grey(rendered, LEFT - 3, TOP + BOX / 2), "and outside is white");
            }
        }

        [TestMethod]
        public void ABlurSoftensTheEdge()
        {
            Declare("blur(4px)");

            using (SKBitmap rendered = Render())
            {
                int outside = Grey(rendered, LEFT - 3, TOP + BOX / 2);

                Assert.IsTrue(outside > 0 && outside < 255,
                    $"the black spills past the box and fades; got {outside}");

                int further = Grey(rendered, LEFT - 8, TOP + BOX / 2);

                Assert.IsTrue(further > outside,
                    $"and it keeps fading outwards: {outside} at 3px, {further} at 8px");
            }
        }

        [TestMethod]
        public void ABiggerRadiusReachesFurther()
        {
            Declare("blur(2px)");

            int tight;

            using (SKBitmap rendered = Render())
            {
                tight = Grey(rendered, LEFT - 6, TOP + BOX / 2);
            }

            Declare("blur(8px)");

            using (SKBitmap rendered = Render())
            {
                int wide = Grey(rendered, LEFT - 6, TOP + BOX / 2);

                Assert.IsTrue(wide < tight,
                    $"a wider blur puts more ink at the same distance; {tight} became {wide}");
            }
        }

        [TestMethod]
        public void TheBlurAppliesToTheWholeSubtree()
        {
            _card.Styles.Background = new BackgroundStyleDescriptor();

            var inner = new VisualElement { Name = "inner" };
            inner.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            inner.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };

            _card.AddChild(inner);

            Declare("blur(4px)");

            using (SKBitmap rendered = Render())
            {
                int outside = Grey(rendered, LEFT - 3, TOP + BOX / 2);

                Assert.IsTrue(outside > 0 && outside < 255,
                    $"a child is blurred by its parent's filter; got {outside}");
            }
        }

        [TestMethod]
        public void NoneIsAValidAndEmptyFilter()
        {
            FilterStyleDescriptor descriptor = Parse("none");

            Assert.AreEqual(0, descriptor.Count);
            Assert.IsFalse(descriptor.IsDeclared,
                "filter is not inherited, so an empty list needs no flag to mean nothing");
        }

        [TestMethod]
        public void AZeroRadiusPaintsNormally()
        {
            Declare("blur(0px)");

            using (SKBitmap rendered = Render())
            {
                Assert.AreEqual(255, Grey(rendered, LEFT - 3, TOP + BOX / 2),
                    "Skia treats a zero sigma as the identity, so the edge stays hard and the "
                    + "chain needs no guard of its own");
            }
        }

        [TestMethod]
        public void ItTakesNoLayoutSpaceAndDoesNotMoveTheBox()
        {
            Declare("blur(6px)");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual((float)LEFT, _card.X);
            Assert.AreEqual((float)BOX, _card.ActualWidth);
        }

        [TestMethod]
        public void ItDoesNotChangeHitTesting()
        {
            Declare("blur(6px)");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(_card, _surface.HitTest(LEFT + 2, TOP + 2),
                "the hit area is the declared box, as in CSS");

            Assert.AreEqual(_root, _surface.HitTest(LEFT - 4, TOP + 2),
                "and the blur that spills outside does not answer");
        }

        [TestMethod]
        public void ItCombinesWithAnOpacityLayer()
        {
            _card.Styles.Opacity = new OpacityStyleDescriptor { Value = 0.5f };
            Declare("blur(4px)");

            using (SKBitmap rendered = Render())
            {
                int inside = Grey(rendered, LEFT + BOX / 2, TOP + BOX / 2);

                Assert.IsTrue(inside > 100 && inside < 160,
                    $"blurred and faded, so the middle is mid grey; got {inside}. Which of the two "
                    + "layers is inner is unobservable, since a blur is a linear convolution and a "
                    + "uniform alpha is a scalar, so the two commute");
            }
        }

        [TestMethod]
        public void TheCanvasStackIsBalancedAcrossFrames()
        {
            Declare("blur(4px)");

            var sibling = new VisualElement { Name = "sibling" };
            sibling.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            sibling.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            sibling.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 5 };
            sibling.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 5 };
            sibling.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };

            _root.AddChild(sibling);

            using (SKBitmap first = Render())
            {
                Assert.AreEqual(0, Grey(first, 10, 10),
                    "the unfiltered sibling has a hard edge, so the layer was popped");
            }

            _root.Invalidate();

            using (SKBitmap second = Render())
            {
                Assert.AreEqual(0, Grey(second, 10, 10),
                    "and it still does on the next frame, so EndFrame unwound it");
            }
        }

        [TestMethod]
        public void AFilteredElementWithChildrenStillPopsItsLayer()
        {
            var inner = new VisualElement { Name = "inner" };
            inner.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            inner.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _card.AddChild(inner);
            Declare("blur(4px)");

            var sibling = new VisualElement { Name = "sibling" };
            sibling.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };
            sibling.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };
            sibling.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 4 };
            sibling.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 4 };
            sibling.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };

            _root.AddChild(sibling);

            using (SKBitmap rendered = Render())
            {
                Assert.AreEqual(0, Grey(rendered, 8, 8),
                    "the card has children, so it takes the recursing path; the sibling after it "
                    + "must not be drawn into a layer that was left open");

                Assert.AreEqual(255, Grey(rendered, 4 + 24 + 3, 8),
                    "and just past the sibling the ground is untouched, so nothing blurred it");
            }
        }

        [TestMethod]
        public void TwoBlursCompose()
        {
            Declare("blur(4px)");

            int single;

            using (SKBitmap rendered = Render())
            {
                single = Grey(rendered, LEFT - 8, TOP + BOX / 2);
            }

            Declare("blur(3px) blur(4px)");

            using (SKBitmap rendered = Render())
            {
                int chained = Grey(rendered, LEFT - 8, TOP + BOX / 2);

                Assert.IsTrue(chained < single,
                    "the second blur takes the first as its input, so the two compose into a "
                    + $"wider one; {single} became {chained}");
            }
        }

        [TestMethod]
        public void SeveralFunctionsKeepTheirOrder()
        {
            FilterStyleDescriptor descriptor = Parse("blur(2px) blur(3px)");

            Assert.AreEqual(2, descriptor.Count, "the grammar is a list, as in CSS");
            Assert.AreEqual(2f, descriptor.Operations[0].Value);
            Assert.AreEqual(3f, descriptor.Operations[1].Value);
        }

        [TestMethod]
        public void ThePxSuffixIsOptional()
        {
            Assert.AreEqual(5f, Parse("blur(5)").Operations.Single().Value);
            Assert.AreEqual(2.5f, Parse("blur(2.5px)").Operations.Single().Value);
        }

        [TestMethod]
        public void NonsenseIsRejected()
        {
            AssertRejected("blur()");
            AssertRejected("blur(-2px)");
            AssertRejected("blur(2px");
            AssertRejected("blur(2em)");
            AssertRejected("wobble(2px)");
            AssertRejected("4px");
            AssertRejected("grayscale(1)");
        }

        [TestMethod]
        public void ItRoundTripsThroughGeneratedSource()
        {
            string source = Parse("blur(3.5px)").ToSource();

            StringAssert.Contains(source, "FilterKind.Blur");
            StringAssert.Contains(source, "Value = 3.5f");
        }
    }
}
