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
    public class SharedTickerTests
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

            _surface = new IxenSurface(_root)
            {
                Styles = _registry,
                Scheduler = _scheduler
            };
        }

        private VisualElement Animated(int iterations)
        {
            var box = new VisualElement();

            box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            box.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = "fade",
                Duration = DURATION,
                Iterations = iterations
            };

            _root.AddChild(box);

            return box;
        }

        private VisualElement Transitioned()
        {
            var box = new VisualElement();

            box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.BACKGROUND, new TransitionSpec { Duration = DURATION } } }
            };

            _root.AddChild(box);

            return box;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Tick(int times = 1)
        {
            for (int index = 0; index < times; index++)
            {
                _scheduler.FireAll();
            }
        }

        [TestMethod]
        public void ManyAnimatingElementsShareOneTimer()
        {
            Animated(AnimationStyleDescriptor.INFINITE);
            Animated(AnimationStyleDescriptor.INFINITE);
            Animated(AnimationStyleDescriptor.INFINITE);

            Layout();

            Assert.AreEqual(3, _surface.AnimatingCount, "all three are animating");
            Assert.AreEqual(1, _scheduler.PendingCount,
                "but the surface owns a single ticker, not one per element");
        }

        [TestMethod]
        public void OneTickAdvancesEveryRegisteredElement()
        {
            VisualElement first = Animated(AnimationStyleDescriptor.INFINITE);
            VisualElement second = Animated(AnimationStyleDescriptor.INFINITE);

            Layout();
            Tick(2);

            byte one = first.Animations.For(StyleIdentifier.BACKGROUND).Current.SKColor.Red;
            byte two = second.Animations.For(StyleIdentifier.BACKGROUND).Current.SKColor.Red;

            Assert.IsTrue(one > 0 && one < 255, $"the first advanced, was {one}");
            Assert.AreEqual(one, two, "and the second advanced by exactly as much on the same tick");
        }

        [TestMethod]
        public void TransitionsShareTheSameTicker()
        {
            VisualElement first = Transitioned();
            VisualElement second = Transitioned();

            Layout();

            first.Styles.Background = new BackgroundStyleDescriptor { Color = BLACK };
            second.Styles.Background = new BackgroundStyleDescriptor { Color = BLACK };
            first.Invalidate();
            second.Invalidate();

            Layout();

            Assert.AreEqual(2, _surface.AnimatingCount);
            Assert.AreEqual(1, _scheduler.PendingCount, "the ticker is not per-feature either");
        }

        [TestMethod]
        public void TheTickerSurvivesOneElementFinishing()
        {
            Animated(1);
            Animated(AnimationStyleDescriptor.INFINITE);

            Layout();
            Tick(4);

            Assert.AreEqual(1, _surface.AnimatingCount, "the finite one dropped out");
            Assert.AreEqual(1, _scheduler.PendingCount, "and the infinite one keeps the ticker alive");
        }

        [TestMethod]
        public void TheTickerStopsWhenTheLastAnimationEnds()
        {
            Animated(1);
            Animated(1);

            Layout();

            Assert.AreEqual(1, _scheduler.PendingCount);

            Tick(4);

            Assert.AreEqual(0, _surface.AnimatingCount);
            Assert.AreEqual(0, _scheduler.PendingCount, "nothing left to tick, so no timer is left running");
        }

        [TestMethod]
        public void DetachingAnElementUnregistersItWithoutWaitingForATick()
        {
            VisualElement box = Animated(AnimationStyleDescriptor.INFINITE);

            Layout();

            Assert.AreEqual(1, _surface.AnimatingCount);

            _root.RemoveChild(box);

            Assert.AreEqual(0, _surface.AnimatingCount,
                "the element remembers which host it registered with, so detaching releases it at once");
            Assert.AreEqual(0, _scheduler.PendingCount);
        }

        [TestMethod]
        public void AHandlerRemovingItsElementDuringATickDoesNotBreakTheBatch()
        {
            VisualElement first = Animated(1);
            VisualElement second = Animated(AnimationStyleDescriptor.INFINITE);

            first.TransitionEnded += (sender, e) => _root.RemoveChild(first);

            Layout();
            Tick(4);

            Assert.AreEqual(1, _surface.AnimatingCount,
                "the tick iterates a snapshot, so mutating the list from a handler is safe");
            Assert.AreEqual(1, _scheduler.PendingCount);

            Tick(2);

            byte survivor = second.Animations.For(StyleIdentifier.BACKGROUND).Current.SKColor.Red;

            Assert.IsTrue(survivor > 0, "and the surviving element keeps animating");
        }

        [TestMethod]
        public void NothingAnimatingMeansNoTimerAtAll()
        {
            var box = new VisualElement();
            box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            _root.AddChild(box);

            Layout();

            Assert.AreEqual(0, _surface.AnimatingCount);
            Assert.AreEqual(0, _scheduler.PendingCount,
                "an application that animates nothing pays for no timer");
        }

        [TestMethod]
        public void WithNoSchedulerNothingIsRegistered()
        {
            var surface = new IxenSurface(_root) { Styles = _registry };

            Animated(AnimationStyleDescriptor.INFINITE);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0, surface.AnimatingCount,
                "a host with no timer cannot animate, so nothing is left registered");
        }
    }
}
