using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    internal sealed class OneRow : IRegionRow
    {
        private readonly VisualElement _element = new VisualElement();

        public int ElementCount => 1;

        public VisualElement ElementAt(int index) => _element;
    }

    [TestClass]
    public class VirtualListKeyTests
    {
        private const int VIEWPORT = 400;
        private const float ITEM = 20;

        private VisualElement _root;
        private VirtualList _list;
        private List<string> _items;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _items = Enumerable.Range(0, 100).Select(i => "track " + i).ToList();

            _list = new VirtualList { Name = "queue", ItemHeight = ITEM };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _list.SetItems(_items, () => new VisualElement { Focusable = true }, (row, index) => row.Text = _items[index]);

            _root.AddChild(_list);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };
            Layout();
        }

        private void Layout()
        {
            for (int pass = 0; pass < 3; pass++)
            {
                _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }
        }

        private string TopText()
        {
            float top = _list.ContentY + _list.ScrollY;

            return _list.RealisedRows.OrderBy(r => r.Y).First(r => r.Y + r.ActualHeight > top + 0.01f).Text;
        }

        private float TopCut()
        {
            float top = _list.ContentY + _list.ScrollY;

            return _list.RealisedRows.OrderBy(r => r.Y).First(r => r.Y + r.ActualHeight > top + 0.01f).Y - top;
        }

        private void ScrollTo(float y)
        {
            _list.ScrollY = y;
            Layout();
        }

        [TestMethod]
        public void TracksQueuedAboveDoNotMoveWhatYouAreLookingAt()
        {
            ScrollTo(400);

            Assert.AreEqual("track 20", TopText());

            _items.InsertRange(0, new[] { "new a", "new b", "new c" });
            _list.Refresh();
            Layout();

            Assert.AreEqual("track 20", TopText(), "the item, not the index, stays at the top");
            Assert.AreEqual(460, _list.ScrollY, 0.01f);
        }

        [TestMethod]
        public void TracksRemovedAboveDoNotMoveItEither()
        {
            ScrollTo(410);

            _items.RemoveRange(0, 5);
            _list.Refresh();
            Layout();

            Assert.AreEqual("track 20", TopText());
            Assert.AreEqual(-10, TopCut(), 0.01f, "with the same cut of the top row");
        }

        [TestMethod]
        public void DraggingTheTopTrackAwayDoesNotChaseIt()
        {
            ScrollTo(400);

            string moved = _items[20];
            _items.RemoveAt(20);
            _items.Add(moved);
            _list.Refresh();
            Layout();

            Assert.AreEqual(400, _list.ScrollY, 0.01f, "the list stays where it was rather than following the track to the end");
            Assert.AreEqual("track 21", TopText());
        }

        [TestMethod]
        public void AtTheTopNewTracksShow()
        {
            _items.Insert(0, "new a");
            _list.Refresh();
            Layout();

            Assert.AreEqual(0, _list.ScrollY);
            Assert.AreEqual("new a", TopText());
        }

        [TestMethod]
        public void AChangeBelowMovesNothing()
        {
            ScrollTo(400);

            _items.Add("last");
            _items.RemoveAt(60);
            _list.Refresh();
            Layout();

            Assert.AreEqual(400, _list.ScrollY, 0.01f);
            Assert.AreEqual("track 20", TopText());
        }

        [TestMethod]
        public void TheFocusFollowsItsTrack()
        {
            ScrollTo(400);
            _list.MoveFocusTo(22);
            Layout();

            Assert.AreEqual("track 22", _surface.FocusedElement.Text);

            _items.InsertRange(0, new[] { "new a", "new b" });
            _list.Refresh();
            Layout();

            Assert.AreEqual("track 22", _surface.FocusedElement?.Text, "the focus stays on the track, now at index 24");

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Layout();

            Assert.AreEqual("track 23", _surface.FocusedElement?.Text, "and the arrows go on from there");
        }

        [TestMethod]
        public void ARegionCanNameItsOwnKeys()
        {
            var ids = Enumerable.Range(0, 100).ToList();

            _list.SetRegion(() => ids.Count,
                () => new OneRow(),
                (row, index) => row.ElementAt(0).Text = "id " + ids[index],
                index => ids[index] / 1);
            Layout();
            ScrollTo(400);

            ids.Insert(0, -1);
            _list.Refresh();
            Layout();

            Assert.AreEqual("id 20", TopText());
        }
    }
}
