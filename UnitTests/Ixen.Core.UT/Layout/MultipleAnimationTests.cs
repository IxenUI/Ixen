using Ixen.Core.Input;
using Ixen.Core.UT.Input;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class MultipleAnimationTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _box;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;

        private void Build(string content)
        {
            var source = new XnsSource(content);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            _box = new VisualElement { Name = "box" };

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.AddChild(_box);

            _scheduler = new FakeScheduler();
            _surface = new IxenSurface(root)
            {
                Styles = registry,
                Scheduler = _scheduler
            };

            root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Tick(int times)
        {
            for (int index = 0; index < times; index++)
            {
                _scheduler.FireAll();
            }

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private const string TWO_SETS = "@keyframes grow {\r\n"
            + "    0%   { width: 20px }\r\n"
            + "    100% { width: 120px }\r\n"
            + "}\r\n"
            + "@keyframes redden {\r\n"
            + "    0%   { background: #000000 }\r\n"
            + "    100% { background: #FF0000 }\r\n"
            + "}\r\n";

        private static string Box(string animation)
            => "box {\r\n"
            + "    width: 20px\r\n"
            + "    height: 20px\r\n"
            + "    background: #000000\r\n"
            + "    animation: " + animation + "\r\n"
            + "}";

        private string Colour()
            => _box.StylesHandlers.Background.Descriptor.Color;

        [TestMethod]
        public void ACommaDeclaresTwoAnimationsThatBothRun()
        {
            Build(TWO_SETS + Box("grow 320ms, redden 320ms"));

            Tick(10);

            Assert.IsTrue(_box.ActualWidth > 30 && _box.ActualWidth < 120,
                $"the size animation is mid-flight; got {_box.ActualWidth}");

            SKColor red = _box.AnimatedBrush(StyleIdentifier.BACKGROUND).SKPaint.Color;

            Assert.IsTrue(red.Red > 20 && red.Red < 240,
                $"and so is the colour one, at the same time; got {red}");
        }

        [TestMethod]
        public void EachOneKeepsItsOwnClock()
        {
            Build(TWO_SETS + Box("grow 320ms, redden 160ms"));

            Tick(9);

            SKColor colour = _box.AnimatedBrush(StyleIdentifier.BACKGROUND).SKPaint.Color;

            Assert.IsTrue(colour.Red > 200,
                $"the shorter one is nearly done on its own clock; got {colour}");

            Assert.IsTrue(_box.ActualWidth < 80,
                $"while the longer one is only about half way; got {_box.ActualWidth}");
        }

        [TestMethod]
        public void EachOneKeepsItsOwnDelay()
        {
            Build(TWO_SETS + Box("grow 160ms, redden 160ms 320ms"));

            Tick(4);

            SKColor colour = _box.AnimatedBrush(StyleIdentifier.BACKGROUND).SKPaint.Color;

            Assert.AreEqual(0, colour.Red,
                "the delayed one has not started");

            Assert.IsTrue(_box.ActualWidth > 30,
                $"while the other one already moved; got {_box.ActualWidth}");
        }

        [TestMethod]
        public void TheLastOneDeclaredWinsAProperty()
        {
            Build("@keyframes to_blue {\r\n"
                + "    0%   { background: #000000 }\r\n"
                + "    100% { background: #0000FF }\r\n"
                + "}\r\n"
                + "@keyframes to_green {\r\n"
                + "    0%   { background: #000000 }\r\n"
                + "    100% { background: #00FF00 }\r\n"
                + "}\r\n"
                + Box("to_blue 160ms, to_green 160ms"));

            Tick(9);

            SKColor colour = _box.AnimatedBrush(StyleIdentifier.BACKGROUND).SKPaint.Color;

            Assert.IsTrue(colour.Green > 200,
                "two animations driving one property are applied in order, so the last declared "
                + $"is the one that lands - CSS's rule; got {colour}");

            Assert.IsTrue(colour.Blue < 40, $"and the first one does not show; got {colour}");
        }

        [TestMethod]
        public void OneInfiniteAnimationKeepsTheOtherTicking()
        {
            Build(TWO_SETS + Box("grow 160ms infinite, redden 160ms"));

            Tick(30);

            Assert.IsTrue(_box.Animations.HasKeyframes,
                "the infinite one is still running long after the finite one ended");
        }

        [TestMethod]
        public void OnlyTheForwardsOneHoldsItsLastFrame()
        {
            Build(TWO_SETS + Box("grow 160ms forwards, redden 160ms"));

            Tick(20);

            Assert.AreEqual(120f, _box.ActualWidth, 0.01f,
                "the forwards one keeps where the timeline left it");

            Assert.AreEqual("#000000", Colour(),
                "while the other one reverted to the cascade on the next style pass");
        }

        [TestMethod]
        public void OneDeclarationIsStillOneAnimation()
        {
            Build(TWO_SETS + Box("grow 320ms"));

            Tick(10);

            Assert.IsTrue(_box.ActualWidth > 30 && _box.ActualWidth < 120);
        }

        private static void AssertRejected(string value)
        {
            var source = new XnsSource($"box {{ animation: {value} }}");

            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'{value}' should have been rejected");
        }

        [TestMethod]
        public void OneBadEntryRejectsTheWholeDeclaration()
        {
            AssertRejected("grow 320ms, wobble");
            AssertRejected("grow 320ms, ");
            AssertRejected(", grow 320ms");
        }

        [TestMethod]
        public void EveryAnimationSurvivesGeneration()
        {
            var source = new XnsSource(
                "box { animation: grow 320ms ease-out 40ms 3x alternate forwards, redden 160ms }");

            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            string generated = set.Classes.Single().Styles.Single().ToSource();

            StringAssert.Contains(generated, "Name = \"grow\"");
            StringAssert.Contains(generated, "Name = \"redden\"");
            StringAssert.Contains(generated, "Duration = 320");
            StringAssert.Contains(generated, "Duration = 160");
            StringAssert.Contains(generated, "Delay = 40");
            StringAssert.Contains(generated, "Iterations = 3");
            StringAssert.Contains(generated, "Alternate = true");
            StringAssert.Contains(generated, "AnimationFill.Forwards");
        }
    }
}
