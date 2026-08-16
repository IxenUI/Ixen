using Ixen.Core.Components;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class RepeatTests
    {
        private const int VIEWPORT = 200;

        private ListComponent _component;
        private VisualElement _list;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _component = new ListComponent { Title = "shopping" };
            _component.Items.Add(new ListItem { Name = "milk", Count = 2 });
            _component.Items.Add(new ListItem { Name = "eggs", Count = 6 });

            _surface = new IxenSurface(_component) { Styles = new StyleRegistry() };
            _list = _surface.Root.Children[0];

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VisualElement Row(int index) => _list.Children[1 + index];

        [TestMethod]
        public void OneElementIsBuiltPerItem()
        {
            Assert.AreEqual(4, _list.Children.Count, "a header, two rows and a footer");

            Assert.AreEqual("milk", Row(0).Children[0].Text);
            Assert.AreEqual("2 left", Row(0).Children[1].Text);
            Assert.AreEqual("eggs", Row(1).Children[0].Text);
            Assert.AreEqual("6 left", Row(1).Children[1].Text);
        }

        [TestMethod]
        public void TheStaticSiblingsKeepTheirPlaces()
        {
            Assert.AreEqual("list_header", _list.Children[0].Name);
            Assert.AreEqual("shopping", _list.Children[0].Text);
            Assert.AreEqual("list_footer", _list.Children[3].Name,
                "the footer was pushed down by the rows rather than swallowed");
        }

        [TestMethod]
        public void GrowingTheCollectionAddsElements()
        {
            _component.Items.Add(new ListItem { Name = "bread", Count = 1 });
            _component.Refresh();
            Layout();

            Assert.AreEqual(5, _list.Children.Count);
            Assert.AreEqual("bread", Row(2).Children[0].Text);
            Assert.AreEqual("list_footer", _list.Children[4].Name);
        }

        [TestMethod]
        public void ShrinkingTheCollectionRemovesElements()
        {
            _component.Items.RemoveAt(1);
            _component.Refresh();
            Layout();

            Assert.AreEqual(3, _list.Children.Count);
            Assert.AreEqual("milk", Row(0).Children[0].Text);
            Assert.AreEqual("list_footer", _list.Children[2].Name);
        }

        [TestMethod]
        public void AnEmptyCollectionLeavesTheStaticsAlone()
        {
            _component.Items.Clear();
            _component.Refresh();
            Layout();

            Assert.AreEqual(2, _list.Children.Count);
            Assert.AreEqual("list_header", _list.Children[0].Name);
            Assert.AreEqual("list_footer", _list.Children[1].Name);
        }

        [TestMethod]
        public void ANullCollectionIsAnEmptyOne()
        {
            _component.Items = null;
            _component.Refresh();
            Layout();

            Assert.AreEqual(2, _list.Children.Count);
        }

        [TestMethod]
        public void ChangingAValueKeepsTheSameElement()
        {
            VisualElement first = Row(0);

            _component.Items[0].Name = "oat milk";
            _component.Refresh();
            Layout();

            Assert.AreSame(first, Row(0), "identity survives, so focus and scroll inside a row would too");
            Assert.AreEqual("oat milk", first.Children[0].Text);
        }

        [TestMethod]
        public void GrowingKeepsTheElementsThatWereAlreadyThere()
        {
            VisualElement first = Row(0);
            VisualElement second = Row(1);

            _component.Items.Add(new ListItem { Name = "bread", Count = 1 });
            _component.Refresh();
            Layout();

            Assert.AreSame(first, Row(0));
            Assert.AreSame(second, Row(1), "only the new one is built");
        }

        [TestMethod]
        public void ReorderingMovesTheValuesNotTheElements()
        {
            VisualElement first = Row(0);

            ListItem milk = _component.Items[0];
            _component.Items.RemoveAt(0);
            _component.Items.Add(milk);
            _component.Refresh();
            Layout();

            Assert.AreSame(first, Row(0), "reconciliation is by index, so the elements stay put");
            Assert.AreEqual("eggs", first.Children[0].Text, "and the values move through them");
        }

        [TestMethod]
        public void TheRowsAreRealElementsWithTheirStyles()
        {
            Assert.IsTrue(Row(0).HasClass("row"));
            Assert.AreEqual("row", Row(0).Name);
            Assert.IsTrue(Row(0).Width > 0, "they take part in layout like anything else");
        }

        [TestMethod]
        public void RepeatingWithoutAStateChangeChangesNothing()
        {
            _component.Items.Add(new ListItem { Name = "bread", Count = 1 });
            Layout();

            Assert.AreEqual(4, _list.Children.Count,
                "nothing observes the collection, so SetState is what rebuilds it");
        }
    }
}
