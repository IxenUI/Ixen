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
    public class NamedSlotTests
    {
        private const int VIEWPORT = 200;

        private NamedSlotHostComponent _host;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _host = new NamedSlotHostComponent();
            _surface = new IxenSurface(_host) { Styles = new StyleRegistry() };
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VisualElement Card() => _host.Initialize().FindByName("card").Children[0];

        private VisualElement Slot(string name)
            => Card().Children.First(c => c is Slot && c.Name == name);

        private VisualElement DefaultSlot()
            => Card().Children.First(c => c is Slot && string.IsNullOrEmpty(c.Name));

        private static string[] Names(VisualElement element)
            => element.Children.Select(c => c.Name).ToArray();

        [TestMethod]
        public void EachChildGoesToTheSlotItNames()
        {
            _host.Initialize();

            CollectionAssert.AreEqual(new[] { "to_head", "to_head_too" }, Names(Slot("head")));
            CollectionAssert.AreEqual(new[] { "to_body_group", "clicker" }, Names(Slot("body")));
        }

        [TestMethod]
        public void AChildThatNamesNoSlotGoesToTheUnnamedOne()
        {
            _host.Initialize();

            CollectionAssert.AreEqual(new[] { "leftover" }, Names(DefaultSlot()));
        }

        [TestMethod]
        public void TheComponentsOwnElementsKeepTheirPlaces()
        {
            CollectionAssert.AreEqual(
                new[] { "head", "named_divider", "body", null },
                Names(Card()),
                "the slots stay where the view declared them, divider included");
        }

        [TestMethod]
        public void TwoChildrenCanShareOneSlotAndKeepTheirOrder()
        {
            _host.Initialize();

            VisualElement head = Slot("head");

            Assert.AreEqual(2, head.Children.Count);
            Assert.AreEqual("to_head", head.Children[0].Name);
            Assert.AreEqual("to_head_too", head.Children[1].Name);
        }

        [TestMethod]
        public void GroupingIsJustWrappingInAnElement()
        {
            _host.Initialize();

            VisualElement group = Slot("body").FindByName("to_body_group");

            CollectionAssert.AreEqual(new[] { "inner_a", "inner_b" }, Names(group),
                "a wrapper is how several children reach one slot together");
        }

        [TestMethod]
        public void ABindingInsideANamedSlotIsApplied()
        {
            _host.Caption = "from the host";
            _host.Refresh();
            Layout();

            Assert.AreEqual("from the host", Slot("head").FindByName("to_head").Text);
        }

        [TestMethod]
        public void AnActionInsideANamedSlotReachesTheHostModel()
        {
            _host.Initialize();

            VisualElement button = Slot("body").FindByName("clicker");
            button.RaisePointerClick(new PointerEventArgs(0, 0, PointerButton.Left, button));

            Assert.AreEqual(1, _host.Bumps);
        }

        [TestMethod]
        public void ARegionGoesToTheDefaultSlotAndCountsOnlyItsOwnStatics()
        {
            _host.Flag = true;
            _host.Refresh();
            Layout();

            CollectionAssert.AreEqual(
                new[] { "leftover", "region_child" },
                Names(DefaultSlot()),
                "the region inserts after the one static that landed in this slot, not after all four");
        }

        [TestMethod]
        public void ClosingTheRegionLeavesTheNamedSlotsAlone()
        {
            _host.Flag = true;
            _host.Refresh();
            Layout();

            _host.Flag = false;
            _host.Refresh();
            Layout();

            CollectionAssert.AreEqual(new[] { "leftover" }, Names(DefaultSlot()));
            CollectionAssert.AreEqual(new[] { "to_head", "to_head_too" }, Names(Slot("head")));
        }

        [TestMethod]
        public void ALoneSlotIsTheDefaultWhateverItIsNamed()
        {
            var component = new SlotHostComponent();
            VisualElement card = component.Initialize().FindByName("first").Children[0];

            VisualElement slot = card.Children.First(c => c is Slot);

            Assert.AreEqual("card_content", slot.Name,
                "the slot is named so a stylesheet can reach it");
            Assert.IsTrue(slot.Children.Count > 0,
                "and being named does not stop it receiving content that names no slot");
        }

        [TestMethod]
        public void AnUnknownSlotNameIsReported()
        {
            InvalidOperationException error =
                Assert.Throws<InvalidOperationException>(() => new MissingSlotHostComponent().Initialize());

            StringAssert.Contains(error.Message, nameof(NamedSlotCardComponent));
            StringAssert.Contains(error.Message, "heade");
        }

        [TestMethod]
        public void TwoSlotsSharingANameAreReported()
        {
            InvalidOperationException error =
                Assert.Throws<InvalidOperationException>(() => new DuplicateSlotHostComponent().Initialize());

            StringAssert.Contains(error.Message, nameof(DuplicateSlotComponent));
            StringAssert.Contains(error.Message, "more than one <Slot> named 'head'");
        }

        [TestMethod]
        public void TheSlotsAreOrdinaryElementsTheStylesheetCanReach()
        {
            _host.Initialize();

            Assert.AreEqual(nameof(Slot), Slot("head").TypeName);
            Assert.AreEqual("head", Slot("head").Name);
        }

        [TestMethod]
        public void ProjectedContentIsLaidOutInsideItsOwnSlot()
        {
            Layout();

            VisualElement head = Slot("head");
            VisualElement child = head.FindByName("to_head");

            Assert.IsTrue(child.Y >= head.Y);
            Assert.IsTrue(child.Y < Slot("body").Y,
                "content follows the slot's position in the view, not its order in the caller");
        }
    }
}
