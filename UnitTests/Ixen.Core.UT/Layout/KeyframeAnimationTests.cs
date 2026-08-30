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
    public class KeyframeAnimationTests
    {
        private const int VIEWPORT = 400;
        private const int DURATION = 64;

        private const string BLACK = "#000000";
        private const string WHITE = "#FFFFFF";
        private const string RED = "#FF0000";

        private FakeScheduler _scheduler;
        private StyleRegistry _registry;
        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();
            _registry = new StyleRegistry();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root)
            {
                Styles = _registry,
                Scheduler = _scheduler
            };
        }

        private static Keyframe Frame(float offset, string color)
            => new Keyframe(offset, new List<StyleDescriptor>
            {
                new BackgroundStyleDescriptor { Color = color }
            });

        private static Keyframe WidthFrame(float offset, float width)
            => new Keyframe(offset, new List<StyleDescriptor>
            {
                new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width }
            });

        private void Keyframes(string name, params Keyframe[] frames)
            => _registry.Add(new KeyframesSet(name, new List<Keyframe>(frames)));

        private void Animate(string name, int iterations = 1, bool alternate = false,
            int delay = 0, EasingKind easing = EasingKind.Linear,
            AnimationFill fill = AnimationFill.None)
        {
            _box.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = name,
                Duration = DURATION,
                Delay = delay,
                Easing = easing,
                Iterations = iterations,
                Alternate = alternate,
                Fill = fill
            };

            _box.Invalidate();
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private Color Current => _box.Animations.For(StyleIdentifier.BACKGROUND).Current;

        private void Tick(int times = 1)
        {
            for (int index = 0; index < times; index++)
            {
                _scheduler.FireAll();
            }
        }

        [TestMethod]
        public void TheFirstFrameIsAppliedBeforeAnyTick()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade");

            Assert.AreEqual(0, Current.SKColor.Red, "it starts on its own first stop, not on the cascade");
            Assert.AreEqual(1, _scheduler.PendingCount, "and a ticker is running");
        }

        [TestMethod]
        public void AColourWalksBetweenTwoStops()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade");

            Tick(2);

            byte middle = Current.SKColor.Red;

            Assert.IsTrue(middle > 0 && middle < 255, $"halfway is neither end, was {middle}");
        }

        [TestMethod]
        public void AMiddleStopIsPassedThrough()
        {
            Keyframes("pulse", Frame(0f, BLACK), Frame(0.5f, WHITE), Frame(1f, BLACK));
            Animate("pulse");

            Tick(2);

            Assert.AreEqual(255, Current.SKColor.Red, "at half the duration it sits exactly on the middle stop");

            Tick(1);

            Assert.IsTrue(Current.SKColor.Red < 255, "and then comes back down");
        }

        [TestMethod]
        public void AnInfiniteAnimationStartsOver()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade", AnimationStyleDescriptor.INFINITE);

            Tick(4);

            Assert.AreEqual(0, Current.SKColor.Red, "the fourth tick wraps back to the first stop");
            Assert.AreEqual(1, _scheduler.PendingCount, "and it keeps ticking");

            Tick(2);

            Assert.IsTrue(Current.SKColor.Red > 0, "the second pass is under way");
        }

        [TestMethod]
        public void AFiniteAnimationRevertsToTheCascade()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade");

            Tick(4);
            Layout();

            Assert.AreEqual(255, Current.SKColor.Red, "red is what the stylesheet says");
            Assert.AreEqual(0, Current.SKColor.Green);
            Assert.AreEqual(0, _scheduler.PendingCount, "and the ticker is gone");
        }

        [TestMethod]
        public void AlternateComesBackToWhereItStarted()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade", 2, alternate: true);

            Tick(4);

            Assert.AreEqual(255, Current.SKColor.Red, "the first pass ends on the last stop");

            Tick(4);

            Assert.AreEqual(0, Current.SKColor.Red, "and the second pass runs it backwards");
        }

        [TestMethod]
        public void ADelayHoldsTheFirstFrame()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade", delay: 32);

            Tick(2);

            Assert.AreEqual(0, Current.SKColor.Red, "two ticks of delay have not started it");

            Tick(1);

            Assert.IsTrue(Current.SKColor.Red > 0, "the third tick is the first real step");
        }

        [TestMethod]
        public void ASizeAnimationMovesTheLayout()
        {
            Keyframes("grow", WidthFrame(0f, 20), WidthFrame(1f, 60));
            Animate("grow");

            Assert.AreEqual(20f, _box.ActualWidth, "the first stop wins over the cascade");

            Tick(2);
            Layout();

            Assert.AreEqual(40f, _box.ActualWidth, "halfway between the two stops");
        }

        [TestMethod]
        public void ASizeAnimationReleasesTheWidthWhenItEnds()
        {
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            Keyframes("grow", WidthFrame(0f, 20), WidthFrame(1f, 60));
            Animate("grow");

            Assert.AreEqual(20f, _box.ActualWidth);

            Tick(4);
            Layout();

            Assert.AreEqual(100f, _box.ActualWidth, "once it ends the declared width is read again");
        }

        [TestMethod]
        public void AnAnimationBeatsATransitionOnTheSameProperty()
        {
            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.BACKGROUND, new TransitionSpec { Duration = DURATION } } }
            };

            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade", AnimationStyleDescriptor.INFINITE);

            Assert.AreEqual(0, Current.SKColor.Red,
                "the animation owns the property, so the cascade never retargets it");

            Tick(2);

            Assert.IsTrue(Current.SKColor.Red > 0 && Current.SKColor.Red < 255);
        }

        [TestMethod]
        public void AnUnknownNameAnimatesNothing()
        {
            Animate("nowhere");

            Assert.AreEqual(0, _scheduler.PendingCount, "there is nothing to tick");
            Assert.AreEqual(255, Current.SKColor.Red, "and the cascade still applies");
        }

        [TestMethod]
        public void ASingleStopIsNotAnAnimation()
        {
            Keyframes("fade", Frame(0f, BLACK));
            Animate("fade");

            Assert.AreEqual(0, _scheduler.PendingCount, "one stop has nothing to interpolate towards");
            Assert.AreEqual(255, Current.SKColor.Red);
        }

        [TestMethod]
        public void WithNoSchedulerNothingBreaks()
        {
            var registry = new StyleRegistry();
            registry.Add(new KeyframesSet("fade", new List<Keyframe> { Frame(0f, BLACK), Frame(1f, WHITE) }));

            var root = new VisualElement { Name = "root" };
            var box = new VisualElement { Name = "box" };

            box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            box.Styles.Animation = new AnimationStyleDescriptor { Name = "fade", Duration = DURATION };

            root.AddChild(box);

            var surface = new IxenSurface(root) { Styles = registry };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(box.Animations.HasKeyframes, "a host with no timer cannot run it");
        }

        [TestMethod]
        public void ForwardsKeepsTheLastFrame()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade", fill: AnimationFill.Forwards);

            Tick(4);
            Layout();

            Assert.AreEqual(255, Current.SKColor.Green,
                "the last frame is white and it stays white");
            Assert.AreEqual(0, _scheduler.PendingCount,
                "holding a frame is not the same as still running");
        }

        [TestMethod]
        public void ForwardsHoldsASizeToo()
        {
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            Keyframes("grow", WidthFrame(0f, 20), WidthFrame(1f, 60));
            Animate("grow", fill: AnimationFill.Forwards);

            Tick(4);
            Layout();

            Assert.AreEqual(60f, _box.ActualWidth,
                "the declared width does not come back");
        }

        [TestMethod]
        public void RemovingTheAnimationDropsTheHeldFrame()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade", fill: AnimationFill.Forwards);

            Tick(4);
            Layout();

            _box.Styles.Animation = new AnimationStyleDescriptor();
            _box.Invalidate();
            Layout();

            Assert.AreEqual(0, Current.SKColor.Green,
                "the hold belongs to the declaration, so undeclaring it reverts");
        }

        [TestMethod]
        public void ANewAnimationRestartsRatherThanHolding()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Keyframes("other", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade", fill: AnimationFill.Forwards);

            Tick(4);
            Layout();

            Animate("other", fill: AnimationFill.Forwards);

            Assert.AreEqual(0, Current.SKColor.Green, "it is back on its own first stop");
            Assert.AreEqual(1, _scheduler.PendingCount, "and ticking again");
        }

        [TestMethod]
        public void ForwardsOnAnAlternateEndsWhereItStarted()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade", 2, alternate: true, fill: AnimationFill.Forwards);

            Tick(8);
            Layout();

            Assert.AreEqual(0, Current.SKColor.Green,
                "two alternating passes end on the first stop, and that is what is held");
        }

        [TestMethod]
        public void WithNoSchedulerForwardsLandsOnTheLastFrame()
        {
            Assert.AreEqual(255, Unscheduled(AnimationFill.Forwards, 1, false).SKColor.Green,
                "a host with no timer shows the end state rather than nothing");
        }

        [TestMethod]
        public void WithNoSchedulerAndNoFillItStaysOnTheCascade()
        {
            Assert.AreEqual(0, Unscheduled(AnimationFill.None, 1, false).SKColor.Green);
        }

        [TestMethod]
        public void WithNoSchedulerAnInfiniteAnimationHoldsNothing()
        {
            Assert.AreEqual(0, Unscheduled(AnimationFill.Forwards,
                AnimationStyleDescriptor.INFINITE, false).SKColor.Green,
                "an animation that never ends has no end state to fill with");
        }

        [TestMethod]
        public void WithNoSchedulerAnAlternateLandsWhereItWouldHave()
        {
            Assert.AreEqual(0, Unscheduled(AnimationFill.Forwards, 2, true).SKColor.Green,
                "the parity of the iteration count decides which stop is the end");
        }

        private static Color Unscheduled(AnimationFill fill, int iterations, bool alternate)
        {
            var registry = new StyleRegistry();

            registry.Add(new KeyframesSet("fade", new List<Keyframe>
            {
                Frame(0f, BLACK), Frame(1f, WHITE)
            }));

            var root = new VisualElement { Name = "root" };
            var box = new VisualElement { Name = "box" };

            box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            box.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = "fade",
                Duration = DURATION,
                Iterations = iterations,
                Alternate = alternate,
                Fill = fill
            };

            root.AddChild(box);

            var surface = new IxenSurface(root) { Styles = registry };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return box.Animations.For(StyleIdentifier.BACKGROUND).Current;
        }

        [TestMethod]
        public void RemovingTheAnimationStopsIt()
        {
            Keyframes("fade", Frame(0f, BLACK), Frame(1f, WHITE));
            Animate("fade", AnimationStyleDescriptor.INFINITE);

            Tick(2);

            Assert.IsTrue(_box.Animations.HasKeyframes);

            _box.Styles.Animation = new AnimationStyleDescriptor();
            _box.Invalidate();
            Layout();

            Assert.IsFalse(_box.Animations.HasKeyframes, "an undeclared animation is stopped");
            Assert.AreEqual(255, Current.SKColor.Red, "and the property goes back to the cascade");
        }
    }
}
