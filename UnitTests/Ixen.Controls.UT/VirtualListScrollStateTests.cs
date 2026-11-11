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
    public class VirtualListScrollStateTests
    {
        private const int VIEWPORT = 400;
        private const float ROW = 20;

        private static IxenSurface Build(out VirtualList list)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            List<string> items = Enumerable.Range(0, 1000).Select(i => "track " + i).ToList();

            list = new VirtualList { Name = "tracks", ItemHeight = ROW, PersistScroll = true };
            list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            list.SetItems(items, () => new VisualElement(), (row, index) => row.Text = items[index]);

            root.AddChild(list);

            return new IxenSurface(root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };
        }

        [TestMethod]
        public void ARestoredListBuildsTheRowsAtItsPlaceInTheFirstLayout()
        {
            IxenSurface first = Build(out VirtualList before);
            first.ComputeLayout(VIEWPORT, VIEWPORT);

            before.ScrollY = 5000;
            first.ComputeLayout(VIEWPORT, VIEWPORT);

            string saved = first.SaveState();

            IxenSurface next = Build(out VirtualList list);

            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(5000, list.ScrollY, 0.01f);
            Assert.AreEqual(249, list.FirstRealised,
                "row 250 is at the top, less one of overscan: the offset moved during the measure, "
                + "and the prepass ran again to build the window there rather than at row 0");
        }

        [TestMethod]
        public void SoDoesAListThatWasAlreadyLaidOut()
        {
            IxenSurface first = Build(out VirtualList before);
            first.ComputeLayout(VIEWPORT, VIEWPORT);

            before.ScrollY = 5000;
            first.ComputeLayout(VIEWPORT, VIEWPORT);

            string saved = first.SaveState();

            IxenSurface next = Build(out VirtualList list);

            next.ComputeLayout(VIEWPORT, VIEWPORT);
            next.RestoreState(saved);
            next.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(249, list.FirstRealised,
                "its size no longer changes, so only the offset moving during the measure asks for "
                + "the second prepass");
        }
    }
}
