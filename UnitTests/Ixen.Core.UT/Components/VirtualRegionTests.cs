using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class VirtualRegionTests
    {
        private const int VIEWPORT = 200;

        private static VirtualComponent Three()
            => new VirtualComponent { Words = new List<string> { "one", "two", "three" } };

        private static RecordingHost HostOf(VirtualComponent component)
            => component.Initialize().FindByName("host") as RecordingHost;

        private static VisualElement Named(VirtualComponent component, string name)
            => component.Initialize().FindByName(name);

        private static string TextOf(IRegionRow row) => row.ElementAt(0).Text;

        [TestMethod]
        public void TheHostIsHandedTheRegionRatherThanHavingItsChildrenSpliced()
        {
            VirtualComponent component = Three();
            RecordingHost host = HostOf(component);

            Assert.AreEqual(1, host.Handovers);
            Assert.AreEqual(0, host.Children.Count);
        }

        [TestMethod]
        public void TheCountComesFromTheSourceRatherThanFromANumberHandedOver()
        {
            VirtualComponent component = Three();
            RecordingHost host = HostOf(component);

            Assert.AreEqual(3, host.Counter());

            component.Words.Add("four");

            Assert.AreEqual(4, host.Counter());
        }

        [TestMethod]
        public void AFactoryBuildsOneElementPerRow()
        {
            RecordingHost host = HostOf(Three());
            IRegionRow row = host.Factory();

            Assert.AreEqual(1, row.ElementCount);
            Assert.AreEqual("word_row", row.ElementAt(0).Name);
        }

        [TestMethod]
        public void TheBindClosureFillsARowForOneIndex()
        {
            RecordingHost host = HostOf(Three());

            Assert.AreEqual("two", TextOf(host.Realise(1)));
        }

        [TestMethod]
        public void ARowIsReboundToWhateverItemItIsGivenNext()
        {
            RecordingHost host = HostOf(Three());
            IRegionRow row = host.Factory();

            host.Binder(row, 0);

            Assert.AreEqual("one", TextOf(row));

            host.Binder(row, 2);

            Assert.AreEqual("three", TextOf(row));
        }

        [TestMethod]
        public void AnActionInsideARowSeesTheItemItWasLastBoundTo()
        {
            VirtualComponent component = Three();
            RecordingHost host = HostOf(component);
            IRegionRow row = host.Factory();

            host.Binder(row, 0);
            host.Binder(row, 2);

            row.ElementAt(0).PerformClick();

            Assert.AreEqual("three", component.Picked);
        }

        [TestMethod]
        public void ANestedRegionInsideARowFollowsTheItem()
        {
            VirtualComponent component = Three();
            RecordingHost host = HostOf(component);
            IRegionRow row = host.Factory();

            component.Pick("two");

            host.Binder(row, 0);

            Assert.AreEqual(0, row.ElementAt(0).Children.Count);

            host.Binder(row, 1);

            Assert.AreEqual(1, row.ElementAt(0).Children.Count);
            Assert.AreEqual("picked", row.ElementAt(0).Children[0].Text);
        }

        [TestMethod]
        public void AParentThatIsNotAHostStillSplicesItsRows()
        {
            VirtualComponent component = Three();

            CollectionAssert.AreEqual(
                new[] { "one", "two", "three" },
                Named(component, "plain").Children.Select(c => c.Text).ToArray());
        }

        [TestMethod]
        public void AHostInsideARowHandsOverToo()
        {
            var component = new VirtualComponent
            {
                Groups = new List<List<string>>
                {
                    new List<string> { "a", "b" },
                    new List<string> { "c", "d", "e" }
                }
            };

            VisualElement outer = Named(component, "outer");

            Assert.AreEqual(2, outer.Children.Count);

            var first = outer.Children[0].Children[0] as RecordingHost;
            var second = outer.Children[1].Children[0] as RecordingHost;

            Assert.AreEqual(2, first.Counter());
            Assert.AreEqual(3, second.Counter());
            Assert.AreEqual("b", TextOf(first.Realise(1)));
            Assert.AreEqual("e", TextOf(second.Realise(2)));
        }

        [TestMethod]
        public void TwoPlainLoopsMayNestInsideOneAnother()
        {
            var component = new VirtualComponent
            {
                Groups = new List<List<string>>
                {
                    new List<string> { "a", "b" },
                    new List<string> { "c", "d", "e" }
                }
            };

            VisualElement nested = Named(component, "nested");

            Assert.AreEqual(2, nested.Children.Count);

            CollectionAssert.AreEqual(
                new[] { "a", "b" },
                nested.Children[0].Children.Select(c => c.Text).ToArray());

            CollectionAssert.AreEqual(
                new[] { "c", "d", "e" },
                nested.Children[1].Children.Select(c => c.Text).ToArray());
        }

        [TestMethod]
        public void AConditionalInsideAHostIsNotVirtualised()
        {
            var component = new VirtualComponent { Flag = true };
            var gate = Named(component, "gate") as RecordingHost;

            Assert.AreEqual(0, gate.Handovers);
            Assert.AreEqual(1, gate.Children.Count);
            Assert.AreEqual("on", gate.Children[0].Text);
        }

        [TestMethod]
        public void AReplayHandsTheRegionOverAgainSoTheVisibleRowsAreRebound()
        {
            VirtualComponent component = Three();
            RecordingHost host = HostOf(component);

            var surface = new IxenSurface(component)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            int before = host.Handovers;

            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(before + 1, host.Handovers);
        }
    }
}
