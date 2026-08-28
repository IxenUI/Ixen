using Ixen.Core.Input;
using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class DamageTests
    {
        private const int WIDTH = 400;
        private const int HEIGHT = 300;

        private static readonly SKColor Scribble = new SKColor(255, 0, 255);

        private VisualElement _root;
        private VisualElement _near;
        private VisualElement _far;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        private static VisualElement Box(string name, float x, float y, string colour)
        {
            var box = new VisualElement { Name = name };

            box.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = x };
            box.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = y };
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            box.Styles.Background = new BackgroundStyleDescriptor { Color = colour };

            return box;
        }

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#101010" };

            _near = Box("near", 20, 20, "#4C6EF5");
            _far = Box("far", 300, 220, "#E8590C");

            _root.AddChildren(_near, _far);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _bitmap = new SKBitmap(WIDTH, HEIGHT);
            _canvas = new SKCanvas(_bitmap);

            Frame();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
        }

        private void Frame()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);
            _surface.Render(_canvas);
        }

        private void Mark(int x, int y)
        {
            _bitmap.SetPixel(x, y, Scribble);
        }

        private bool Survived(int x, int y) => _bitmap.GetPixel(x, y) == Scribble;

        [TestMethod]
        public void AVisualInvalidationRepaintsOnlyThatElement()
        {
            Mark(320, 240);
            Mark(200, 150);

            _surface.InvalidateVisual(_near);
            Frame();

            Assert.IsTrue(Survived(320, 240),
                "a repaint of one element must not touch the far corner - the pixel buffer keeps "
                + "the previous frame there, which is what makes clipping the render safe");
            Assert.IsTrue(Survived(200, 150), "nor the middle of the surface");
        }

        [TestMethod]
        public void AndTheElementItselfIsRepainted()
        {
            Mark(30, 30);

            _surface.InvalidateVisual(_near);
            Frame();

            Assert.IsFalse(Survived(30, 30),
                "the damaged element is inside the clip, so its own pixels are painted again");
        }

        [TestMethod]
        public void AWholeSurfaceInvalidationStillRepaintsEverything()
        {
            Mark(320, 240);

            _surface.InvalidateVisual();
            Frame();

            Assert.IsFalse(Survived(320, 240),
                "the parameterless overload means the caller cannot say what changed, so it has "
                + "to mean the whole surface");
        }

        [TestMethod]
        public void ALayoutPassBeatsADamageAlreadyAccumulated()
        {
            Mark(320, 240);

            _surface.InvalidateVisual(_near);

            _near.Styles.Width.Value = 90;
            _near.InvalidateLayout();

            Frame();

            Assert.IsFalse(Survived(320, 240),
                "the region already held one element when the geometry changed, and a region is "
                + "only ever widened - so the layout pass has to force it whole, or a frame that "
                + "moves things would repaint one element and leave the rest stale");
        }

        [TestMethod]
        public void ALayoutPassRepaintsEverything()
        {
            Mark(320, 240);

            _near.Styles.Width.Value = 60;
            _near.InvalidateLayout();
            Frame();

            Assert.IsFalse(Survived(320, 240),
                "geometry may have moved anywhere, and the damage would have to be the union of "
                + "the old and the new bounds - so a real layout pass forces the whole surface");
        }

        [TestMethod]
        public void TwoDamagedElementsGiveTheirUnion()
        {
            Mark(200, 150);
            Mark(390, 10);

            _surface.InvalidateVisual(_near);
            _surface.InvalidateVisual(_far);
            Frame();

            Assert.IsFalse(Survived(200, 150),
                "the region is one rectangle covering both, so the space between them is in it");
            Assert.IsTrue(Survived(390, 10),
                "but the corner outside that rectangle is still untouched");
        }

        [TestMethod]
        public void AShadowIsCoveredByTheDamage()
        {
            _near.Styles.BoxShadow = new BoxShadowStyleDescriptor();
            _near.Styles.BoxShadow.Shadows.Add(new Shadow
            {
                OffsetX = 0,
                OffsetY = 0,
                Blur = 20,
                Color = "#FF000000"
            });

            _near.Invalidate();
            Frame();

            Mark(12, 12);

            _surface.InvalidateVisual(_near);
            Frame();

            Assert.IsFalse(Survived(12, 12),
                "a shadow paints outside the element's bounds, so the damage is grown by its "
                + "reach - offset plus blur plus spread - or the falloff would be left stale");
        }

        [TestMethod]
        public void AnOuterBorderIsCoveredToo()
        {
            _near.Styles.Border = new BorderStyleDescriptor
            {
                Color = "#FFFFFF",
                Thickness = 8,
                Type = BorderType.Outer
            };

            _near.Invalidate();
            Frame();

            Assert.AreEqual(8f, _near.BorderOutsideLeft);
            Assert.AreEqual(28f, _near.X,
                "an outer border pushes the bounds inwards, so the stroke lives in 20..28");
            Assert.AreEqual(new SKColor(255, 255, 255), _bitmap.GetPixel(24, 48),
                "and the stroke really is painted there, so the next assertion is not vacuous");

            Mark(24, 48);

            _surface.InvalidateVisual(_near);
            Frame();

            Assert.IsFalse(Survived(24, 48),
                "an outer border is painted past the bounds, and the box model already knows how "
                + "far - BorderOutside on each side");
        }

        [TestMethod]
        public void ATransformedElementDamagesTheWholeSurface()
        {
            _near.Styles.Transform = new TransformStyleDescriptor();
            _near.Styles.Transform.Operations.Add(new TransformOperation
            {
                Kind = TransformKind.Translate,
                X = 250,
                Y = 180
            });

            _near.Invalidate();
            Frame();

            Mark(320, 240);

            _surface.InvalidateVisual(_near);
            Frame();

            Assert.IsFalse(Survived(320, 240),
                "a transform paints the element somewhere its bounds do not describe. What "
                + "enforces this is ClippingComputer: it gives a transformed subtree its "
                + "ancestor's clip UNNARROWED so nothing is wrongly culled, and that clip is "
                + "the viewport - so the region is already the whole surface. Tighten that "
                + "conservatism and this test is what tells you the damage went with it.");
        }

        [TestMethod]
        public void ATransformOnAnAncestorDoesTheSame()
        {
            _root.Styles.Transform = new TransformStyleDescriptor();
            _root.Styles.Transform.Operations.Add(new TransformOperation
            {
                Kind = TransformKind.Scale,
                X = 2,
                Y = 2
            });

            _root.Invalidate();
            Frame();

            Mark(320, 240);

            _surface.InvalidateVisual(_near);
            Frame();

            Assert.IsFalse(Survived(320, 240),
                "a descendant of a transformed element inherits the same unnarrowed clip");
        }

        [TestMethod]
        public void AnAnimationTickDamagesOnlyWhatIsAnimating()
        {
            var scheduler = new FakeScheduler();
            _surface.Scheduler = scheduler;

            _near.Styles.Background = new BackgroundStyleDescriptor { Color = "#4C6EF5" };
            _near.Styles.Transition = new TransitionStyleDescriptor();
            _near.Styles.Transition.Specs.Add(Ixen.Core.Visual.Styles.StyleIdentifier.BACKGROUND,
                new TransitionSpec { Duration = 160 });

            _near.Invalidate();
            Frame();

            _near.Styles.Background = new BackgroundStyleDescriptor { Color = "#E8590C" };
            _near.Invalidate();
            Frame();

            Mark(320, 240);

            scheduler.FireAll();
            Frame();

            Assert.IsTrue(Survived(320, 240),
                "the shared ticker used to ask for one repaint of the whole surface after every "
                + "batch, so one pulsing badge repainted the window sixty times a second");
        }

        [TestMethod]
        public void TheFirstFrameOfAllPaintsEverything()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#207020" };

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            using (var bitmap = new SKBitmap(WIDTH, HEIGHT))
            using (var canvas = new SKCanvas(bitmap))
            {
                bitmap.SetPixel(320, 240, Scribble);

                surface.ComputeLayout(WIDTH, HEIGHT);
                surface.Render(canvas);

                Assert.AreNotEqual(Scribble, bitmap.GetPixel(320, 240),
                    "a fresh surface has nothing on screen to keep, so the region starts whole");
            }
        }
    }
}
