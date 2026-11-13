using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class ScrollMemoryTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _page;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _page = new VisualElement { Name = "page" };
            _page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            root.AddChild(_page);

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private static VisualElement Library(bool persist = true, string key = null, int rows = 50)
        {
            var library = new VisualElement { Name = "library", Scrollable = true, PersistScroll = persist, ScrollKey = key };
            library.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            library.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            library.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            Fill(library, rows);

            return library;
        }

        private static void Fill(VisualElement library, int rows)
        {
            for (int i = 0; i < rows; i++)
            {
                var row = new VisualElement();
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
                library.AddChild(row);
            }
        }

        private void LeaveAt(float scrollY)
        {
            VisualElement first = Show(Library());

            first.ScrollY = scrollY;
            Layout();

            Leave(first);
        }

        [TestMethod]
        public void ContentThatArrivesLaterIsScrolledToOnceItIsThere()
        {
            LeaveAt(300);

            VisualElement again = Show(Library(rows: 3));

            Assert.AreEqual(0, again.ScrollY, "three rows do not scroll");

            Fill(again, 20);
            Layout();

            Assert.AreEqual(360, again.MaxScrollY, 0.01f);
            Assert.AreEqual(300, again.ScrollY, 0.01f,
                "the measure clamped the offset to what was there; the wanted value waited for the rest");
        }

        [TestMethod]
        public void TheUserScrollingFirstWins()
        {
            LeaveAt(300);

            VisualElement again = Show(Library(rows: 10));

            Assert.AreEqual(100, again.ScrollY, 0.01f, "as far as it goes for now");

            again.ScrollBy(0, -40);
            Layout();

            Fill(again, 40);
            Layout();

            Assert.AreEqual(60, again.ScrollY, 0.01f, "a scroll by the user ends the wait");
        }

        [TestMethod]
        public void LeavingWhileStillWaitingKeepsWhatWasWanted()
        {
            LeaveAt(300);

            VisualElement waiting = Show(Library(rows: 3));

            Leave(waiting);

            Assert.AreEqual(300, Show(Library()).ScrollY, 0.01f,
                "a page left before its library loaded would otherwise remember 0 over the reader's place");
        }

        private VisualElement Show(VisualElement library)
        {
            _page.AddChild(library);
            Layout();

            return library;
        }

        private void Leave(VisualElement library)
        {
            _page.RemoveChild(library);
            Layout();
        }

        [TestMethod]
        public void APageThatComesBackFindsItsListWhereItWasLeft()
        {
            VisualElement first = Show(Library());

            first.ScrollY = 300;
            Layout();

            Leave(first);

            VisualElement again = Show(Library());

            Assert.AreNotSame(first, again, "an @if rebuilds its branch: the element is a new one");
            Assert.AreEqual(300, again.ScrollY, 0.01f);
        }

        [TestMethod]
        public void OnlyAnElementThatAskedIsRemembered()
        {
            VisualElement first = Show(Library(false));

            first.ScrollY = 300;
            Layout();

            Leave(first);

            Assert.AreEqual(0, _surface.RememberedScrolls);
            Assert.AreEqual(0, Show(Library()).ScrollY);
        }

        [TestMethod]
        public void AnElementAskedOnceItIsShownIsRememberedToo()
        {
            VisualElement first = Show(Library(false));

            first.PersistScroll = true;
            first.ScrollY = 300;
            Layout();

            Leave(first);

            Assert.AreEqual(300, Show(Library()).ScrollY, 0.01f,
                "the path is taken at attach; asked later, from code, the element had none and was forgotten");
        }

        [TestMethod]
        public void TheKeyTellsTwoContentsOfOneListApart()
        {
            VisualElement first = Show(Library(true, "album/12"));

            first.ScrollY = 300;
            Layout();

            Leave(first);

            Assert.AreEqual(0, Show(Library(true, "album/40")).ScrollY,
                "another album's tracks start at the top");
        }

        [TestMethod]
        public void ChangingTheKeyOfAListThatStaysSwapsItsPlace()
        {
            VisualElement tracks = Show(Library(true, "album/12"));

            tracks.ScrollY = 300;
            Layout();

            tracks.ScrollKey = "album/40";
            Layout();

            Assert.AreEqual(0, tracks.ScrollY, "the list was reused for another album: it goes back to the top");

            tracks.ScrollY = 100;
            Layout();

            tracks.ScrollKey = "album/12";
            Layout();

            Assert.AreEqual(300, tracks.ScrollY, 0.01f, "and the first album's place is still there");

            tracks.ScrollKey = "album/40";
            Layout();

            Assert.AreEqual(100, tracks.ScrollY, 0.01f);
        }
    }
}
