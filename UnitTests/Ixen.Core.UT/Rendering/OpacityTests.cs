using Ixen.Core.Input;
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
    public class OpacityTests
    {
        private const int VIEWPORT = 100;

        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Opaque(VisualElement element, float value)
        {
            element.Styles.Opacity = new OpacityStyleDescriptor { Value = value };
            element.Invalidate();
        }

        private SKBitmap Render()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return _surface.RenderToBitmap();
        }

        private static int Grey(SKBitmap bitmap, int x, int y) => bitmap.GetPixel(x, y).Red;

        private static OpacityStyleDescriptor Parse(string value)
        {
            var xnsSource = new XnsSource($"box {{ opacity: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (OpacityStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var xnsSource = new XnsSource($"box {{ opacity: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'opacity: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void AFullyOpaqueElementIsUnchanged()
        {
            using (SKBitmap rendered = Render())
            {
                Assert.AreEqual(0, Grey(rendered, 50, 50), "black on white");
            }
        }

        [TestMethod]
        public void HalfOpacityBlendsWithWhatIsBehind()
        {
            Opaque(_box, 0.5f);

            using (SKBitmap rendered = Render())
            {
                int grey = Grey(rendered, 50, 50);

                Assert.IsTrue(grey > 110 && grey < 145, $"expected mid grey, got {grey}");
            }
        }

        [TestMethod]
        public void ZeroOpacityPaintsNothing()
        {
            Opaque(_box, 0f);

            using (SKBitmap rendered = Render())
            {
                Assert.AreEqual(255, Grey(rendered, 50, 50), "the white ground is untouched");
            }
        }

        [TestMethod]
        public void ItAppliesToTheWholeSubtreeNotJustTheBackground()
        {
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            var child = new VisualElement { Name = "child" };
            child.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };
            _box.AddChild(child);

            Opaque(_box, 0.5f);

            using (SKBitmap rendered = Render())
            {
                int grey = Grey(rendered, 50, 50);

                Assert.IsTrue(grey > 110 && grey < 145,
                    $"the child is faded by its parent's opacity, got {grey}");
            }
        }

        [TestMethod]
        public void ASubtreeIsCompositedAsOneGroup()
        {
            var left = new VisualElement { Name = "left" };
            left.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };
            left.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            left.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };

            var right = new VisualElement { Name = "right" };
            right.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };
            right.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            right.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            right.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            _box.Styles.Background = new BackgroundStyleDescriptor();
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _box.AddChildren(left, right);

            Opaque(_box, 0.5f);

            using (SKBitmap rendered = Render())
            {
                int overlap = Grey(rendered, 30, 30);

                Assert.IsTrue(overlap > 110 && overlap < 145,
                    $"two overlapping opaque children flatten inside the layer before it is "
                    + $"composited once, so the overlap is not darker; got {overlap}");
            }
        }

        [TestMethod]
        public void NestedOpacitiesMultiply()
        {
            var child = new VisualElement { Name = "child" };
            child.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };
            _box.Styles.Background = new BackgroundStyleDescriptor();
            _box.AddChild(child);

            Opaque(_box, 0.5f);
            Opaque(child, 0.5f);

            using (SKBitmap rendered = Render())
            {
                int grey = Grey(rendered, 50, 50);

                Assert.IsTrue(grey > 175 && grey < 210,
                    $"0.5 inside 0.5 composites to a quarter of black, got {grey}");
            }
        }

        [TestMethod]
        public void OpacityDoesNotChangeHitTesting()
        {
            Opaque(_box, 0f);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(_box, _surface.HitTest(50, 50),
                "an invisible element still answers a click, as in CSS");
        }

        [TestMethod]
        public void OpacityTakesNoLayoutSpace()
        {
            Opaque(_box, 0.25f);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual((float)VIEWPORT, _box.ActualWidth);
            Assert.AreEqual((float)VIEWPORT, _box.ActualHeight);
        }

        [TestMethod]
        public void TheCanvasStackIsBalancedAcrossFrames()
        {
            Opaque(_box, 0.5f);

            using (SKBitmap first = Render())
            {
                Assert.IsTrue(Grey(first, 50, 50) > 110);
            }

            _root.Invalidate();

            using (SKBitmap second = Render())
            {
                Assert.IsTrue(Grey(second, 50, 50) > 110,
                    "a layer is saved on the same stack as a clip, so EndFrame unwinds it");
            }
        }

        [TestMethod]
        public void ALeafWithNoChildrenStillUnwindsItsLayer()
        {
            Opaque(_box, 0.5f);

            var sibling = new VisualElement { Name = "sibling" };
            sibling.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };
            _root.AddChild(sibling);

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(Grey(rendered, 50, 25) > 110, "the faded leaf");
                Assert.AreEqual(0, Grey(rendered, 50, 75),
                    "and the sibling after it is fully opaque, so the layer was popped");
            }
        }

        [TestMethod]
        public void ANumberAndAPercentageBothParse()
        {
            Assert.AreEqual(0.5f, Parse("0.5").Value);
            Assert.AreEqual(0.5f, Parse("50%").Value);
            Assert.AreEqual(1f, Parse("1").Value);
            Assert.AreEqual(0f, Parse("0").Value);
        }

        [TestMethod]
        public void OneIsNotATransparentValue()
        {
            Assert.IsFalse(Parse("1").IsTransparent);
            Assert.IsFalse(new OpacityStyleDescriptor().IsTransparent);
            Assert.IsTrue(Parse("0.99").IsTransparent);
        }

        [TestMethod]
        public void OutOfRangeAndNonsenseAreRejected()
        {
            AssertRejected("1.5");
            AssertRejected("120%");
            AssertRejected("-0.5");
            AssertRejected("half");
            AssertRejected("0.5px");
        }

        [TestMethod]
        public void ItRoundTripsThroughGeneratedSource()
        {
            StringAssert.Contains(Parse("0.25").ToSource(), "Value = 0.25f");
        }
    }
}
