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
        public void ABackendThatDoesNotKeepTheLastFrameRepaintsEverything()
        {
            _surface.PreservesFrame = false;

            Mark(320, 240);

            _surface.InvalidateVisual(_near);
            Frame();

            Assert.IsFalse(Survived(320, 240),
                "clipping the render to the damage is only safe while something keeps the pixels "
                + "outside it. A double-buffered GL swap chain does not: after the swap the back "
                + "buffer holds the frame before last, so a damaged frame would show two frames "
                + "alternating everywhere except the damaged rectangle.");
        }

        [TestMethod]
        public void AndItIsTheDefaultThatKeepsThemBecauseARasterBufferDoes()
        {
            Assert.IsTrue(_surface.PreservesFrame,
                "the pixel buffer the raster host blits from persists between frames, so the "
                + "default must stay true or damage rectangles buy nothing anywhere");
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
        public void ALayoutPassJoinsTheDamageAlreadyAccumulated()
        {
            Mark(320, 240);

            _surface.InvalidateVisual(_near);

            _near.Styles.Width.Value = 90;
            _near.InvalidateLayout();

            Frame();

            Assert.AreEqual(new SKColor(76, 110, 245), _bitmap.GetPixel(100, 30),
                "the element grew from 40 wide to 90, and the band it grew into is painted");
            Assert.IsTrue(Survived(320, 240),
                "this test used to assert the opposite, because a layout pass forced the region "
                + "whole. It does not any more: a geometry change contributes the union of its "
                + "own old and new bounds, so the far element is left alone");
        }

        [TestMethod]
        public void ALayoutPassRepaintsWhatMovedAndNotTheRest()
        {
            Mark(320, 240);
            Mark(50, 30);

            _near.Styles.Width.Value = 60;
            _near.InvalidateLayout();
            Frame();

            Assert.IsFalse(Survived(50, 30),
                "what changed is repainted - that half has to hold first, because a damage "
                + "region that is too tight is a stale pixel and no figure in this repo could "
                + "catch one");
            Assert.IsTrue(Survived(320, 240),
                "and what did not change is not. Every frame that lays out used to repaint the "
                + "whole window - 0,039 ms against 1,193 on the demo, a factor of thirty, with "
                + "83% of the pixels unable to have changed");
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
        public void ADropShadowsOffsetIsCoveredByTheDamage()
        {
            _near.Styles.Filter = new FilterStyleDescriptor();
            _near.Styles.Filter.Operations.Add(new FilterOperation
            {
                Kind = FilterKind.DropShadow,
                Shadow = new Shadow
                {
                    OffsetX = 0,
                    OffsetY = 60,
                    Blur = 0,
                    Color = "#FF000000"
                }
            });

            _near.Invalidate();
            Frame();

            Mark(30, 100);

            _surface.InvalidateVisual(_near);
            Frame();

            Assert.IsFalse(Survived(30, 100),
                "the element is 40 tall at y 20, so a shadow offset by 60 lands at y 80 to 120 - "
                + "entirely outside its own bounds. The damage is grown by FilterChain.Margin, "
                + "which therefore has to count a drop shadow's offset and not only a blur");
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

        private VisualElement Column(out VisualElement first, out VisualElement second,
            out VisualElement third, out IxenSurface surface)
        {
            var root = new VisualElement { Name = "column" };

            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#101010" };

            first = Row("first", "#4C6EF5");
            second = Row("second", "#E8590C");
            third = Row("third", "#2F9E44");

            root.AddChildren(first, second, third);

            surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            surface.ComputeLayout(WIDTH, HEIGHT);
            surface.Render(_canvas);

            return root;
        }

        private static VisualElement Row(string name, string colour)
        {
            var row = new VisualElement { Name = name };

            row.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            row.Styles.Background = new BackgroundStyleDescriptor { Color = colour };

            return row;
        }

        [TestMethod]
        public void AChangeOfColourRepaintsTheElementItChangedOn()
        {
            _near.Styles.Background = new BackgroundStyleDescriptor { Color = "#E8590C" };
            _near.Invalidate();

            Frame();

            Assert.AreEqual(new SKColor(232, 89, 12), _bitmap.GetPixel(30, 30),
                "the whole point of a tight damage region is that it still repaints what changed");
        }

        [TestMethod]
        public void AndNothingElse()
        {
            Mark(320, 240);

            _near.Styles.Background = new BackgroundStyleDescriptor { Color = "#E8590C" };
            _near.Invalidate();

            Frame();

            Assert.IsTrue(Survived(320, 240),
                "a style change on one element is a layout frame, and a layout frame used to "
                + "repaint the whole window - 0,039 ms against 1,193 on the demo");
        }

        [TestMethod]
        public void ChangingTheTextRepaintsTheElement()
        {
            _near.Styles.Color = new ColorStyleDescriptor { Value = "#FFFFFF" };
            _near.Styles.FontSize = new FontSizeStyleDescriptor { Value = 30 };
            _near.Invalidate();

            Frame();

            _near.Text = "IIII";

            Frame();

            bool inked = false;

            for (int x = 20; x < 60 && !inked; x++)
            {
                for (int y = 20; y < 60 && !inked; y++)
                {
                    inked = _bitmap.GetPixel(x, y) == new SKColor(255, 255, 255);
                }
            }

            Assert.IsTrue(inked, "a keystroke has to reach the pixels of the field it was typed in");
        }

        [TestMethod]
        public void AnElementThatGrewRepaintsBothItsOldAndItsNewBox()
        {
            _near.Styles.Width.Value = 200;
            _near.InvalidateLayout();

            Frame();

            Assert.AreEqual(new SKColor(76, 110, 245), _bitmap.GetPixel(150, 30),
                "the new box is painted");

            _near.Styles.Width.Value = 40;
            _near.InvalidateLayout();

            Mark(150, 30);

            Frame();

            Assert.IsFalse(Survived(150, 30),
                "and shrinking has to repaint the band the element has just left, which is what "
                + "the union of the old and the new bounds is for");
        }

        [TestMethod]
        public void ASiblingThatMovedIsRepaintedAlthoughNobodyInvalidatedIt()
        {
            Column(out VisualElement first, out _, out _,
                out IxenSurface surface);

            Assert.AreEqual(new SKColor(232, 89, 12), _bitmap.GetPixel(10, 50),
                "second starts at y 40, under a 40-tall first");

            first.Styles.Height.Value = 80;
            first.InvalidateLayout();

            surface.ComputeLayout(WIDTH, HEIGHT);
            surface.Render(_canvas);

            Assert.AreEqual(new SKColor(76, 110, 245), _bitmap.GetPixel(10, 50),
                "first is 80 tall now, so what used to be second's first row is first's");
            Assert.AreEqual(new SKColor(232, 89, 12), _bitmap.GetPixel(10, 90),
                "and second moved down with it - nothing invalidated second, its CLIP moved, "
                + "which is the other half of the damage predicate");
        }

        [TestMethod]
        public void ARemovedElementLeavesNoGhost()
        {
            VisualElement root = Column(out VisualElement first, out _, out _,
                out IxenSurface surface);

            Assert.AreEqual(new SKColor(76, 110, 245), _bitmap.GetPixel(10, 10));

            root.RemoveChild(first);

            surface.ComputeLayout(WIDTH, HEIGHT);
            surface.Render(_canvas);

            Assert.AreEqual(new SKColor(232, 89, 12), _bitmap.GetPixel(10, 10),
                "an element that leaves the tree is walked by nothing afterwards, so the box it "
                + "used to paint has to be damaged on the way out - ElementDetached is where");
        }

        private VisualElement Scroller(float height, out VisualElement page,
            out IxenSurface surface)
        {
            var root = new VisualElement { Name = "root" };

            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#101010" };

            page = new VisualElement { Name = "page", Scrollable = true };

            page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            for (int index = 0; index < 12; index++)
            {
                page.AddChild(Row("row" + index, index % 2 == 0 ? "#4C6EF5" : "#E8590C"));
            }

            root.AddChild(page);

            surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            surface.ComputeLayout(WIDTH, HEIGHT);
            surface.Render(_canvas);

            return root;
        }

        [TestMethod]
        public void ScrolledContentIsRepaintedWhereItMoved()
        {
            Scroller(280, out VisualElement page, out IxenSurface surface);

            Assert.AreEqual(new SKColor(76, 110, 245), _bitmap.GetPixel(10, 10));

            page.ScrollBy(0, 40);

            surface.ComputeLayout(WIDTH, HEIGHT);
            surface.Render(_canvas);

            Assert.AreEqual(new SKColor(232, 89, 12), _bitmap.GetPixel(10, 10),
                "a scroll moves every child, so every child's clip moved and every one of them "
                + "is repainted - a scroll is the interaction a stale pixel would show on first");
        }

        [TestMethod]
        public void AnAnimatedSizeRepaintsWhatItUncovers()
        {
            var scheduler = new FakeScheduler();

            Column(out VisualElement first, out _, out _, out IxenSurface surface);

            surface.Scheduler = scheduler;

            first.Styles.Transition = new TransitionStyleDescriptor();
            first.Styles.Transition.Specs.Add(Ixen.Core.Visual.Styles.StyleIdentifier.HEIGHT,
                new TransitionSpec { Duration = 160 });

            first.Invalidate();

            surface.ComputeLayout(WIDTH, HEIGHT);
            surface.Render(_canvas);

            first.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 90 };
            first.Invalidate();

            surface.ComputeLayout(WIDTH, HEIGHT);
            surface.Render(_canvas);

            for (int tick = 0; tick < 12; tick++)
            {
                scheduler.FireAll();
                surface.ComputeLayout(WIDTH, HEIGHT);
                surface.Render(_canvas);
            }

            Assert.AreEqual(new SKColor(76, 110, 245), _bitmap.GetPixel(10, 80),
                "a height transition is a layout pass per tick, and each one has to repaint the "
                + "band the element grew into");
            Assert.AreEqual(new SKColor(232, 89, 12), _bitmap.GetPixel(10, 100),
                "and the sibling it pushed down");
        }

        [TestMethod]
        public void AnElementScrolledOutOfViewLeavesNothingBehind()
        {
            Scroller(100, out VisualElement page, out IxenSurface surface);

            Assert.AreEqual(new SKColor(76, 110, 245), _bitmap.GetPixel(10, 10));

            page.ScrollBy(0, 200);

            surface.ComputeLayout(WIDTH, HEIGHT);
            surface.Render(_canvas);

            Assert.AreEqual(new SKColor(232, 89, 12), _bitmap.GetPixel(10, 10),
                "row 0 is off the top now and row 5 is at the top instead. Row 0's new clip is "
                + "void, so a damage walk that skips a void clip entirely would never repaint "
                + "the place it used to be - the OLD box has to be damaged even when the new "
                + "one is nothing");
        }

        [TestMethod]
        public void AChangeInsideABlurredSubtreeDamagesTheWholeBlur()
        {
            var blurred = new VisualElement { Name = "blurred" };

            blurred.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 150 };
            blurred.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 150 };
            blurred.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            blurred.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            blurred.Styles.Background = new BackgroundStyleDescriptor { Color = "#2E3138" };
            blurred.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            blurred.Styles.Filter = new FilterStyleDescriptor();
            blurred.Styles.Filter.Operations.Add(new FilterOperation
            {
                Kind = FilterKind.Blur,
                Value = 6
            });

            VisualElement chip = Box("chip", 10, 10, "#4C6EF5");

            blurred.AddChild(chip);
            _root.AddChild(blurred);

            Frame();

            Mark(260, 260);

            chip.Styles.Background = new BackgroundStyleDescriptor { Color = "#E8590C" };
            chip.Invalidate();

            Frame();

            Assert.IsFalse(Survived(260, 260),
                "a filter is a SaveLayer, and Skia intersects that layer with whatever clip is "
                + "in force - so a blur computed inside a tight damage rectangle is computed "
                + "from a truncated source and differs near the cut. A change anywhere inside a "
                + "filtered subtree therefore damages the filtering ancestor whole.");
        }

        [TestMethod]
        public void ABackdropFilterIsRepaintedWheneverAnythingIsLaidOut()
        {
            var frosted = new VisualElement { Name = "frosted" };

            frosted.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 150 };
            frosted.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 150 };
            frosted.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            frosted.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            frosted.Styles.Background = new BackgroundStyleDescriptor { Color = "#3000FF00" };
            frosted.Styles.BackdropFilter = new BackdropFilterStyleDescriptor();
            frosted.Styles.BackdropFilter.Operations.Add(new FilterOperation
            {
                Kind = FilterKind.Blur,
                Value = 6
            });

            _root.AddChild(frosted);

            Frame();

            Mark(260, 260);

            _near.Styles.Background = new BackgroundStyleDescriptor { Color = "#E8590C" };
            _near.Invalidate();

            Frame();

            Assert.IsFalse(Survived(260, 260),
                "a backdrop reads what is already on the canvas, so it has to be recomputed "
                + "whenever anything at all moved - and its blur has the same truncation "
                + "problem as an ordinary filter. It is cheap to be conservative here because "
                + "a frame that lays out nothing never reaches the damage walk at all.");
        }

        [TestMethod]
        public void AColourChangeIsNotLostWhenSomethingElseMovedAsWell()
        {
            _near.Styles.Width.Value = 90;
            _near.InvalidateLayout();

            _far.Styles.Background = new BackgroundStyleDescriptor { Color = "#2F9E44" };
            _far.Invalidate();

            Frame();

            Assert.AreEqual(new SKColor(47, 158, 68), _bitmap.GetPixel(320, 240),
                "one element moved and another changed colour without moving. A region that is "
                + "EMPTY still repaints the whole surface, so a mark that is never set degrades "
                + "to a full frame and looks harmless - it is only once something else is "
                + "damaged that the missed element becomes a stale pixel. This is the shape "
                + "that discriminates.");
        }

        [TestMethod]
        public void AHiddenBackdropFilterCostsNothing()
        {
            var frosted = new VisualElement { Name = "frosted" };

            frosted.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 150 };
            frosted.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 150 };
            frosted.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            frosted.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            frosted.Styles.Background = new BackgroundStyleDescriptor { Color = "#3000FF00" };
            frosted.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            frosted.Styles.BackdropFilter = new BackdropFilterStyleDescriptor();
            frosted.Styles.BackdropFilter.Operations.Add(new FilterOperation
            {
                Kind = FilterKind.Blur,
                Value = 6
            });

            _root.AddChild(frosted);

            Frame();

            Mark(260, 260);

            _near.Styles.Background = new BackgroundStyleDescriptor { Color = "#E8590C" };
            _near.Invalidate();

            Frame();

            Assert.IsTrue(Survived(260, 260),
                "a closed dialog is HIDDEN rather than destroyed, so its frosted scrim is still "
                + "an element with a clip the size of the viewport. Repainting it unconditionally "
                + "is what a first cut of this did, and it cost the whole feature: a keystroke on "
                + "the demo still repainted the window because the Layers section's scrim asked "
                + "for it from a tab nobody was looking at.");
        }

        [TestMethod]
        public void TheRegionIsRoundedOutToWholePixelsAndThenGrownByOne()
        {
            var odd = new VisualElement { Name = "odd" };

            odd.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100.5f };
            odd.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100.5f };
            odd.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40.5f };
            odd.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40.5f };
            odd.Styles.Background = new BackgroundStyleDescriptor { Color = "#4C6EF5" };

            _root.AddChild(odd);

            Frame();

            Mark(100, 120);
            Mark(99, 120);

            _surface.InvalidateVisual(odd);
            Frame();

            Assert.IsFalse(Survived(100, 120),
                "the element starts at x 100,5, so the pixel at 100 is half covered. A rectangular "
                + "clip is NOT antialiased and Skia rounds it INWARDS, which drops that pixel "
                + "entirely - measured on the demo as 1248 stale pixels along the boundary of "
                + "one damage rectangle. The region is floored and ceilinged before it is "
                + "pushed.");
            Assert.IsFalse(Survived(99, 120),
                "and rounding alone still left 703 of them, because an antialiased edge bleeds "
                + "a pixel past the box it belongs to. The region is grown by one unit as well, "
                + "which takes it to one pixel over a seventy-frame run on the demo.");
        }
    }
}
