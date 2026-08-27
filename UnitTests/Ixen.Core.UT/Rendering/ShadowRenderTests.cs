using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class ShadowRenderTests
    {
        private const int VIEWPORT = 200;
        private const int BOX = 60;
        private const int LEFT = 60;
        private const int TOP = 60;

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
            _card.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _root.AddChild(_card);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Shadow(float x, float y, float blur, string colour, float spread = 0)
        {
            _card.Styles.BoxShadow = new BoxShadowStyleDescriptor
            {
                Shadows =
                {
                    new Ixen.Core.Visual.Styles.Descriptors.Shadow
                    {
                        OffsetX = x,
                        OffsetY = y,
                        Blur = blur,
                        Spread = spread,
                        Color = colour
                    }
                }
            };

            _card.Invalidate();
        }

        private SKBitmap Render()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return _surface.RenderToBitmap();
        }

        private static int Grey(SKBitmap bitmap, int x, int y) => bitmap.GetPixel(x, y).Red;

        [TestMethod]
        public void WithNoShadowTheGroundStaysUntouched()
        {
            using (SKBitmap rendered = Render())
            {
                Assert.AreEqual(255, Grey(rendered, LEFT + 10, TOP + BOX + 6),
                    "nothing is painted under the card");
            }
        }

        [TestMethod]
        public void AShadowDarkensTheGroundBelowTheElement()
        {
            Shadow(0, 6, 0, "#80000000");

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(Grey(rendered, LEFT + 10, TOP + BOX + 3) < 200,
                    "the offset moved it down, so the ground under the card is darkened");
            }
        }

        [TestMethod]
        public void AShadowIsBehindTheElementNotOverIt()
        {
            Shadow(0, 0, 0, "#FF000000");

            using (SKBitmap rendered = Render())
            {
                Assert.AreEqual(255, Grey(rendered, LEFT + BOX / 2, TOP + BOX / 2),
                    "an opaque shadow at offset zero is completely covered by the background");
            }
        }

        [TestMethod]
        public void TheOffsetDecidesWhichSideItFallsOn()
        {
            Shadow(-8, 0, 0, "#80000000");

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(Grey(rendered, LEFT - 4, TOP + BOX / 2) < 200, "left of the card is dark");
                Assert.AreEqual(255, Grey(rendered, LEFT + BOX + 4, TOP + BOX / 2),
                    "and the right is untouched");
            }
        }

        [TestMethod]
        public void TheBlurFadesOutwardsInsteadOfEndingFlat()
        {
            Shadow(0, 0, 16, "#A0000000", spread: 4);

            using (SKBitmap rendered = Render())
            {
                int near = Grey(rendered, LEFT + BOX / 2, TOP + BOX + 3);
                int mid = Grey(rendered, LEFT + BOX / 2, TOP + BOX + 8);
                int far = Grey(rendered, LEFT + BOX / 2, TOP + BOX + 16);

                Assert.IsTrue(near < mid, $"near {near} should be darker than mid {mid}");
                Assert.IsTrue(mid < far, $"mid {mid} should be darker than far {far}");
            }
        }

        [TestMethod]
        public void TheSpreadGrowsItWithoutBlurring()
        {
            Shadow(0, 0, 0, "#80000000", spread: 6);

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(Grey(rendered, LEFT + BOX / 2, TOP + BOX + 3) < 200,
                    "a ring appears on every side");
                Assert.IsTrue(Grey(rendered, LEFT - 3, TOP + BOX / 2) < 200);
                Assert.AreEqual(255, Grey(rendered, LEFT + BOX / 2, TOP + BOX + 9),
                    "and it ends flat at the spread distance");
            }
        }

        [TestMethod]
        public void ARadiusRoundsTheShadowToo()
        {
            _card.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 20,
                TopRight = 20,
                BottomRight = 20,
                BottomLeft = 20
            };

            Shadow(0, 0, 0, "#FF000000", spread: 4);

            using (SKBitmap rendered = Render())
            {
                Assert.AreEqual(255, Grey(rendered, LEFT - 3, TOP - 3),
                    "the corner is cut, so the shadow does not reach the square corner");
                Assert.IsTrue(Grey(rendered, LEFT + BOX / 2, TOP - 3) < 200,
                    "while the middle of the edge is covered");
            }
        }

        [TestMethod]
        public void AShadowIsClippedByTheParentLikeAnyPainting()
        {
            var frame = new VisualElement { Name = "frame" };
            frame.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            frame.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            frame.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            frame.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = LEFT };
            frame.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = TOP };

            _root.RemoveChild(_card);

            _card.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _card.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            frame.AddChild(_card);
            _root.AddChild(frame);

            Shadow(0, 20, 0, "#FF000000");

            using (SKBitmap rendered = Render())
            {
                Assert.AreEqual(255, Grey(rendered, LEFT + 10, TOP + BOX + 6),
                    "the shadow falls outside the frame, and a parent always clips its children");
            }
        }

        [TestMethod]
        public void ATextShadowPaintsAroundTheGlyphs()
        {
            _card.Text = "Ixen";
            _card.Styles.FontSize = new FontSizeStyleDescriptor { Value = 24 };
            _card.Styles.Color = new ColorStyleDescriptor { Value = "#FFFFFF" };
            _card.Styles.TextShadow = new TextShadowStyleDescriptor
            {
                Shadows = { new Ixen.Core.Visual.Styles.Descriptors.Shadow { Blur = 6, Color = "#FF000000" } }
            };

            _card.Invalidate();

            using (SKBitmap rendered = Render())
            {
                int darkest = 255;

                for (int y = TOP; y < TOP + BOX; y++)
                {
                    for (int x = LEFT; x < LEFT + BOX; x++)
                    {
                        darkest = System.Math.Min(darkest, Grey(rendered, x, y));
                    }
                }

                Assert.IsTrue(darkest < 200,
                    "white glyphs on a white card are only visible because of the shadow behind them");
            }
        }

        [TestMethod]
        public void ATextShadowIsInherited()
        {
            var label = new VisualElement { Name = "label" };
            label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            label.Text = "Ixen";

            _card.AddChild(label);

            _card.Styles.TextShadow = new TextShadowStyleDescriptor
            {
                Shadows = { new Ixen.Core.Visual.Styles.Descriptors.Shadow { OffsetX = 1, OffsetY = 2, Blur = 3, Color = "#80112233" } }
            };

            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            TextShadowStyleDescriptor resolved = label.StylesHandlers.TextShadow.Descriptor;

            Assert.AreEqual("#80112233", resolved.First.Color,
                "text-shadow travels with the font properties, as it does in CSS");
            Assert.AreEqual(2f, resolved.First.OffsetY);
        }

        [TestMethod]
        public void ADeclaredShadowStopsTheInheritedOne()
        {
            var label = new VisualElement { Name = "label" };
            label.Text = "Ixen";
            label.Styles.TextShadow = new TextShadowStyleDescriptor { Shadows = { new Ixen.Core.Visual.Styles.Descriptors.Shadow { Blur = 1, Color = "#FF445566" } } };

            _card.AddChild(label);
            _card.Styles.TextShadow = new TextShadowStyleDescriptor { Shadows = { new Ixen.Core.Visual.Styles.Descriptors.Shadow { Blur = 9, Color = "#80112233" } } };

            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#FF445566", label.StylesHandlers.TextShadow.Descriptor.First.Color);
        }

        [TestMethod]
        public void ABoxShadowIsNotInherited()
        {
            var label = new VisualElement { Name = "label" };
            _card.AddChild(label);

            Shadow(0, 4, 8, "#80000000");

            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(label.StylesHandlers.BoxShadow.Descriptor.IsDeclared,
                "a box shadow belongs to the box that declared it");
        }

        [TestMethod]
        public void AnUndeclaredShadowSharesTheDefaultHandler()
        {
            var other = new VisualElement { Name = "other" };
            _root.AddChild(other);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreSame(_card.StylesHandlers.BoxShadow, other.StylesHandlers.BoxShadow,
                "no shadow allocates no handler, like every other unset style");
        }
    }
}
