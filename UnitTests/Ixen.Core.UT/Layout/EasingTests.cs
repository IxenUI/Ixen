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
    public class EasingTests
    {
        private const int VIEWPORT = 200;
        private const string FROM = "#FF000000";
        private const string TO = "#FF0000FF";

        [TestMethod]
        public void EveryCurveStartsAtZeroAndEndsAtOne()
        {
            foreach (EasingKind kind in System.Enum.GetValues(typeof(EasingKind)))
            {
                Assert.AreEqual(0f, Easing.Apply(kind, 0f), $"{kind} at 0");
                Assert.AreEqual(1f, Easing.Apply(kind, 1f), $"{kind} at 1");
                Assert.AreEqual(0f, Easing.Apply(kind, -0.5f), $"{kind} is clamped below");
                Assert.AreEqual(1f, Easing.Apply(kind, 1.5f), $"{kind} is clamped above");
            }
        }

        [TestMethod]
        public void EachCurveBendsTheWayItsNameSays()
        {
            Assert.AreEqual(0.5f, Easing.Apply(EasingKind.Linear, 0.5f), 0.0001f);
            Assert.IsTrue(Easing.Apply(EasingKind.EaseIn, 0.5f) < 0.5f, "ease-in starts slow");
            Assert.IsTrue(Easing.Apply(EasingKind.EaseOut, 0.5f) > 0.5f, "ease-out starts fast");
            Assert.AreEqual(0.5f, Easing.Apply(EasingKind.EaseInOut, 0.5f), 0.0001f,
                "ease-in-out is symmetric about its midpoint");
        }

        [TestMethod]
        public void ACurveIsMonotonic()
        {
            foreach (EasingKind kind in System.Enum.GetValues(typeof(EasingKind)))
            {
                float previous = -1f;

                for (int i = 0; i <= 20; i++)
                {
                    float value = Easing.Apply(kind, i / 20f);

                    Assert.IsTrue(value >= previous, $"{kind} went backwards at {i}");
                    previous = value;
                }
            }
        }

        private static TransitionStyleDescriptor Parse(string value)
        {
            var parser = new TransitionStyleParser(value);

            Assert.IsTrue(parser.IsValid, value);

            return parser.Descriptor;
        }

        [TestMethod]
        public void ACurveIsOptionalAndDefaultsToLinear()
        {
            TransitionStyleDescriptor descriptor = Parse("background 160ms");

            Assert.AreEqual(160, descriptor.DurationOf(StyleIdentifier.BACKGROUND));
            Assert.AreEqual(EasingKind.Linear, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Easing);
        }

        [TestMethod]
        public void ACurveIsReadAfterItsDuration()
        {
            TransitionStyleDescriptor descriptor = Parse("background 160ms ease-out");

            Assert.AreEqual(160, descriptor.DurationOf(StyleIdentifier.BACKGROUND));
            Assert.AreEqual(EasingKind.EaseOut, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Easing);
        }

        [TestMethod]
        public void EachPropertyKeepsItsOwnCurve()
        {
            TransitionStyleDescriptor descriptor = Parse("background 160ms ease-out border 200ms ease-in");

            Assert.AreEqual(EasingKind.EaseOut, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Easing);
            Assert.AreEqual(EasingKind.EaseIn, descriptor.SpecOf(StyleIdentifier.BORDER).Easing);
            Assert.AreEqual(200, descriptor.DurationOf(StyleIdentifier.BORDER));
        }

        [TestMethod]
        public void APropertyWithNoCurveFollowsTheOneWithout()
        {
            TransitionStyleDescriptor descriptor = Parse("background 160ms color 100ms ease-in");

            Assert.AreEqual(EasingKind.Linear, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Easing,
                "the curve after a duration belongs to the pair it follows, not to the next one");
            Assert.AreEqual(EasingKind.EaseIn, descriptor.SpecOf(StyleIdentifier.COLOR).Easing);
        }

        [TestMethod]
        public void ADelayIsASecondDuration()
        {
            TransitionStyleDescriptor descriptor = Parse("background 160ms 40ms ease-out");

            Assert.AreEqual(160, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Duration);
            Assert.AreEqual(40, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Delay);
            Assert.AreEqual(EasingKind.EaseOut, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Easing);
        }

        [TestMethod]
        public void ACurveAndADelayComeInAnyOrder()
        {
            TransitionStyleDescriptor descriptor = Parse("background 160ms ease-in 40ms");

            Assert.AreEqual(40, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Delay);
            Assert.AreEqual(EasingKind.EaseIn, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Easing);
        }

        [TestMethod]
        public void ADelayDoesNotSwallowTheNextProperty()
        {
            TransitionStyleDescriptor descriptor = Parse("background 160ms 40ms color 100ms");

            Assert.AreEqual(40, descriptor.SpecOf(StyleIdentifier.BACKGROUND).Delay);
            Assert.AreEqual(100, descriptor.SpecOf(StyleIdentifier.COLOR).Duration);
            Assert.AreEqual(0, descriptor.SpecOf(StyleIdentifier.COLOR).Delay,
                "a property name parses as neither a curve nor a duration, so the group ends");
        }

        [TestMethod]
        public void ADelayHoldsTheValueBeforeMoving()
        {
            var scheduler = new FakeScheduler();
            var root = new VisualElement { Name = "root" };
            var box = new VisualElement { Name = "box" };

            box.Styles.Background = new BackgroundStyleDescriptor { Color = FROM };
            box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs =
                {
                    {
                        StyleIdentifier.BACKGROUND,
                        new TransitionSpec { Duration = 32, Delay = 32 }
                    }
                }
            };

            root.AddChild(box);

            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                Scheduler = scheduler
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            box.Styles.Background = new BackgroundStyleDescriptor { Color = TO };
            box.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            scheduler.FireAll();
            scheduler.FireAll();

            Assert.AreEqual(new Color(FROM), box.Animations.For(StyleIdentifier.BACKGROUND).Current,
                "two ticks of delay have passed, and nothing has moved yet");

            scheduler.FireAll();
            scheduler.FireAll();

            Assert.AreEqual(new Color(TO), box.Animations.For(StyleIdentifier.BACKGROUND).Current,
                "then two ticks cover the 32ms");
        }

        [TestMethod]
        public void TheEndOfATransitionRaisesAnEvent()
        {
            var scheduler = new FakeScheduler();
            var root = new VisualElement { Name = "root" };
            var box = new VisualElement { Name = "box" };

            box.Styles.Background = new BackgroundStyleDescriptor { Color = FROM };
            box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.BACKGROUND, new TransitionSpec { Duration = 32 } } }
            };

            root.AddChild(box);

            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                Scheduler = scheduler
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            var ended = new System.Collections.Generic.List<string>();
            box.TransitionEnded += (sender, e) => ended.Add(e.Property);

            box.Styles.Background = new BackgroundStyleDescriptor { Color = TO };
            box.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            scheduler.FireAll();

            Assert.AreEqual(0, ended.Count, "one tick of two is not the end");

            scheduler.FireAll();

            CollectionAssert.AreEqual(new[] { StyleIdentifier.BACKGROUND }, ended);

            scheduler.FireAll();

            Assert.AreEqual(1, ended.Count, "and it fires once, not on every later tick");
        }

        [TestMethod]
        public void AnUnknownWordIsNotACurve()
        {
            Assert.IsFalse(new TransitionStyleParser("background 160ms bouncy").IsValid,
                "it is read as a property name, and there is no such property");
        }

        [TestMethod]
        public void ACurveChangesTheInterpolatedColour()
        {
            byte linear = Midpoint(EasingKind.Linear).SKColor.Blue;
            byte easeOut = Midpoint(EasingKind.EaseOut).SKColor.Blue;
            byte easeIn = Midpoint(EasingKind.EaseIn).SKColor.Blue;

            Assert.IsTrue(easeOut > linear, "ease-out is further along at the same tick than linear");
            Assert.IsTrue(easeIn < linear, "and ease-in is behind it");
        }

        private static Color Midpoint(EasingKind easing)
        {
            var scheduler = new FakeScheduler();
            var root = new VisualElement { Name = "root" };
            var box = new VisualElement { Name = "box" };

            box.Styles.Background = new BackgroundStyleDescriptor { Color = FROM };
            box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.BACKGROUND, new TransitionSpec { Duration = 64, Easing = easing } } }
            };

            root.AddChild(box);

            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                Scheduler = scheduler
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            box.Styles.Background = new BackgroundStyleDescriptor { Color = TO };
            box.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            scheduler.FireAll();
            scheduler.FireAll();

            return box.Animations.For(StyleIdentifier.BACKGROUND).Current;
        }
    }
}
