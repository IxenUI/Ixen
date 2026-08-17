using Ixen.Core.Input;
using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class RegionTests
    {
        private const int VIEWPORT = 200;

        private static IxenSurface Laid(RegionComponent component)
        {
            var surface = new IxenSurface(component)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static string[] Names(RegionComponent component)
            => component.Initialize()
                .FindByName("region_root")
                .Children
                .Select(c => c.Name)
                .ToArray();

        [TestMethod]
        public void AFalseConditionLeavesTheNodesOut()
        {
            var component = new RegionComponent();

            CollectionAssert.AreEqual(
                new[] { "region_before", "region_middle", "region_after" },
                Names(component));
        }

        [TestMethod]
        public void ATrueConditionInsertsInDeclarationOrder()
        {
            var component = new RegionComponent { ShowTitle = true, ShowFooter = true };

            CollectionAssert.AreEqual(
                new[]
                {
                    "region_before", "region_title", "region_subtitle",
                    "region_middle", "region_footer", "region_after"
                },
                Names(component),
                "static siblings keep their places; each region is spliced at a computed offset");
        }

        [TestMethod]
        public void ARegionWithSeveralNodesAppearsAndDisappearsTogether()
        {
            var component = new RegionComponent { ShowTitle = true };

            CollectionAssert.AreEqual(
                new[] { "region_before", "region_title", "region_subtitle", "region_middle", "region_after" },
                Names(component));
        }

        [TestMethod]
        public void OnlyTheSecondRegionCanBeOpen()
        {
            var component = new RegionComponent { ShowFooter = true };

            CollectionAssert.AreEqual(
                new[] { "region_before", "region_middle", "region_footer", "region_after" },
                Names(component),
                "the footer offset accounts for the empty region before it");
        }

        [TestMethod]
        public void ABindingInsideARegionIsApplied()
        {
            var component = new RegionComponent { ShowTitle = true, Title = "hello" };

            Assert.AreEqual("hello", component.Initialize().FindByName("region_title").Text);
        }

        [TestMethod]
        public void TogglingOnAddsTheNodesOnTheNextLayout()
        {
            var component = new RegionComponent();
            IxenSurface surface = Laid(component);

            Assert.IsNull(component.Initialize().FindByName("region_title"));

            component.Toggle(true, false);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNotNull(component.Initialize().FindByName("region_title"));
        }

        [TestMethod]
        public void TogglingOffRemovesThem()
        {
            var component = new RegionComponent { ShowTitle = true };
            IxenSurface surface = Laid(component);

            component.Toggle(false, false);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(
                new[] { "region_before", "region_middle", "region_after" },
                Names(component));
        }

        [TestMethod]
        public void ARebuiltRegionIsANewElement()
        {
            var component = new RegionComponent { ShowTitle = true };
            IxenSurface surface = Laid(component);

            VisualElement first = component.Initialize().FindByName("region_title");

            component.Toggle(false, false);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            component.Toggle(true, false);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreNotSame(first, component.Initialize().FindByName("region_title"),
                "closing a region destroys its elements, so focus and hover inside it do not survive");
        }

        [TestMethod]
        public void AnElementThatStaysIsNotRecreated()
        {
            var component = new RegionComponent { ShowTitle = true };
            IxenSurface surface = Laid(component);

            VisualElement title = component.Initialize().FindByName("region_title");

            component.Toggle(true, true);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreSame(title, component.Initialize().FindByName("region_title"),
                "opening another region must not disturb the one already open");
        }

        [TestMethod]
        public void AnActionInsideARegionIsWired()
        {
            var component = new RegionComponent { ShowFooter = true };
            VisualElement footer = component.Initialize().FindByName("region_footer");

            footer.RaisePointerClick(new PointerEventArgs(0, 0, PointerButton.Left, footer));

            Assert.AreEqual(1, component.Bumps,
                "the handler is wired in the factory, where the model field is in scope");
        }
    }
}
