using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class VirtualRegionHostTests
    {
        private const int VIEWPORT = 300;
        private const int LIST_HEIGHT = 200;
        private const float ROW = 20;

        private VisualElement _root;
        private VirtualList _list;
        private List<string> _items;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _list = new VirtualList { Name = "list", ItemHeight = ROW };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = LIST_HEIGHT };

            _root.AddChild(_list);

            _items = new List<string>();

            for (int i = 0; i < 1000; i++)
            {
                _items.Add($"row {i}");
            }

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private sealed class WordRow : IRegionRow
        {
            internal readonly VisualElement Label = new VisualElement { Name = "word_row" };

            public int ElementCount => 1;

            public VisualElement ElementAt(int index) => Label;
        }

        private sealed class PairRow : IRegionRow
        {
            private readonly VisualElement _first = new VisualElement();
            private readonly VisualElement _second = new VisualElement();

            public int ElementCount => 2;

            public VisualElement ElementAt(int index) => index == 0 ? _first : _second;
        }

        private void Hand()
            => _list.SetRegion(
                () => _items.Count,
                () => new WordRow(),
                (row, index) => ((WordRow)row).Label.Text = _items[index]);

        [TestMethod]
        public void AListIsARegionHost()
        {
            Assert.IsInstanceOfType(_list, typeof(IRegionHost));
        }

        [TestMethod]
        public void TheRowsAreTheOnesTheFactoryBuilt()
        {
            Hand();
            Layout();

            Assert.IsTrue(_list.RealisedCount >= 10);

            foreach (VisualElement row in _list.RealisedRows)
            {
                Assert.AreEqual("word_row", row.Name);
            }
        }

        [TestMethod]
        public void TheBinderFillsEachRealisedRowWithItsOwnItem()
        {
            Hand();
            Layout();

            int index = _list.FirstRealised;

            foreach (VisualElement row in _list.RealisedRows)
            {
                Assert.AreEqual(_items[index], row.Text);
                index++;
            }
        }

        [TestMethod]
        public void OnlyTheVisibleWindowIsEverBuilt()
        {
            int created = 0;

            _list.SetRegion(
                () => _items.Count,
                () => { created++; return new WordRow(); },
                (row, index) => ((WordRow)row).Label.Text = _items[index]);

            Layout();

            Assert.IsTrue(created < 40, $"1000 items and {created} elements.");
        }

        [TestMethod]
        public void TheCountIsAskedEveryPassRatherThanCapturedOnce()
        {
            Hand();
            Layout();

            Assert.AreEqual(1000, _list.Count);

            _items.Add("one more");

            Assert.AreEqual(1001, _list.Count);
        }

        [TestMethod]
        public void ARowThatIsNotOneElementIsRefused()
        {
            _list.SetRegion(() => _items.Count, () => new PairRow(), (row, index) => { });

            Assert.ThrowsExactly<InvalidOperationException>(() => Layout());
        }

        [TestMethod]
        public void SetItemsGoesThroughTheSameRoute()
        {
            _list.SetItems(_items,
                () => new VisualElement { Name = "word_row" },
                (row, index) => row.Text = _items[index]);

            Layout();

            Assert.IsTrue(_list.RealisedCount >= 10);

            int index = _list.FirstRealised;

            foreach (VisualElement row in _list.RealisedRows)
            {
                Assert.AreEqual("word_row", row.Name);
                Assert.AreEqual(_items[index], row.Text);
                index++;
            }
        }
    }
}
