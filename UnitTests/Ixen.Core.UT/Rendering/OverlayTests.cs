using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class OverlayTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private VisualElement _frame;
        private VisualElement _layer;
        private VisualElement _dialog;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = Element("root");
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _frame = Element("frame");
            Size(_frame, 50, 50);

            _layer = Element("layer");
            _layer.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };

            _dialog = Element("dialog");
            Size(_dialog, 40, 40);
            _dialog.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _dialog.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _dialog.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF0000" };

            _layer.AddChild(_dialog);
            _frame.AddChild(_layer);
            _root.AddChild(_frame);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static VisualElement Element(string name)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            return element;
        }

        private static void Size(VisualElement element, float width, float height)
        {
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
        }

        private static bool IsRed(SKBitmap bitmap, int x, int y)
        {
            SKColor pixel = bitmap.GetPixel(x, y);

            return pixel.Red > 0xC0 && pixel.Green < 0x40 && pixel.Blue < 0x40;
        }

        [TestMethod]
        public void ALayerEscapesItsAncestorsClip()
        {
            Assert.AreEqual(100f, _dialog.X, "it is placed against the viewport, as fixed always did");
            Assert.AreEqual(100f, _dialog.Y);

            Assert.IsFalse(_dialog.Clip.IsVoidOrInvalid,
                "and it is no longer cut away by a 50x50 ancestor");
        }

        [TestMethod]
        public void ALayerIsActuallyPainted()
        {
            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                Assert.IsTrue(IsRed(rendered, 120, 120),
                    "the dialog paints outside the frame that contains it in the tree");
            }
        }

        [TestMethod]
        public void ALayerPaintsOverALaterSibling()
        {
            VisualElement cover = Element("cover");
            Size(cover, VIEWPORT, VIEWPORT);
            cover.Styles.Background = new BackgroundStyleDescriptor { Color = "#0000FF" };

            _root.AddChild(cover);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                Assert.IsTrue(IsRed(rendered, 120, 120),
                    "a layer is painted last, so a sibling declared after it cannot bury it");
            }
        }

        [TestMethod]
        public void ALayerIsHitTestedFirst()
        {
            VisualElement cover = Element("cover");
            Size(cover, VIEWPORT, VIEWPORT);

            _root.AddChild(cover);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(_dialog, _surface.HitTest(120, 120),
                "the topmost thing under the point is the layer's child, not the covering sibling");

            Assert.AreEqual(cover, _surface.HitTest(250, 250),
                "and away from the layer the ordinary tree answers");
        }

        [TestMethod]
        public void TheLastDeclaredLayerWins()
        {
            VisualElement second = Element("second");
            second.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };

            VisualElement top = Element("top");
            Size(top, 40, 40);
            top.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            top.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            second.AddChild(top);
            _root.AddChild(second);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(top, _surface.HitTest(120, 120),
                "two layers stack in document order, with no z-index to write");
        }

        [TestMethod]
        public void AFullViewportLayerSwallowsEveryClick()
        {
            Size(_layer, VIEWPORT, VIEWPORT);
            _layer.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(VIEWPORT, _layer.ActualWidth, "the layer really covers the surface");

            Assert.AreEqual(_layer, _surface.HitTest(250, 250),
                "which is what a modal scrim wants, and the trap for anything else");
        }

        [TestMethod]
        public void AZeroSizedLayerTakesNoSpaceAndStillShowsItsChildren()
        {
            Size(_layer, 0, 0);
            _layer.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0f, _layer.ActualWidth, "it consumes nothing in its parent's layout");

            Assert.AreEqual(_dialog, _surface.HitTest(120, 120),
                "yet its child is still reachable, because a layer is entered through its children");

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                Assert.IsTrue(IsRed(rendered, 120, 120), "and still painted");
            }
        }

        [TestMethod]
        public void AChildStretchedByFourAnchorsFillsTheViewport()
        {
            Size(_layer, 0, 0);

            var scrim = new VisualElement { Name = "scrim" };
            scrim.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            scrim.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            scrim.Styles.Right = new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            scrim.Styles.Bottom = new BottomStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            _layer.AddChild(scrim);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(VIEWPORT, scrim.ActualWidth,
                "the recipe for a modal scrim: a weightless layer holding a stretched child");
            Assert.AreEqual(VIEWPORT, scrim.ActualHeight);

            Assert.AreEqual(scrim, _surface.HitTest(250, 250), "and it swallows clicks like one");
        }

        [TestMethod]
        public void ASmallLayerLetsClicksThroughAroundIt()
        {
            Assert.AreEqual(_dialog, _surface.HitTest(120, 120), "the dialog takes its own area");

            VisualElement elsewhere = _surface.HitTest(250, 250);

            Assert.AreNotEqual(_dialog, elsewhere);
            Assert.AreNotEqual(_layer, elsewhere,
                "a layer sized to its content does not block the rest of the surface");
        }

        [TestMethod]
        public void DroppingFixedPutsTheChildBackUnderTheAncestorClip()
        {
            _layer.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _layer.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(_root.HasOverlays, "the layer list is rebuilt every pass, not latched");
            Assert.IsTrue(_dialog.Clip.IsVoidOrInvalid,
                "and the child is clipped away by its 50x50 ancestor again");
        }

        [TestMethod]
        public void TheLayerListDoesNotAccumulateAcrossLayouts()
        {
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, _root.Overlays.Count,
                "the clipping pass clears the list before collecting");
        }

        [TestMethod]
        public void ALayerIgnoresAnAncestorScroll()
        {
            _frame.Scrollable = true;

            VisualElement filler = Element("filler");
            Size(filler, 40, 400);
            _frame.AddChild(filler);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _frame.ScrollY = 30;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(100f, _dialog.Y,
                "a fixed layer is anchored to the viewport, so scrolling above it moves nothing");
        }
    }
}
