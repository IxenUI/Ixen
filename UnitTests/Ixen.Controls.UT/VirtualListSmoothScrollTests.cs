using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class VirtualListSmoothScrollTests
    {
        private const float ITEM = 20;

        private VisualElement _root;
        private VirtualList _list;
        private List<string> _songs;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;

        private void Build(bool smooth)
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _songs = Enumerable.Range(0, 200).Select(i => "song " + i).ToList();

            _list = new VirtualList { Name = "list", ItemHeight = ITEM };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            if (smooth)
            {
                _list.Styles.ScrollBehavior = new ScrollBehaviorStyleDescriptor { Value = ScrollBehavior.Smooth };
            }

            _list.SetItems(_songs, () => new VisualElement(), (row, index) => row.Text = _songs[index]);
            _root.AddChild(_list);

            _scheduler = new FakeScheduler();
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = _scheduler };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(400, 400);

        private void Ticks(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _scheduler.Tick();
                Layout();
            }
        }

        private bool Shows(string text) => _list.RealisedRows.Any(row => row.Text == text);

        [TestMethod]
        public void GoingToTheTrackThatIsPlayingGlidesThere()
        {
            Build(smooth: true);

            _list.ScrollTo(120);
            Layout();

            Assert.IsTrue(_list.ScrollY < 120 * ITEM, "not in one jump");

            Ticks(20);

            Assert.AreEqual(120 * ITEM, _list.ScrollY, 0.01f);
            Assert.IsTrue(Shows("song 120"), "and the rows on the way were built as it went");
        }

        [TestMethod]
        public void ScrollIntoViewGlidesToo()
        {
            Build(smooth: true);

            _list.ScrollIntoView(50);
            Layout();
            Ticks(20);

            Assert.AreEqual(51 * ITEM - 100, _list.ScrollY, 0.01f, "the row's bottom at the window's bottom");
            Assert.IsTrue(Shows("song 50"));
        }

        [TestMethod]
        public void WithoutSmoothItJumps()
        {
            Build(smooth: false);

            _list.ScrollTo(120);
            Layout();

            Assert.AreEqual(120 * ITEM, _list.ScrollY, 0.01f);
            Assert.IsTrue(Shows("song 120"));
        }
    }
}
