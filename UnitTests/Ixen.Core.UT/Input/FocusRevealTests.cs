using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class FocusRevealTests
    {
        private const int VIEWPORT = 400;
        private const float CARD = 80;
        private const float SHELF = 200;

        private VisualElement _root;
        private VisualElement _shelf;
        private VisualElement _list;
        private readonly List<VisualElement> _cards = new List<VisualElement>();
        private readonly List<VisualElement> _rows = new List<VisualElement>();
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _shelf = new VisualElement { Name = "shelf", Scrollable = true };
            _shelf.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            _shelf.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SHELF };
            _shelf.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };

            for (int i = 0; i < 10; i++)
            {
                var card = new VisualElement { Name = "card" + i, Focusable = true };
                card.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = CARD };
                card.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
                _cards.Add(card);
                _shelf.AddChild(card);
            }

            _list = new VisualElement { Name = "list", Scrollable = true };
            _list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            for (int i = 0; i < 10; i++)
            {
                var row = new VisualElement { Name = "row" + i, Focusable = true };
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
                _rows.Add(row);
                _list.AddChild(row);
            }

            _root.AddChildren(_shelf, _list);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Tab(int times)
        {
            for (int i = 0; i < times; i++)
            {
                _surface.KeyDown(Key.Tab, KeyModifiers.None);
                Layout();
            }
        }

        [TestMethod]
        public void TabbingAlongAShelfBringsEachCardIntoView()
        {
            Tab(4);

            Assert.AreSame(_cards[3], _surface.FocusedElement);
            Assert.AreEqual(4 * CARD - _shelf.ContentWidth, _shelf.ScrollX, 0.01f,
                "the fourth card ends at 320 in a shelf that shows 200: it is brought just into view, "
                + "its right edge on the shelf's");

            Tab(1);

            Assert.AreEqual(5 * CARD - _shelf.ContentWidth, _shelf.ScrollX, 0.01f);
        }

        [TestMethod]
        public void FocusingACardBehindScrollsTheOtherWay()
        {
            _shelf.ScrollX = _shelf.MaxScrollX;
            Layout();

            _surface.Focus(_cards[1]);
            Layout();

            Assert.AreEqual(CARD, _shelf.ScrollX, 0.01f, "the card's left edge comes to the shelf's left edge");
        }

        [TestMethod]
        public void TabbingIntoAListBelowTheFoldScrollsIt()
        {
            _surface.Focus(_rows[2]);
            Layout();

            Tab(3);

            Assert.AreSame(_rows[5], _surface.FocusedElement);
            Assert.AreEqual(6 * 30 - _list.ContentHeight, _list.ScrollY, 0.01f,
                "row 5 ends at 180 in a list that shows 100");
        }

        [TestMethod]
        public void AClickFocusesWithoutScrolling()
        {
            VisualElement card = _cards[2];
            float x = card.X + 10;
            float y = card.Y + 10;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
            Layout();

            Assert.AreSame(card, _surface.FocusedElement);
            Assert.AreEqual(0, _shelf.ScrollX,
                "the third card is cut by the shelf's edge, but the pointer is on it: what is clicked "
                + "stays under the pointer");
        }

        [TestMethod]
        public void ScrollIntoViewWorksSidewaysToo()
        {
            _cards[7].ScrollIntoView();
            Layout();

            Assert.AreEqual(8 * CARD - _shelf.ContentWidth, _shelf.ScrollX, 0.01f);
        }

        [TestMethod]
        public void FocusGivenBeforeTheLayoutIsRevealedOnceItIsDone()
        {
            var late = new VisualElement { Name = "late", Focusable = true };
            late.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            _list.AddChild(late);

            _surface.Focus(late);
            Layout();

            Assert.AreEqual(11 * 30 - _list.ContentHeight, _list.ScrollY, 0.01f,
                "the new row had no position yet, so the reveal waited for the layout that gave it one");
        }

        [TestMethod]
        public void AScrollAfterTheFocusIsNotUndone()
        {
            _surface.Focus(_rows[5]);
            _list.ScrollY = 0;
            Layout();

            Assert.AreEqual(0, _list.ScrollY, "the reveal happened at the focus, not at the next layout");
        }
    }
}
