using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class MarqueeTests
    {
        private const string LONG = "A song title far too long for the little box it has to live in";

        private VisualElement _root;
        private VisualElement _title;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            _title = new VisualElement { Name = "title", Text = LONG };
            _title.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _title.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _title.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _title.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };
            _title.Styles.TextOverflow = new TextOverflowStyleDescriptor { Value = TextOverflow.Marquee };
            _title.Styles.Color = new ColorStyleDescriptor { Value = "#000000" };

            _root.AddChild(_title);

            _scheduler = new FakeScheduler();
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = _scheduler };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(300, 100);

        private void Ticks(int count)
        {
            for (int tick = 0; tick < count; tick++)
            {
                _scheduler.FireAll();
                Layout();
            }
        }

        private bool Ticking => _scheduler.Scheduled.Any(entry => entry.Repeat && !entry.Cancelled);

        [TestMethod]
        public void ALongTitleWaitsThenScrollsAtAnEvenPace()
        {
            Assert.AreEqual(1, _title.TextLines.Count, "a marquee is one line");
            Assert.IsTrue(Ticking, "it overflows, so the ticker runs");

            Ticks(Marquee.PAUSE_TICKS);

            Assert.AreEqual(0, _title.MarqueeOffset, "it rests at its start first");

            Ticks(10);

            Assert.AreEqual(10 * Marquee.STEP, _title.MarqueeOffset, 0.001f);
        }

        [TestMethod]
        public void AfterAWholeLapItRestsAgain()
        {
            float lap = _title.TextLayout.Width + Marquee.GAP;
            int ticks = Marquee.PAUSE_TICKS + (int)System.Math.Ceiling(lap / Marquee.STEP);

            Ticks(ticks - 1);

            Assert.IsTrue(_title.MarqueeOffset > lap - 2 * Marquee.STEP, "almost a lap: " + _title.MarqueeOffset);

            Ticks(1);

            Assert.AreEqual(0, _title.MarqueeOffset, "back to the start, which the second copy reached seamlessly");
            Assert.AreEqual(0, _title.MarqueeTicks, "and the pause starts again");
        }

        [TestMethod]
        public void ATitleThatFitsCostsNoFrames()
        {
            _title.Text = "Short";
            Layout();
            _scheduler.FireAll();
            Layout();

            Assert.IsFalse(Ticking, "nothing to scroll, so nothing ticks");
            Assert.AreEqual(1, _surface.MarqueeCount, "though it is still watched, in case the text grows");
        }

        [TestMethod]
        public void AHiddenTitleDoesNotMove()
        {
            _title.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            _title.Invalidate();
            Layout();

            Assert.IsFalse(Ticking);
        }

        [TestMethod]
        public void ReducedMotionShowsAnEllipsisInstead()
        {
            Ticks(Marquee.PAUSE_TICKS + 20);

            _surface.ReducedMotion = true;
            Layout();

            Assert.AreEqual(0, _title.MarqueeOffset);
            Assert.IsFalse(Ticking);
            Assert.IsTrue(_title.TextLines[0].EndsWith("…"), "the title is cut with an ellipsis: " + _title.TextLines[0]);

            _surface.ReducedMotion = false;
            Layout();

            Assert.AreEqual(LONG, _title.TextLines[0], "and whole again when motion is allowed");
            Assert.IsTrue(Ticking);
        }

        [TestMethod]
        public void LeavingMarqueeOrTheTreeStopsWatching()
        {
            _title.Styles.TextOverflow = new TextOverflowStyleDescriptor { Value = TextOverflow.Clip };
            _title.Invalidate();
            Layout();

            Assert.AreEqual(0, _surface.MarqueeCount);

            _title.Styles.TextOverflow = new TextOverflowStyleDescriptor { Value = TextOverflow.Marquee };
            _title.Invalidate();
            Layout();

            Assert.AreEqual(1, _surface.MarqueeCount);

            _root.RemoveChild(_title);
            Layout();
            _scheduler.FireAll();

            Assert.AreEqual(0, _surface.MarqueeCount, "a detached title is dropped");

            _root.AddChild(_title);
            Layout();

            Assert.AreEqual(1, _surface.MarqueeCount, "and taken back when it returns");
        }

        [TestMethod]
        public void ARuleFromAStylesheetMakesAMarqueeToo()
        {
            var registry = new StyleRegistry();
            registry.Add(new Ixen.StyleSheets.AllGeneratedStyles_StyleSheet());

            _title.Styles.TextOverflow = new TextOverflowStyleDescriptor();
            _title.Name = "generated_marquee";
            _surface.Styles = registry;
            Layout();

            Assert.IsTrue(_title.MarqueeWanted);
            Assert.AreEqual(1, _surface.MarqueeCount);
        }

        [TestMethod]
        public void TheTextMovesInsideItsBoxAndNowhereElse()
        {
            using (SKBitmap still = _surface.RenderToBitmap())
            {
                Ticks(Marquee.PAUSE_TICKS + 40);

                using (SKBitmap moved = _surface.RenderToBitmap())
                {
                    bool inside = false;

                    for (int x = 20; x < 120 && !inside; x++)
                    {
                        for (int y = 20; y < 44 && !inside; y++)
                        {
                            inside = still.GetPixel(x, y) != moved.GetPixel(x, y);
                        }
                    }

                    Assert.IsTrue(inside, "twenty units later the ink inside the box has moved");

                    for (int x = 121; x < 300; x++)
                    {
                        for (int y = 0; y < 100; y++)
                        {
                            Assert.AreEqual(0, moved.GetPixel(x, y).Alpha, $"nothing is drawn past the box ({x}, {y})");
                        }
                    }
                }
            }
        }
    }
}
