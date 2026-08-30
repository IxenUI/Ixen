using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class ReducedMotionTests
    {
        private const int VIEWPORT = 400;
        private const int DURATION = 64;

        private const string BLACK = "#000000";
        private const string WHITE = "#FFFFFF";
        private const string RED = "#FF0000";

        private FakeScheduler _scheduler;
        private StyleRegistry _registry;
        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();
            _registry = new StyleRegistry();

            _registry.Add(new KeyframesSet("fade", new List<Keyframe>
            {
                new Keyframe(0f, new List<StyleDescriptor> { new BackgroundStyleDescriptor { Color = BLACK } }),
                new Keyframe(1f, new List<StyleDescriptor> { new BackgroundStyleDescriptor { Color = WHITE } })
            }));

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = _registry, Scheduler = _scheduler };
        }

        private VisualElement Transitioned()
        {
            var box = new VisualElement { Name = "box" };

            box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.BACKGROUND, new TransitionSpec { Duration = DURATION } } }
            };

            _root.AddChild(box);

            return box;
        }

        private VisualElement Animated()
        {
            var box = new VisualElement { Name = "animated" };

            box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            box.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = "fade",
                Duration = DURATION,
                Iterations = AnimationStyleDescriptor.INFINITE
            };

            _root.AddChild(box);

            return box;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private static byte Red(VisualElement element)
            => element.Animations.For(StyleIdentifier.BACKGROUND).Current.SKColor.Red;

        private void Retarget(VisualElement box, string colour)
        {
            box.Styles.Background = new BackgroundStyleDescriptor { Color = colour };
            box.Invalidate();
            Layout();
        }

        [TestMethod]
        public void ATransitionJumpsStraightToItsTarget()
        {
            _surface.ReducedMotion = true;

            VisualElement box = Transitioned();
            Layout();

            Retarget(box, BLACK);

            Assert.AreEqual(0, Red(box),
                "the end state, immediately - a user who asked the system for less motion has "
                + "asked not to see the interpolation, not to see nothing");
            Assert.AreEqual(0, _surface.AnimatingCount, "and nothing is registered with the ticker");
            Assert.AreEqual(0, _scheduler.PendingCount, "so no timer is running at all");
        }

        [TestMethod]
        public void WithoutItTheSameTransitionInterpolates()
        {
            VisualElement box = Transitioned();
            Layout();

            Retarget(box, BLACK);

            Assert.AreEqual(1, _surface.AnimatingCount);

            _scheduler.FireAll();

            byte red = Red(box);

            Assert.IsTrue(red > 0 && red < 255,
                $"the counter-case: it really does interpolate when nothing asked it not to, was {red}");
        }

        [TestMethod]
        public void AKeyframeAnimationNeverStarts()
        {
            _surface.ReducedMotion = true;

            Animated();
            Layout();

            Assert.AreEqual(0, _surface.AnimatingCount);
            Assert.AreEqual(0, _scheduler.PendingCount,
                "an infinite keyframe animation would otherwise run for the life of the window");
        }

        [TestMethod]
        public void AForwardsAnimationStillLandsOnItsEndState()
        {
            _surface.ReducedMotion = true;

            var box = new VisualElement { Name = "filled" };

            box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            box.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = "fade",
                Duration = DURATION,
                Fill = AnimationFill.Forwards
            };

            _root.AddChild(box);
            Layout();

            Assert.AreEqual(255,
                box.Animations.For(StyleIdentifier.BACKGROUND).Current.SKColor.Green,
                "less motion means the end state at once, not the base style");
            Assert.AreEqual(0, _scheduler.PendingCount, "and nothing is ticking");
        }

        [TestMethod]
        public void TurningItOnFinishesWhatIsAlreadyRunning()
        {
            VisualElement box = Transitioned();
            Layout();

            Retarget(box, BLACK);
            _scheduler.FireAll();

            Assert.IsTrue(Red(box) > 0, "it is part way through");

            _surface.ReducedMotion = true;

            Assert.AreEqual(0, Red(box),
                "the setting can change while the window is open, and what is mid-flight has to "
                + "land rather than freeze half way");
            Assert.AreEqual(0, _surface.AnimatingCount);
            Assert.AreEqual(0, _scheduler.PendingCount, "and the shared ticker is stopped");
        }

        [TestMethod]
        public void SettingItToWhatItAlreadyIsAsksForNoRepaint()
        {
            _surface.ReducedMotion = true;

            Transitioned();
            Layout();

            using (var bitmap = new SkiaSharp.SKBitmap(VIEWPORT, VIEWPORT))
            using (var canvas = new SkiaSharp.SKCanvas(bitmap))
            {
                _surface.Render(canvas);
            }

            Assert.IsFalse(_surface.IsDirty, "the frame is on screen and nothing is pending");

            _surface.ReducedMotion = true;

            Assert.IsFalse(_surface.IsDirty,
                "a host re-reads this preference whenever the system says it changed, and it is "
                + "usually the same value - so an unchanged set must not ask for a repaint");
        }

        [TestMethod]
        public void ItIsOffUnlessTheHostSaysOtherwise()
        {
            Assert.IsFalse(new IxenSurface().ReducedMotion,
                "Ixen.Core cannot read an operating system preference, so the host reads it and "
                + "sets it - and the default has to be the one that changes nothing");
        }
    }
}
