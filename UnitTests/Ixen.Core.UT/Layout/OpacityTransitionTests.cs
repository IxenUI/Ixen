using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class OpacityTransitionTests
    {
        private const int DURATION = 64;

        private FakeScheduler _scheduler;
        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            var ground = new VisualElement { Name = "ground" };
            ground.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            ground.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            ground.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };
            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.OPACITY, new TransitionSpec { Duration = DURATION, Easing = EasingKind.Linear } } }
            };

            _root.AddChildren(ground, _box);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = _scheduler };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(100, 100);

        private void Fade(float opacity)
        {
            _box.Styles.Opacity = new OpacityStyleDescriptor { Value = opacity };
            _box.Invalidate();
            Layout();
        }

        private void Tick()
        {
            _scheduler.FireAll();
            Layout();
        }

        private byte Grey()
        {
            using (SKBitmap bitmap = _surface.RenderToBitmap())
            {
                return bitmap.GetPixel(50, 50).Red;
            }
        }

        [TestMethod]
        public void OpacityFadesRatherThanSnaps()
        {
            Assert.AreEqual(255, Grey(), "a white box, whole");

            Fade(0);

            Assert.AreEqual(255, Grey(), "nothing has moved before the first tick");

            Tick();
            Tick();

            byte half = Grey();

            Assert.IsTrue(half > 90 && half < 165, "half way through a linear fade the white is half gone: " + half);

            Tick();
            Tick();

            Assert.AreEqual(0, Grey(), "and gone at the end");
        }

        [TestMethod]
        public void ItFadesBackFromWhereverItWas()
        {
            Fade(0);
            Tick();
            Tick();

            Fade(1);

            byte start = Grey();

            Tick();

            Assert.IsTrue(Grey() > start, "turning round mid-fade starts from the current value, not from the end");

            Tick();
            Tick();
            Tick();

            Assert.AreEqual(255, Grey());
        }

        [TestMethod]
        public void ReducedMotionJumpsStraightToTheTarget()
        {
            _surface.ReducedMotion = true;

            Fade(0.5f);

            byte grey = Grey();

            Assert.IsTrue(grey > 110 && grey < 145, "already at half: " + grey);
        }

        [TestMethod]
        public void ItsEndIsReported()
        {
            var ended = new List<string>();
            _box.TransitionEnded += (sender, e) => ended.Add(e.Property);

            Fade(0.3f);

            for (int tick = 0; tick < 4; tick++)
            {
                Tick();
            }

            CollectionAssert.AreEqual(new[] { StyleIdentifier.OPACITY }, ended);
        }

        [TestMethod]
        public void TheStylesheetFormIsUnderstood()
        {
            var parser = new TransitionStyleParser("opacity 120ms ease-out");

            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(120, parser.Descriptor.SpecOf(StyleIdentifier.OPACITY).Duration);
        }
    }
}
