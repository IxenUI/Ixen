using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class GridPlacementGeometryTests : BaseGeometryTests
    {
        private static VisualElement Grid(float width, float height)
            => Element("grid", LayoutType.Grid, SizeUnit.Pixels, width, SizeUnit.Pixels, height);

        private static VisualElement Cell(string name)
            => Element(name, LayoutType.Column, SizeUnit.Unset, 1, SizeUnit.Unset, 1);

        private static SizeStyleDescriptor Px(float value)
            => new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value };

        private static SizeStyleDescriptor Auto()
            => new SizeStyleDescriptor { Unit = SizeUnit.Content, Value = 1 };

        private static VisualElement WithColumnWidths(VisualElement element, params SizeStyleDescriptor[] tracks)
        {
            var descriptor = new RowTemplateStyleDescriptor();
            descriptor.Value.AddRange(tracks);
            element.Styles.RowTemplate = descriptor;
            return element;
        }

        private static VisualElement WithRowHeights(VisualElement element, params SizeStyleDescriptor[] tracks)
        {
            var descriptor = new ColumnTemplateStyleDescriptor();
            descriptor.Value.AddRange(tracks);
            element.Styles.ColumnTemplate = descriptor;
            return element;
        }

        private static VisualElement At(VisualElement element, int column, int row)
        {
            element.Styles.ColumnIndex = new ColumnIndexStyleDescriptor { Value = column };
            element.Styles.RowIndex = new RowIndexStyleDescriptor { Value = row };
            return element;
        }

        private static VisualElement Spanning(VisualElement element, int columns, int rows = 1)
        {
            element.Styles.ColumnSpan = new ColumnSpanStyleDescriptor { Value = columns };
            element.Styles.RowSpan = new RowSpanStyleDescriptor { Value = rows };
            return element;
        }

        [TestMethod]
        public void AnExplicitCellIsHonoured()
        {
            VisualElement grid = Grid(300, 200);
            WithColumnWidths(grid, Px(100), Px(200));
            WithRowHeights(grid, Px(50), Px(150));

            grid.AddChildren(At(Cell("a"), 1, 1));
            Layout(grid);

            AssertBox(grid.Children[0], 100, 50, 200, 150);
        }

        [TestMethod]
        public void AutoChildrenFlowAroundAnExplicitOne()
        {
            VisualElement grid = Grid(300, 100);
            WithColumnWidths(grid, Px(100), Px(100), Px(100));
            WithRowHeights(grid, Px(100));

            grid.AddChildren(At(Cell("pinned"), 1, 0), Cell("a"), Cell("b"));
            Layout(grid);

            AssertBox(grid.Children[0], 100, 0, 100, 100);
            AssertBox(grid.Children[1], 0, 0, 100, 100);
            AssertBox(grid.Children[2], 200, 0, 100, 100);
        }

        [TestMethod]
        public void AnExplicitRowAlonePicksTheFirstFreeColumn()
        {
            VisualElement grid = Grid(200, 200);
            WithColumnWidths(grid, Px(100), Px(100));
            WithRowHeights(grid, Px(100), Px(100));

            VisualElement second = Cell("second");
            second.Styles.RowIndex = new RowIndexStyleDescriptor { Value = 1 };

            VisualElement third = Cell("third");
            third.Styles.RowIndex = new RowIndexStyleDescriptor { Value = 1 };

            grid.AddChildren(second, third);
            Layout(grid);

            AssertBox(grid.Children[0], 0, 100, 100, 100);
            AssertBox(grid.Children[1], 100, 100, 100, 100);
        }

        [TestMethod]
        public void AColumnSpanTakesTheSumOfItsTracks()
        {
            VisualElement grid = Grid(300, 100);
            WithColumnWidths(grid, Px(100), Px(200));
            WithRowHeights(grid, Px(100));

            grid.AddChildren(Spanning(Cell("wide"), 2));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 300, 100);
        }

        [TestMethod]
        public void ARowSpanTakesTheSumOfItsTracks()
        {
            VisualElement grid = Grid(100, 200);
            WithColumnWidths(grid, Px(100));
            WithRowHeights(grid, Px(50), Px(150));

            grid.AddChildren(Spanning(Cell("tall"), 1, 2));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 100, 200);
        }

        [TestMethod]
        public void AutoFlowSkipsWhatASpanOccupies()
        {
            VisualElement grid = Grid(300, 200);
            WithColumnWidths(grid, Px(100), Px(100), Px(100));
            WithRowHeights(grid, Px(100), Px(100));

            grid.AddChildren(Spanning(Cell("wide"), 2), Cell("a"), Cell("b"));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 200, 100);
            AssertBox(grid.Children[1], 200, 0, 100, 100);
            AssertBox(grid.Children[2], 0, 100, 100, 100);
        }

        [TestMethod]
        public void ASpanWiderThanTheGridIsClamped()
        {
            VisualElement grid = Grid(200, 100);
            WithColumnWidths(grid, Px(100), Px(100));
            WithRowHeights(grid, Px(100));

            grid.AddChildren(Spanning(Cell("wide"), 5));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 200, 100);
        }

        [TestMethod]
        public void ARowSpanGrowsTheImplicitRowCount()
        {
            VisualElement grid = Grid(100, 300);
            WithColumnWidths(grid, Px(100));

            grid.AddChildren(Spanning(Cell("tall"), 1, 3));
            Layout(grid);

            Assert.AreEqual(3, grid.GridRows.Length,
                "the row count comes from where the children actually reach");
        }

        [TestMethod]
        public void ASpanningChildDoesNotSizeAnAutoTrack()
        {
            VisualElement grid = Grid(400, 100);
            WithColumnWidths(grid, Auto(), Auto());
            WithRowHeights(grid, Px(100));

            VisualElement narrow = Cell("narrow");
            narrow.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            VisualElement wide = Spanning(Cell("wide"), 2);
            wide.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            At(wide, 0, 0);

            grid.AddChildren(wide, At(narrow, 1, 0));
            Layout(grid);

            Assert.AreEqual(40f, grid.GridColumns[1],
                "an auto track sizes from its non-spanning children only");
        }

        [TestMethod]
        public void AnExplicitColumnBeyondTheLastIsClamped()
        {
            VisualElement grid = Grid(200, 100);
            WithColumnWidths(grid, Px(100), Px(100));
            WithRowHeights(grid, Px(100));

            grid.AddChildren(At(Cell("a"), 9, 0));
            Layout(grid);

            AssertBox(grid.Children[0], 100, 0, 100, 100);
        }
    }
}
