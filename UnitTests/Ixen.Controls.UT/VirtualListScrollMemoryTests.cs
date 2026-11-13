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
    public class VirtualListScrollMemoryTests
    {
        private const int VIEWPORT = 400;
        private const float ROW = 20;

        private VisualElement _root;
        private IxenSurface _surface;
        private readonly List<string> _items = Enumerable.Range(0, 1000).Select(i => "track " + i).ToList();

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VirtualList Tracks(string key = null)
        {
            var list = new VirtualList { Name = "tracks", ItemHeight = ROW, PersistScroll = true, ScrollKey = key };
            list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            list.SetItems(_items, () => new VisualElement(), (row, index) => row.Text = _items[index]);

            return list;
        }

        [TestMethod]
        public void AListThatComesBackBuildsTheRowsAtItsPlaceInItsFirstLayout()
        {
            VirtualList before = Tracks();

            _root.AddChild(before);
            Layout();

            before.ScrollY = 5000;
            Layout();

            _root.RemoveChild(before);
            Layout();

            VirtualList list = Tracks();

            _root.AddChild(list);
            Layout();

            Assert.AreEqual(5000, list.ScrollY, 0.01f);
            Assert.AreEqual(249, list.FirstRealised,
                "row 250 is at the top, less one of overscan: the offset moved during the measure, "
                + "and the prepass ran again to build the window there rather than at row 0");
        }

        [TestMethod]
        public void SoDoesAListThatWasAlreadyLaidOut()
        {
            VirtualList list = Tracks("album/12");

            _root.AddChild(list);
            Layout();

            list.ScrollY = 5000;
            Layout();

            list.ScrollKey = "album/40";
            Layout();

            Assert.AreEqual(0, list.FirstRealised);

            list.ScrollKey = "album/12";
            Layout();

            Assert.AreEqual(249, list.FirstRealised,
                "its size no longer changes, so only the offset moving during the measure asks for "
                + "the second prepass");
        }
    }
}
