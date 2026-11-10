using Ixen.Core.Language.Xns;
using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class OpacityKeyframesTests
    {
        private const string FADE = "@keyframes fade {\r\n    from { opacity: 1 }\r\n    to { opacity: 0 }\r\n}\r\n";

        private FakeScheduler _scheduler;
        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        private void Build(string sheet)
        {
            var source = new XnsSource(FADE + sheet);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

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

            _root.AddChildren(ground, _box);

            _surface = new IxenSurface(_root) { Styles = registry, Scheduler = _scheduler };
        }

        private void Layout() => _surface.ComputeLayout(100, 100);

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
        public void AKeyframeFadesTheElement()
        {
            Build("box {\r\n    animation: fade 64ms linear\r\n}\r\n");
            Layout();

            Assert.AreEqual(255, Grey(), "the first stop is whole");

            Tick();
            Tick();

            byte half = Grey();

            Assert.IsTrue(half > 90 && half < 165, "half way through, half gone: " + half);

            Tick();

            Assert.IsTrue(Grey() < half, "and still going down");
        }

        [TestMethod]
        public void WithoutAFillTheDeclaredOpacityComesBack()
        {
            Build("box {\r\n    animation: fade 64ms linear\r\n}\r\n");
            Layout();

            for (int tick = 0; tick < 6; tick++)
            {
                Tick();
            }

            Assert.AreEqual(255, Grey(), "an ended animation lets go of the element");
        }

        [TestMethod]
        public void AForwardsFillKeepsTheLastStop()
        {
            Build("box {\r\n    animation: fade 64ms linear forwards\r\n}\r\n");
            Layout();

            for (int tick = 0; tick < 6; tick++)
            {
                Tick();
            }

            Assert.AreEqual(0, Grey(), "faded out and left there");
        }

        [TestMethod]
        public void AnInfinitePulseComesRound()
        {
            Build("box {\r\n    animation: fade 64ms linear infinite alternate\r\n}\r\n");
            Layout();

            var greys = new List<byte>();

            for (int tick = 0; tick < 8; tick++)
            {
                Tick();
                greys.Add(Grey());
            }

            Assert.IsTrue(greys.Min() < 40, "it went down: " + string.Join(",", greys));
            Assert.IsTrue(greys.Last() > 215, "and came back up: " + string.Join(",", greys));
        }

        [TestMethod]
        public void ADeclaredTransitionDoesNotFightTheKeyframes()
        {
            Build("box {\r\n    opacity: 0.5\r\n    transition: opacity 64ms linear\r\n    animation: fade 64ms linear forwards\r\n}\r\n");
            Layout();

            for (int tick = 0; tick < 12; tick++)
            {
                Tick();
            }

            Assert.AreEqual(0, Grey(), "the keyframes own the property while they run or hold");
        }

        [TestMethod]
        public void TakingTheAnimationAwayMidwayGivesTheDeclaredOpacityBack()
        {
            Build(".breathing {\r\n    animation: fade 640ms linear infinite alternate\r\n}\r\n");
            _box.Classes.Add("breathing");
            _box.Invalidate();
            Layout();

            for (int tick = 0; tick < 20; tick++)
            {
                Tick();
            }

            Assert.IsTrue(Grey() < 200, "it is somewhere in its breath");

            _box.Classes.Remove("breathing");
            _box.Invalidate();
            Layout();

            Assert.AreEqual(255, Grey(), "the picture arrived: whole again at once");
        }

        [TestMethod]
        public void ReducedMotionNeverStartsIt()
        {
            Build("box {\r\n    animation: fade 64ms linear infinite\r\n}\r\n");
            _surface.ReducedMotion = true;
            Layout();
            Tick();
            Tick();

            Assert.AreEqual(255, Grey(), "the declared opacity, untouched");
        }
    }
}
