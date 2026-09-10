using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class GridAreaGeometryTests : BaseGeometryTests
    {
        private static VisualElement Grid(float width, float height, string areas,
            string columns = null, string rows = null)
        {
            VisualElement grid = Element("grid", LayoutType.Grid,
                SizeUnit.Pixels, width, SizeUnit.Pixels, height);

            var template = new AreaTemplateStyleParser(areas);

            Assert.IsTrue(template.IsValid, $"'{areas}' should have parsed");

            grid.Styles.AreaTemplate = template.Descriptor;

            if (columns != null)
            {
                grid.Styles.RowTemplate = new RowTemplateStyleParser(columns).Descriptor;
            }

            if (rows != null)
            {
                grid.Styles.ColumnTemplate = new ColumnTemplateStyleParser(rows).Descriptor;
            }

            return grid;
        }

        private static VisualElement Cell(string name, string area = null)
        {
            VisualElement cell = Element(name, LayoutType.Column,
                SizeUnit.Unset, 1, SizeUnit.Unset, 1);

            if (area != null)
            {
                cell.Styles.GridArea = new GridAreaStyleParser(area).Descriptor;
            }

            return cell;
        }

        [TestMethod]
        public void ANamedCellLandsWhereTheTemplatePutIt()
        {
            VisualElement grid = Grid(300, 200, "head head / nav main", "100px 200px", "60px 140px");
            grid.AddChildren(Cell("m", "main"), Cell("h", "head"), Cell("n", "nav"));
            Layout(grid);

            AssertBox(grid.FindByName("h"), 0, 0, 300, 60);
            AssertBox(grid.FindByName("n"), 0, 60, 100, 140);
            AssertBox(grid.FindByName("m"), 100, 60, 200, 140);
        }

        [TestMethod]
        public void ANamedAreaCarriesItsOwnSpan()
        {
            VisualElement grid = Grid(300, 200, "head head / nav main", "100px 200px", "60px 140px");
            grid.AddChildren(Cell("h", "head"));
            Layout(grid);

            VisualElement head = grid.FindByName("h");

            Assert.AreEqual(0, head.GridColumn);
            Assert.AreEqual(0, head.GridRow);
            Assert.AreEqual(2, head.GridColumnSpan);
            Assert.AreEqual(1, head.GridRowSpan);
        }

        [TestMethod]
        public void AnAreaSpanningTwoRowsIsAsTallAsBoth()
        {
            VisualElement grid = Grid(300, 200, "side head / side main", "100px 200px", "60px 140px");
            grid.AddChildren(Cell("s", "side"));
            Layout(grid);

            AssertBox(grid.FindByName("s"), 0, 0, 100, 200);
        }

        [TestMethod]
        public void TheColumnCountComesFromTheAreasWhenNoTrackIsDeclared()
        {
            VisualElement grid = Grid(300, 200, "a b c", null, "200px");
            grid.AddChildren(Cell("x", "b"));
            Layout(grid);

            Assert.AreEqual(3, grid.GridColumns.Length);
            AssertBox(grid.FindByName("x"), 100, 0, 100, 200);
        }

        [TestMethod]
        public void TheRowCountComesFromTheAreasToo()
        {
            VisualElement grid = Grid(300, 300, "a / b / c");
            grid.AddChildren(Cell("x", "c"));
            Layout(grid);

            Assert.AreEqual(3, grid.GridRows.Length);
        }

        [TestMethod]
        public void AnAreaRowNobodyUsesStillExists()
        {
            VisualElement grid = Grid(200, 200, "head / foot", null, "100px 100px");
            grid.AddChildren(Cell("h", "head"));
            Layout(grid);

            Assert.AreEqual(2, grid.GridRows.Length,
                "the areas declare the explicit grid, whether or not a child lands in a row");

            AssertBox(grid.FindByName("h"), 0, 0, 200, 100);
        }

        [TestMethod]
        public void ADeclaredTrackListStillWinsOverTheAreaColumnCount()
        {
            VisualElement grid = Grid(300, 200, "a b c", "50px 100px 150px", "200px");
            grid.AddChildren(Cell("x", "c"));
            Layout(grid);

            Assert.AreEqual(3, grid.GridColumns.Length);
            AssertBox(grid.FindByName("x"), 150, 0, 150, 200);
        }

        [TestMethod]
        public void AnUnknownAreaNameFallsBackToTheFlow()
        {
            VisualElement grid = Grid(200, 100, "head head", "100px 100px", "100px");
            grid.AddChildren(Cell("a", "nowhere"), Cell("b", "nowhere"));
            Layout(grid);

            AssertBox(grid.FindByName("a"), 0, 0, 100, 100);
            AssertBox(grid.FindByName("b"), 100, 0, 100, 100);
        }

        [TestMethod]
        public void AChildWithNoAreaStillFlowsAroundThePlacedOnes()
        {
            VisualElement grid = Grid(200, 200, "head head / . .", "100px 100px", "100px 100px");
            grid.AddChildren(Cell("free"), Cell("h", "head"));
            Layout(grid);

            AssertBox(grid.FindByName("h"), 0, 0, 200, 100);
            AssertBox(grid.FindByName("free"), 0, 100, 100, 100);
        }

        [TestMethod]
        public void AnExplicitIndexIsIgnoredWhenAnAreaNamesTheCell()
        {
            VisualElement grid = Grid(200, 200, "head head / nav main", "100px 100px", "100px 100px");

            VisualElement cell = Cell("m", "main");
            cell.Styles.ColumnIndex = new ColumnIndexStyleParser("0").Descriptor;
            cell.Styles.RowIndex = new RowIndexStyleParser("0").Descriptor;

            grid.AddChildren(cell);
            Layout(grid);

            AssertBox(grid.FindByName("m"), 100, 100, 100, 100);
        }

        [TestMethod]
        public void AnAreaBeatsADeclaredSpanToo()
        {
            VisualElement grid = Grid(200, 200, "head head / nav main", "100px 100px", "100px 100px");

            VisualElement cell = Cell("n", "nav");
            cell.Styles.ColumnSpan = new ColumnSpanStyleParser("2").Descriptor;

            grid.AddChildren(cell);
            Layout(grid);

            Assert.AreEqual(1, grid.FindByName("n").GridColumnSpan);
        }

        [TestMethod]
        public void TwoChildrenMayShareOneArea()
        {
            VisualElement grid = Grid(200, 200, "head head / nav main", "100px 100px", "100px 100px");
            grid.AddChildren(Cell("a", "main"), Cell("b", "main"));
            Layout(grid);

            AssertBox(grid.FindByName("a"), 100, 100, 100, 100);
            AssertBox(grid.FindByName("b"), 100, 100, 100, 100);
        }

        [TestMethod]
        public void AnAreaTemplateOnANonGridChangesNothing()
        {
            VisualElement column = Element("column", LayoutType.Column,
                SizeUnit.Pixels, 200, SizeUnit.Pixels, 200);

            column.Styles.AreaTemplate = new AreaTemplateStyleParser("head / foot").Descriptor;
            column.AddChildren(Cell("a", "foot"), Cell("b", "head"));
            Layout(column);

            AssertBox(column.FindByName("a"), 0, 0, 200, 100);
            AssertBox(column.FindByName("b"), 0, 100, 200, 100);
        }

        [TestMethod]
        public void AStylesheetCanNameTheAreasToo()
        {
            var xns = new XnsSource(@"shell {
    layout: grid
    width: 200px
    height: 200px
    row-template: 100px 100px
    column-template: 100px 100px
    area-template: head head / nav main
}

cell { grid-area: main }");

            ClassesSet set = xns.Compile();

            Assert.IsFalse(xns.HasErrors, string.Join(" | ", xns.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();

            registry.Add(set);

            var shell = new VisualElement { Name = "shell" };

            shell.AddChild(new VisualElement { Name = "cell" });

            var viewport = new VisualElement { Name = "viewport" };

            viewport.AddChild(shell);

            var surface = new IxenSurface(viewport) { Styles = registry };

            surface.ComputeLayout(VIEWPORT_WIDTH, VIEWPORT_HEIGHT);

            AssertBox(shell.FindByName("cell"), 100, 100, 100, 100);
        }

        [TestMethod]
        public void AGapSeparatesNamedAreasLikeAnyOtherCell()
        {
            VisualElement grid = Grid(210, 200, "head head / nav main", "100px 100px", "100px 90px");
            grid.Styles.Gap = new GapStyleParser("10px 10px").Descriptor;
            grid.AddChildren(Cell("h", "head"), Cell("m", "main"));
            Layout(grid);

            AssertBox(grid.FindByName("h"), 0, 0, 210, 100);
            AssertBox(grid.FindByName("m"), 110, 110, 100, 90);
        }
    }
}
