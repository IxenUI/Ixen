using Ixen.Core.Input;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class NestedTests
    {
        private const int VIEWPORT = 200;

        private static IxenSurface Laid(NestedComponent component)
        {
            var surface = new IxenSurface(component)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static List<ListItem> Two()
            => new List<ListItem>
            {
                new ListItem { Id = 1, Name = "one", Count = 3 },
                new ListItem { Id = 2, Name = "two", Count = 0 }
            };

        private static VisualElement[] Rows(NestedComponent component)
            => component.Initialize()
                .FindByName("nested_root")
                .Children
                .Where(c => c.Name == "nested_row")
                .ToArray();

        private static string[] Names(VisualElement row)
            => row.Children.Select(c => c.Name).ToArray();

        [TestMethod]
        public void ANestedConditionIsEvaluatedPerRow()
        {
            var component = new NestedComponent { Items = Two() };
            VisualElement[] rows = Rows(component);

            CollectionAssert.AreEqual(
                new[] { "nested_name", "nested_badge", "nested_tail", "nested_field" },
                Names(rows[0]),
                "the first row has a count, so it takes the @if branch");

            CollectionAssert.AreEqual(
                new[] { "nested_name", "nested_empty", "nested_tail", "nested_field" },
                Names(rows[1]),
                "the second has none, so it takes the @else branch");
        }

        [TestMethod]
        public void ANestedBindingReadsTheLoopVariable()
        {
            var component = new NestedComponent { Items = Two() };

            Assert.AreEqual("3 left", Rows(component)[0].Children[1].Text);
        }

        [TestMethod]
        public void AStaticSiblingAfterNestedRegionsKeepsItsBinding()
        {
            var component = new NestedComponent { Items = Two() };

            Assert.AreEqual("one tail", Rows(component)[0].Children[2].Text,
                "its index path has to add the element count of both regions before it");
            Assert.AreEqual("two tail", Rows(component)[1].Children[2].Text);
        }

        [TestMethod]
        public void ALoopNestedInAConditionWorks()
        {
            var component = new NestedComponent
            {
                ShowDeep = true,
                Words = new List<string> { "a", "b", "c" }
            };

            VisualElement panel = component.Initialize().FindByName("deep_panel");

            Assert.IsNotNull(panel);
            CollectionAssert.AreEqual(
                new[] { "a", "b", "c" },
                panel.Children.Select(c => c.Text).ToArray());
        }

        [TestMethod]
        public void ClosingTheOuterConditionTakesTheInnerLoopWithIt()
        {
            var component = new NestedComponent
            {
                ShowDeep = true,
                Words = new List<string> { "a" }
            };

            IxenSurface surface = Laid(component);

            Assert.IsNotNull(component.Initialize().FindByName("deep_word"));

            component.ShowDeep = false;
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(component.Initialize().FindByName("deep_panel"));
            Assert.IsNull(component.Initialize().FindByName("deep_word"));
        }

        [TestMethod]
        public void ReopeningTheOuterConditionRebuildsTheInnerLoop()
        {
            var component = new NestedComponent
            {
                ShowDeep = true,
                Words = new List<string> { "a", "b" }
            };

            IxenSurface surface = Laid(component);

            component.ShowDeep = false;
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            component.ShowDeep = true;
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(
                new[] { "a", "b" },
                component.Initialize().FindByName("deep_panel").Children.Select(c => c.Text).ToArray(),
                "the inner state belongs to the outer row, so it is rebuilt with it rather than left stale");
        }

        [TestMethod]
        public void AKeyedReorderCarriesTheNestedStateWithTheRow()
        {
            var component = new NestedComponent { Items = Two() };
            IxenSurface surface = Laid(component);

            VisualElement badge = Rows(component)[0].Children[1];

            component.Items.Reverse();
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            VisualElement[] rows = Rows(component);

            Assert.AreSame(badge, rows[1].Children[1],
                "the row object owns its nested lists, so they travel with the element");
            Assert.AreEqual("3 left", rows[1].Children[1].Text);
            Assert.AreEqual("none", rows[0].Children[1].Text);
        }

        [TestMethod]
        public void ANestedConditionFollowsItsRowWhenTheItemChanges()
        {
            var component = new NestedComponent { Items = Two() };
            IxenSurface surface = Laid(component);

            component.Items[1].Count = 7;
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(
                new[] { "nested_name", "nested_badge", "nested_tail", "nested_field" },
                Names(Rows(component)[1]),
                "the branch switches inside the surviving row");
        }

        [TestMethod]
        public void AnActionInsideALoopReachesTheLoopVariable()
        {
            var component = new NestedComponent { Items = Two() };
            VisualElement name = Rows(component)[1].Children[0];

            name.RaisePointerClick(new PointerEventArgs(0, 0, PointerButton.Left, name));

            Assert.AreSame(component.Items[1], component.Picked,
                "the handler is an Action the view reassigns per row, so it sees the right item");
        }

        [TestMethod]
        public void ATwoWayBindingInsideALoopWritesBackToTheItem()
        {
            var component = new NestedComponent { Items = Two() };
            var field = (TextField)Rows(component)[0].Children[3];

            field.Text = string.Empty;
            field.RaiseTextInput(new TextInputEventArgs("z", field));

            Assert.AreEqual("z", component.Items[0].Name);
        }
    }
}
