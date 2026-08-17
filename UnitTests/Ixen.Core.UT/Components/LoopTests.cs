using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class LoopTests
    {
        private const int VIEWPORT = 200;

        private static IxenSurface Laid(LoopComponent component)
        {
            var surface = new IxenSurface(component)
            {
                Styles = new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static string[] Names(LoopComponent component)
            => component.Initialize()
                .FindByName("loop_root")
                .Children
                .Select(c => c.Name)
                .ToArray();

        private static string[] Texts(LoopComponent component)
            => component.Initialize()
                .FindByName("loop_root")
                .Children
                .Select(c => c.Text)
                .ToArray();

        private static List<ListItem> Two()
            => new List<ListItem>
            {
                new ListItem { Id = 1, Name = "one", Count = 3 },
                new ListItem { Id = 2, Name = "two", Count = 5 }
            };

        [TestMethod]
        public void AnEmptyCollectionProducesNothing()
        {
            var component = new LoopComponent();

            CollectionAssert.AreEqual(
                new[] { "loop_before", "loop_middle", "loop_after" },
                Names(component));
        }

        [TestMethod]
        public void SeveralNodesPerIterationAreInterleaved()
        {
            var component = new LoopComponent { Items = Two() };

            CollectionAssert.AreEqual(
                new[]
                {
                    "loop_before",
                    "pair_name", "pair_count",
                    "pair_name", "pair_count",
                    "loop_middle", "loop_after"
                },
                Names(component),
                "the group is the iteration, not the node, so nodes alternate per item");
        }

        [TestMethod]
        public void EachNodeOfTheGroupGetsItsOwnBinding()
        {
            var component = new LoopComponent { Items = Two() };

            CollectionAssert.AreEqual(
                new[] { "before", "one", "3x", "two", "5x", "middle", "after" },
                Texts(component));
        }

        [TestMethod]
        public void AnExplicitLoopTypeWorks()
        {
            var component = new LoopComponent { Words = new List<string> { "a", "b" } };

            CollectionAssert.AreEqual(
                new[] { "loop_before", "loop_middle", "word_row", "word_row", "loop_after" },
                Names(component),
                "'foreach (string word in Words)' declares the variable verbatim");
        }

        [TestMethod]
        public void TwoLoopsKeepTheirOwnBands()
        {
            var component = new LoopComponent
            {
                Items = Two(),
                Words = new List<string> { "w" }
            };

            CollectionAssert.AreEqual(
                new[]
                {
                    "loop_before",
                    "pair_name", "pair_count", "pair_name", "pair_count",
                    "loop_middle",
                    "word_row",
                    "loop_after"
                },
                Names(component),
                "the second loop's offset accounts for the first one's actual element count");
        }

        [TestMethod]
        public void GrowingAddsOnlyTheNewIterations()
        {
            var component = new LoopComponent { Items = Two() };
            IxenSurface surface = Laid(component);

            VisualElement firstName = component.Initialize().FindByName("loop_root").Children[1];

            component.Items.Add(new ListItem { Id = 3, Name = "three", Count = 1 });
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(9, component.Initialize().FindByName("loop_root").Children.Count);
            Assert.AreSame(firstName, component.Initialize().FindByName("loop_root").Children[1],
                "an unkeyed loop reconciles by index, so the existing rows are reused");
        }

        [TestMethod]
        public void ShrinkingRemovesFromTheEnd()
        {
            var component = new LoopComponent { Items = Two() };
            IxenSurface surface = Laid(component);

            component.Items.RemoveAt(1);
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(
                new[] { "before", "one", "3x", "middle", "after" },
                Texts(component));
        }

        private static VisualElement[] KeyedGroups(LoopComponent component)
            => component.Initialize()
                .FindByName("loop_root")
                .Children
                .Where(c => c.Name == "keyed_name" || c.Name == "keyed_count")
                .ToArray();

        [TestMethod]
        public void AKeyedGroupMovesWithItsItem()
        {
            var component = new LoopComponent { Keyed = Two() };
            IxenSurface surface = Laid(component);

            VisualElement[] before = KeyedGroups(component);

            component.Keyed.Reverse();
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            VisualElement[] after = KeyedGroups(component);

            Assert.AreSame(before[0], after[2], "the key made the element follow its item");
            Assert.AreSame(before[1], after[3], "and its whole group came with it");
            Assert.AreSame(before[2], after[0]);
            Assert.AreSame(before[3], after[1]);
        }

        [TestMethod]
        public void AKeyedInsertionMovesTheExistingGroups()
        {
            var component = new LoopComponent { Keyed = Two() };
            IxenSurface surface = Laid(component);

            VisualElement[] before = KeyedGroups(component);

            component.Keyed.Insert(0, new ListItem { Id = 9, Name = "new", Count = 7 });
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            VisualElement[] after = KeyedGroups(component);

            Assert.AreEqual(6, after.Length);
            Assert.AreSame(before[0], after[2],
                "inserting at the front moved the existing groups instead of rebuilding them");
            Assert.AreSame(before[2], after[4]);
        }

        [TestMethod]
        public void AKeyedGroupIsBoundAfterAMove()
        {
            var component = new LoopComponent { Keyed = Two() };
            IxenSurface surface = Laid(component);

            component.Keyed.Reverse();
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            CollectionAssert.AreEqual(
                new[] { "two", "5x", "one", "3x" },
                KeyedGroups(component).Select(c => c.Text).ToArray());
        }

        private static VisualElement[] Indexed(LoopComponent component)
            => component.Initialize()
                .FindByName("loop_root")
                .Children
                .Where(c => c.Name == "indexed")
                .ToArray();

        [TestMethod]
        public void AVarIsVisibleToTheLoopThatFollowsIt()
        {
            var component = new LoopComponent { Max = 3 };

            CollectionAssert.AreEqual(
                new[] { "0/3", "1/3", "2/3" },
                Indexed(component).Select(c => c.Text).ToArray(),
                "'@var limit = Max;' is emitted before the '@for' that reads it");
        }

        [TestMethod]
        public void AZeroCountForProducesNothing()
        {
            var component = new LoopComponent { Max = 0 };

            Assert.AreEqual(0, Indexed(component).Length);
            CollectionAssert.AreEqual(
                new[] { "loop_before", "loop_middle", "loop_after" },
                Names(component));
        }

        [TestMethod]
        public void AForGrowsWithoutRebuildingWhatExists()
        {
            var component = new LoopComponent { Max = 2 };
            IxenSurface surface = Laid(component);

            VisualElement first = Indexed(component)[0];

            component.Max = 4;
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(4, Indexed(component).Length);
            Assert.AreSame(first, Indexed(component)[0],
                "the single pass grows as it goes rather than rebuilding");
        }

        [TestMethod]
        public void AForShrinksFromTheEnd()
        {
            var component = new LoopComponent { Max = 4 };
            IxenSurface surface = Laid(component);

            VisualElement first = Indexed(component)[0];

            component.Max = 1;
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, Indexed(component).Length);
            Assert.AreSame(first, Indexed(component)[0], "Trim removes the excess at the end");
        }

        [TestMethod]
        public void AForSitsAfterEveryPrecedingRegion()
        {
            var component = new LoopComponent
            {
                Items = Two(),
                Words = new List<string> { "w" },
                Max = 1
            };

            CollectionAssert.AreEqual(
                new[]
                {
                    "loop_before",
                    "pair_name", "pair_count", "pair_name", "pair_count",
                    "loop_middle",
                    "word_row",
                    "indexed",
                    "loop_after"
                },
                Names(component),
                "its offset sums the actual element count of all three regions before it");
        }

        [TestMethod]
        public void ANullCollectionIsAnEmptyOne()
        {
            var component = new LoopComponent { Items = null };

            CollectionAssert.AreEqual(
                new[] { "loop_before", "loop_middle", "loop_after" },
                Names(component));
        }
    }
}
