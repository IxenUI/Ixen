using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class SizeTransitionTests
    {
        private const int VIEWPORT = 400;

        private FakeScheduler _scheduler;
        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.WIDTH, new TransitionSpec { Duration = 64 } } }
            };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                Scheduler = _scheduler
            };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void WidthTo(SizeUnit unit, float value)
        {
            _box.Styles.Width = new WidthStyleDescriptor { Unit = unit, Value = value };
            _box.Invalidate();
            Layout();
        }

        [TestMethod]
        public void AWidthWalksToItsNewValue()
        {
            WidthTo(SizeUnit.Pixels, 200);

            Assert.AreEqual(100f, _box.ActualWidth, "it starts where it was");
            Assert.AreEqual(1, _scheduler.PendingCount, "and a ticker is running");

            _scheduler.FireAll();
            Layout();

            Assert.IsTrue(_box.ActualWidth > 100f && _box.ActualWidth < 200f,
                $"a quarter of the way is neither end, was {_box.ActualWidth}");

            _scheduler.FireAll();
            _scheduler.FireAll();
            _scheduler.FireAll();
            Layout();

            Assert.AreEqual(200f, _box.ActualWidth, "four ticks of 16ms cover 64ms");
        }

        [TestMethod]
        public void ATickInvalidatesLayoutRatherThanJustThePaint()
        {
            WidthTo(SizeUnit.Pixels, 200);
            Layout();

            Assert.IsFalse(_surface.IsDirty, "the layout pass cleared it");

            _scheduler.FireAll();

            Assert.IsTrue(_surface.IsDirty,
                "a size needs the four passes again, not a repaint of the same geometry");
        }

        [TestMethod]
        public void AChangeOfUnitSnaps()
        {
            WidthTo(SizeUnit.Percents, 50);

            Assert.AreEqual(200f, _box.ActualWidth,
                "interpolating 100px towards 50% has no answer, so it jumps");
            Assert.AreEqual(0, _scheduler.PendingCount, "and nothing is ticking");
        }

        [TestMethod]
        public void WeightAndContentSnapEvenWithinTheSameUnit()
        {
            WidthTo(SizeUnit.Content, 1);

            Assert.AreEqual(0, _scheduler.PendingCount, "'?' has no midpoint");

            WidthTo(SizeUnit.Weight, 1);

            Assert.AreEqual(0, _scheduler.PendingCount, "nor does a weight share");
        }

        [TestMethod]
        public void APercentageAnimatesAgainstAnotherPercentage()
        {
            WidthTo(SizeUnit.Percents, 25);
            Layout();

            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.WIDTH, new TransitionSpec { Duration = 64 } } }
            };

            WidthTo(SizeUnit.Percents, 75);

            Assert.AreEqual(1, _scheduler.PendingCount);

            _scheduler.FireAll();
            _scheduler.FireAll();
            Layout();

            Assert.IsTrue(_box.ActualWidth > VIEWPORT * 0.25f && _box.ActualWidth < VIEWPORT * 0.75f,
                $"halfway between a quarter and three quarters, was {_box.ActualWidth}");
        }

        [TestMethod]
        public void APropertyWithNoDeclaredTransitionStillSnaps()
        {
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 90 };
            _box.Invalidate();
            Layout();

            Assert.AreEqual(90f, _box.ActualHeight, "only width was declared animatable");
        }

        [TestMethod]
        public void AnOffsetAnimates()
        {
            var canvas = new VisualElement { Name = "canvas" };
            canvas.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            var puck = new VisualElement { Name = "puck" };
            puck.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            puck.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            puck.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            puck.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.LEFT, new TransitionSpec { Duration = 64 } } }
            };

            canvas.AddChild(puck);

            var scheduler = new FakeScheduler();
            var surface = new IxenSurface(canvas)
            {
                Styles = new StyleRegistry(),
                Scheduler = scheduler
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0f, puck.X);

            puck.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            puck.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            scheduler.FireAll();
            scheduler.FireAll();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsTrue(puck.X > 0f && puck.X < 80f, $"it is on its way, was {puck.X}");

            scheduler.FireAll();
            scheduler.FireAll();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(80f, puck.X);
        }

        [TestMethod]
        public void ASizeIsAcceptedByTheParser()
        {
            var parser = new TransitionStyleParser("width 200ms ease-out height 100ms left 0.3s");

            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(200, parser.Descriptor.DurationOf(StyleIdentifier.WIDTH));
            Assert.AreEqual(EasingKind.EaseOut, parser.Descriptor.SpecOf(StyleIdentifier.WIDTH).Easing);
            Assert.AreEqual(100, parser.Descriptor.DurationOf(StyleIdentifier.HEIGHT));
            Assert.AreEqual(300, parser.Descriptor.DurationOf(StyleIdentifier.LEFT));
        }

        [TestMethod]
        public void AllCoversTheSizesToo()
        {
            var parser = new TransitionStyleParser("all 120ms");

            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(120, parser.Descriptor.DurationOf(StyleIdentifier.WIDTH));
            Assert.AreEqual(120, parser.Descriptor.DurationOf(StyleIdentifier.TOP));
        }
    }
}
