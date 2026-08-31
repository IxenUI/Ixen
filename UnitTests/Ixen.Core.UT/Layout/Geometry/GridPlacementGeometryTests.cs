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

        private static SizeStyleDescriptor Fill()
            => new SizeStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };

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
        public void ASpanningChildGrowsTheAutoTracksItCrosses()
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

            Assert.AreEqual(300f, grid.GridColumns[0] + grid.GridColumns[1], 0.01f,
                "the spanning child no longer overflows: what it needs beyond the tracks it "
                + "crosses is distributed over the auto ones");

            Assert.AreEqual(130f, grid.GridColumns[0], 0.01f,
                "the excess is 300 - (0 + 40), shared equally between the two auto tracks");

            Assert.AreEqual(170f, grid.GridColumns[1], 0.01f,
                "and the track that already had 40 keeps it on top of its share");
        }

        [TestMethod]
        public void ASpanThatFitsChangesNothing()
        {
            VisualElement grid = Grid(400, 100);
            WithColumnWidths(grid, Auto(), Auto());
            WithRowHeights(grid, Px(100));

            VisualElement narrow = Cell("narrow");
            narrow.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 90 };

            VisualElement small = Spanning(Cell("small"), 2);
            small.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            At(small, 0, 0);

            grid.AddChildren(At(narrow, 1, 0), small);
            Layout(grid);

            Assert.AreEqual(90f, grid.GridColumns[1], 0.01f,
                "a spanning child that already fits asks for nothing, so the tracks keep the "
                + "sizes their own children gave them");
        }

        [TestMethod]
        public void OnlyTheAutoTracksGrow()
        {
            VisualElement grid = Grid(400, 100);
            WithColumnWidths(grid, Px(50), Auto());
            WithRowHeights(grid, Px(100));

            VisualElement wide = Spanning(Cell("wide"), 2);
            wide.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            At(wide, 0, 0);

            grid.AddChild(wide);
            Layout(grid);

            Assert.AreEqual(50f, grid.GridColumns[0], 0.01f, "a pixel track is not intrinsic");

            Assert.AreEqual(150f, grid.GridColumns[1], 0.01f,
                "so the whole excess lands on the one track that can absorb it");
        }

        [TestMethod]
        public void ASpanOverNoAutoTrackIsLeftAlone()
        {
            VisualElement grid = Grid(400, 100);
            WithColumnWidths(grid, Px(20), Px(20));
            WithRowHeights(grid, Px(100));

            VisualElement wide = Spanning(Cell("wide"), 2);
            wide.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            At(wide, 0, 0);

            grid.AddChild(wide);
            Layout(grid);

            Assert.AreEqual(20f, grid.GridColumns[0], 0.01f);
            Assert.AreEqual(20f, grid.GridColumns[1], 0.01f,
                "the author asked for two fixed tracks, so the child overflows rather than "
                + "the tracks quietly disobeying");
        }

        [TestMethod]
        public void TheGapCountsAsRoomTheSpanAlreadyHas()
        {
            VisualElement grid = Grid(400, 100);
            WithColumnWidths(grid, Auto(), Auto());
            WithRowHeights(grid, Px(100));

            grid.Styles.Gap = new GapStyleDescriptor { Column = 20 };

            VisualElement wide = Spanning(Cell("wide"), 2);
            wide.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            At(wide, 0, 0);

            grid.AddChild(wide);
            Layout(grid);

            Assert.AreEqual(40f, grid.GridColumns[0], 0.01f,
                "the 20px gap is part of what the child spans, so only 80 has to come from "
                + "the tracks themselves");
        }

        [TestMethod]
        public void ARowSpanGrowsTheAutoRows()
        {
            VisualElement grid = Grid(200, 400);
            WithColumnWidths(grid, Px(200));
            WithRowHeights(grid, Auto(), Auto());

            VisualElement tall = Spanning(Cell("tall"), 1, 2);
            tall.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 180 };
            At(tall, 0, 0);

            grid.AddChild(tall);
            Layout(grid);

            Assert.AreEqual(90f, grid.GridRows[0], 0.01f, "the same rule on the other axis");
            Assert.AreEqual(90f, grid.GridRows[1], 0.01f);
        }

        [TestMethod]
        public void ASpanOverNoAutoTrackDoesNotEatTheWeightedPool()
        {
            VisualElement grid = Grid(400, 100);
            WithColumnWidths(grid, Px(20), Px(20), Fill());
            WithRowHeights(grid, Px(100));

            VisualElement wide = Spanning(Cell("wide"), 2);
            wide.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            At(wide, 0, 0);

            grid.AddChild(wide);
            Layout(grid);

            Assert.AreEqual(360f, grid.GridColumns[2], 0.01f,
                "nothing could absorb the excess, so nothing was taken: reporting it anyway would "
                + "shrink the weighted track by room no fixed track ever gained");
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
