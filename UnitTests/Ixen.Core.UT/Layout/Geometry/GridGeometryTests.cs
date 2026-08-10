using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class GridGeometryTests : BaseGeometryTests
    {
        private static VisualElement Grid(float width, SizeUnit heightUnit = SizeUnit.Content, float heightValue = 1)
            => Element("grid", LayoutType.Grid, SizeUnit.Pixels, width, heightUnit, heightValue);

        private static VisualElement Cell(string name, SizeUnit widthUnit = SizeUnit.Unset, float widthValue = 1,
            SizeUnit heightUnit = SizeUnit.Unset, float heightValue = 1)
            => Element(name, LayoutType.Column, widthUnit, widthValue, heightUnit, heightValue);

        private static VisualElement FixedCell(string name, float height)
            => Cell(name, SizeUnit.Unset, 1, SizeUnit.Pixels, height);

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

        private static SizeStyleDescriptor Px(float value)
            => new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value };

        private static SizeStyleDescriptor Star(float value)
            => new SizeStyleDescriptor { Unit = SizeUnit.Weight, Value = value };

        private static SizeStyleDescriptor Pct(float value)
            => new SizeStyleDescriptor { Unit = SizeUnit.Percents, Value = value };

        private static SizeStyleDescriptor Auto()
            => new SizeStyleDescriptor { Unit = SizeUnit.Content, Value = 1 };

        [TestMethod]
        public void FixedTracksPlaceChildrenInReadingOrder()
        {
            VisualElement grid = Grid(300, SizeUnit.Pixels, 200);
            WithColumnWidths(grid, Px(100), Px(200));
            WithRowHeights(grid, Px(50), Px(150));

            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"), Cell("d"));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 100, 50);
            AssertBox(grid.Children[1], 100, 0, 200, 50);
            AssertBox(grid.Children[2], 0, 50, 100, 150);
            AssertBox(grid.Children[3], 100, 50, 200, 150);
        }

        [TestMethod]
        public void WeightColumnsShareTheContentWidth()
        {
            VisualElement grid = Grid(300);
            WithColumnWidths(grid, Star(1), Star(2));
            WithRowHeights(grid, Px(100));

            grid.AddChildren(Cell("a"), Cell("b"));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 100, 100);
            AssertBox(grid.Children[1], 100, 0, 200, 100);
        }

        [TestMethod]
        public void AFixedColumnIsRemovedFromTheWeightPool()
        {
            VisualElement grid = Grid(300);
            WithColumnWidths(grid, Px(120), Star(1), Star(1));
            WithRowHeights(grid, Px(40));

            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 120, 40);
            AssertBox(grid.Children[1], 120, 0, 90, 40);
            AssertBox(grid.Children[2], 210, 0, 90, 40);
        }

        [TestMethod]
        public void PercentColumnsResolveAgainstTheContentWidth()
        {
            VisualElement grid = Grid(300);
            WithColumnWidths(grid, Pct(25), Pct(75));
            WithRowHeights(grid, Px(40));

            grid.AddChildren(Cell("a"), Cell("b"));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 75, 40);
            AssertBox(grid.Children[1], 75, 0, 225, 40);
        }

        [TestMethod]
        public void AMissingColumnTemplateGivesOneFullWidthColumn()
        {
            VisualElement grid = Grid(300);
            grid.AddChildren(FixedCell("a", 40), FixedCell("b", 40));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 300, 40);
            AssertBox(grid.Children[1], 0, 40, 300, 40);
        }

        [TestMethod]
        public void AMissingRowTemplateGivesRowsSizedToTheirTallestChild()
        {
            VisualElement grid = Grid(300);
            WithColumnWidths(grid, Star(1), Star(1));

            grid.AddChildren(FixedCell("a", 30), FixedCell("b", 70),
                FixedCell("c", 20), FixedCell("d", 50));

            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 150, 30);
            AssertBox(grid.Children[1], 150, 0, 150, 70);
            AssertBox(grid.Children[2], 0, 70, 150, 20);
            AssertBox(grid.Children[3], 150, 70, 150, 50);
        }

        [TestMethod]
        public void AContentSizedGridSumsItsTracks()
        {
            VisualElement grid = Grid(300);
            WithColumnWidths(grid, Star(1), Star(1));

            grid.AddChildren(FixedCell("a", 30), FixedCell("b", 70),
                FixedCell("c", 20), FixedCell("d", 50));

            Layout(grid);

            AssertBox(grid, 0, 0, 300, 120);
        }

        [TestMethod]
        public void AContentColumnTakesItsWidestChild()
        {
            VisualElement grid = Grid(300);
            WithColumnWidths(grid, Auto(), Star(1));
            WithRowHeights(grid, Px(50));

            grid.AddChildren(Cell("a", SizeUnit.Pixels, 80), Cell("b"));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 80, 50);
            AssertBox(grid.Children[1], 80, 0, 220, 50);
        }

        [TestMethod]
        public void AnExplicitChildSizeWinsOverItsTrack()
        {
            VisualElement grid = Grid(300);
            WithColumnWidths(grid, Px(100), Px(100));
            WithRowHeights(grid, Px(60));

            grid.AddChildren(Cell("a", SizeUnit.Pixels, 40), Cell("b"));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 40, 60);
            AssertBox(grid.Children[1], 100, 0, 100, 60);
        }

        [TestMethod]
        public void TheRowTemplateRepeatsDownTheRows()
        {
            VisualElement grid = Grid(300);
            WithRowHeights(grid, Px(30), Px(50));

            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"), Cell("d"));
            Layout(grid);

            AssertBox(grid.Children[0], 0, 0, 300, 30);
            AssertBox(grid.Children[1], 0, 30, 300, 50);
            AssertBox(grid.Children[2], 0, 80, 300, 30);
            AssertBox(grid.Children[3], 0, 110, 300, 50);
        }

        [TestMethod]
        public void ThePaddingOffsetsTheFirstCellAndShrinksTheTracks()
        {
            VisualElement grid = WithPadding(Grid(300), 10);
            grid.AddChildren(FixedCell("a", 40));
            Layout(grid);

            AssertBox(grid.Children[0], 10, 10, 280, 40);
            AssertBox(grid, 0, 0, 300, 60);
        }

        [TestMethod]
        public void AChildMarginComesOutOfItsOwnCell()
        {
            VisualElement grid = Grid(300);
            WithColumnWidths(grid, Px(100), Px(100));
            WithRowHeights(grid, Px(60));

            grid.AddChildren(WithMargin(Cell("a"), 10), Cell("b"));
            Layout(grid);

            AssertBox(grid.Children[0], 10, 10, 80, 40);
            AssertBox(grid.Children[1], 100, 0, 100, 60);
        }

        [TestMethod]
        public void AnEmptyGridDoesNotThrow()
        {
            VisualElement grid = Grid(300);
            Layout(grid);

            AssertBox(grid, 0, 0, 300, 0);
        }

        [TestMethod]
        public void ALastRowWithFewerChildrenLeavesTheRemainingCellsEmpty()
        {
            VisualElement grid = Grid(300);
            WithColumnWidths(grid, Star(1), Star(1));
            WithRowHeights(grid, Px(40));

            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"));
            Layout(grid);

            AssertBox(grid.Children[2], 0, 40, 150, 40);
            AssertBox(grid, 0, 0, 300, 80);
        }
    }
}
