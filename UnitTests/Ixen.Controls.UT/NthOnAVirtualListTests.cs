using Ixen.Core;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class NthOnAVirtualListTests
    {
        private const int VIEWPORT = 300;
        private const float ROW = 20;
        private const string STRIPE = "#4C6EF5";

        private VirtualList _list;
        private List<string> _items;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            var registry = new StyleRegistry();

            registry.Add(new XnsSource("row:nth-child(2n) { background: " + STRIPE + " }").Compile());

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _list = new VirtualList { Name = "list", ItemHeight = ROW };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            root.AddChild(_list);

            _items = new List<string>();

            for (int index = 0; index < 500; index++)
            {
                _items.Add("item " + index);
            }

            _list.SetItems(_items, () => new VisualElement { Name = "row" },
                (row, index) => row.Text = _items[index]);

            _surface = new IxenSurface(root) { Styles = registry };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private VisualElement Slot(int slot) => _list.ChildElements[slot];

        private static string BackgroundOf(VisualElement element)
            => element.StylesHandlers.Background.Descriptor?.Color;

        [TestMethod]
        public void TheSpacerTakesTheFirstChildSlot()
        {
            Assert.IsNull(Slot(0).Name,
                "the spacer that carries the full scroll height is an ordinary child, and it is "
                + "the FIRST one - so nth-child counts it");
            Assert.AreEqual("item 0", Slot(1).Text);
            Assert.AreEqual(STRIPE, BackgroundOf(Slot(1)),
                "item 0 sits at child position 2 because the spacer holds position 1, so "
                + "nth-child(2n) stripes the EVEN-numbered items rather than the odd ones - off "
                + "by one from what an author writing 2n means, and silently");
        }

        [TestMethod]
        public void TheStripeBelongsToTheSlotRatherThanToTheItem()
        {
            Assert.AreEqual("item 0", Slot(1).Text);
            Assert.AreEqual(STRIPE, BackgroundOf(Slot(1)));

            _list.ScrollY = 9 * ROW;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("item 8", Slot(1).Text, "nine rows further down");
            Assert.AreEqual(STRIPE, BackgroundOf(Slot(1)),
                "the same slot, so the same stripe - item 8 now carries the band item 9 carried "
                + "one frame ago. Scrolling an ODD number of rows flips every stripe, because a "
                + "structural pseudo-class is a position in the tree and a recycled slot keeps "
                + "its position while the items flow through it. That is the same shape as the "
                + "hover that walked down a list, and it is why nth-child belongs on a @foreach "
                + "rather than on a virtual one.");
        }
    }
}
