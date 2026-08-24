using Ixen.Core.Input;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class SlotTests
    {
        private const int VIEWPORT = 200;

        private static VisualElement Card(SlotHostComponent component, string name)
            => component.Initialize().FindByName(name).Children[0];

        private static VisualElement SlotOf(VisualElement card)
            => card.Children.First(c => c is Slot);

        private static string[] Names(VisualElement element)
            => element.Children.Select(c => c.Name).ToArray();

        [TestMethod]
        public void TheProjectedContentLandsInTheSlotAndNotOnTheWrapper()
        {
            var component = new SlotHostComponent();
            VisualElement card = Card(component, "first");

            CollectionAssert.AreEqual(
                new[] { "card_title", "card_content", "card_footer" },
                Names(card),
                "The component's own view must keep its shape, with the slot in the middle.");

            CollectionAssert.AreEqual(
                new[] { "projected_static", "projected_bound", "projected_action", "inner" },
                Names(SlotOf(card)),
                "The projected children belong to the slot.");
        }

        [TestMethod]
        public void TheSlotIsWhereTheComponentDeclaredIt()
        {
            var component = new SlotHostComponent();
            VisualElement card = Card(component, "first");

            Assert.AreEqual("card_content", SlotOf(card).Name);
            Assert.AreEqual(1, card.Children.IndexOf(SlotOf(card)));
        }

        [TestMethod]
        public void TwoInstancesGetTheirOwnSlot()
        {
            var component = new SlotHostComponent();
            component.Initialize();

            VisualElement first = SlotOf(Card(component, "first"));
            VisualElement second = SlotOf(Card(component, "second"));

            Assert.AreNotSame(first, second);
            CollectionAssert.AreEqual(new[] { "projected_second" }, Names(second));
        }

        [TestMethod]
        public void ASlotGivenNothingStaysAnEmptyElement()
        {
            var component = new SlotHostComponent();

            Assert.AreEqual(0, SlotOf(Card(component, "empty")).Children.Count);
        }

        [TestMethod]
        public void ABindingInsideTheProjectedContentIsAppliedByTheHost()
        {
            var component = new SlotHostComponent { Caption = "from the host" };
            component.Initialize();

            Assert.AreEqual(
                "from the host",
                SlotOf(Card(component, "first")).FindByName("projected_bound").Text);
        }

        [TestMethod]
        public void AnActionInsideTheProjectedContentReachesTheHostModel()
        {
            var component = new SlotHostComponent();
            component.Initialize();

            VisualElement button = SlotOf(Card(component, "first")).FindByName("projected_action");

            button.RaisePointerClick(new PointerEventArgs(0, 0, PointerButton.Left, button));

            Assert.AreEqual(1, component.Bumps);
        }

        [TestMethod]
        public void AConditionalRegionInsideTheProjectedContentWorks()
        {
            var component = new SlotHostComponent();
            var surface = new IxenSurface(component) { Styles = new StyleRegistry() };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            VisualElement slot = SlotOf(Card(component, "first"));

            Assert.IsNull(slot.FindByName("projected_if"));

            component.Flag = true;
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNotNull(slot.FindByName("projected_if"));
        }

        [TestMethod]
        public void ALoopInsideTheProjectedContentKeepsItsPlaceAmongTheStatics()
        {
            var component = new SlotHostComponent();
            component.Words.Add("one");
            component.Words.Add("two");

            var surface = new IxenSurface(component) { Styles = new StyleRegistry() };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(
                new[]
                {
                    "projected_static", "projected_bound", "projected_action",
                    "projected_word", "projected_word", "inner"
                },
                Names(SlotOf(Card(component, "first"))),
                "The loop must insert after the three statics and before the trailing component.");
        }

        [TestMethod]
        public void ANestedComponentInsideTheProjectedContentIsInitialized()
        {
            var component = new SlotHostComponent();
            component.Initialize();

            VisualElement inner = SlotOf(Card(component, "first")).FindByName("inner");

            Assert.IsNotNull(inner);
            Assert.IsNotNull(inner.FindByName("slotless_label"));
        }

        [TestMethod]
        public void AnOuterComponentDoesNotStealItsOwnChildComponentsSlot()
        {
            var component = new OuterSlotHostComponent();
            VisualElement outer = component.Initialize().FindByName("outer").Children[0];

            CollectionAssert.AreEqual(
                new[] { "outer_projected" },
                Names(outer.Children.First(c => c is Slot)),
                "The content belongs to the outer component's own slot.");

            Assert.AreEqual(
                0,
                outer.FindByName("outer_inner").FindByName("card_content").Children.Count,
                "The nested card's slot must be left alone.");
        }

        [TestMethod]
        public void AComponentWithNoSlotGivenContentIsReported()
        {
            InvalidOperationException error =
                Assert.Throws<InvalidOperationException>(() => new SlotlessHostComponent().Initialize());

            StringAssert.Contains(error.Message, nameof(SlotlessCardComponent));
            StringAssert.Contains(error.Message, "<Slot>");
        }

        [TestMethod]
        public void AComponentWithTwoSlotsGivenContentIsReported()
        {
            InvalidOperationException error =
                Assert.Throws<InvalidOperationException>(() => new TwoSlotsHostComponent().Initialize());

            StringAssert.Contains(error.Message, nameof(TwoSlotsCardComponent));
            StringAssert.Contains(error.Message, "more than one");
        }

        [TestMethod]
        public void ASlotIsAnOrdinaryElementTheStylesheetCanReach()
        {
            var component = new SlotHostComponent();
            VisualElement slot = SlotOf(Card(component, "first"));

            Assert.AreEqual(nameof(Slot), slot.TypeName);
        }

        [TestMethod]
        public void TheProjectedContentIsLaidOutInsideTheSlot()
        {
            var component = new SlotHostComponent();
            var surface = new IxenSurface(component) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            VisualElement slot = SlotOf(Card(component, "first"));
            VisualElement projected = slot.FindByName("projected_static");

            Assert.IsTrue(projected.X >= slot.X, "The child sits inside its slot horizontally.");
            Assert.IsTrue(projected.Y >= slot.Y, "The child sits inside its slot vertically.");
            Assert.IsTrue(slot.ActualHeight > 0, "The slot takes the height of its content.");
        }

        [TestMethod]
        public void ASlottedComponentInsideARegionGetsItsOwnContent()
        {
            var component = new SlotHostComponent { Caption = "row caption", Flag = true };
            var surface = new IxenSurface(component) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            VisualElement card = component.Initialize().FindByName("region_card").Children[0];
            VisualElement slot = SlotOf(card);

            CollectionAssert.AreEqual(new[] { "region_projected" }, Names(slot));
            Assert.AreEqual("row caption", slot.FindByName("region_projected").Text);
        }

        [TestMethod]
        public void ClosingTheRegionTakesTheProjectedContentWithIt()
        {
            var component = new SlotHostComponent { Flag = true };
            var surface = new IxenSurface(component) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNotNull(component.Initialize().FindByName("region_card"));

            component.Flag = false;
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(component.Initialize().FindByName("region_card"));
        }
    }
}
