using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class GridTemplateGeometryTests : BaseGeometryTests
    {
        private static VisualElement Grid(float width, string columnTemplate, float gap = 0)
        {
            VisualElement grid = Element("grid", LayoutType.Grid,
                SizeUnit.Pixels, width, SizeUnit.Pixels, 200);

            var template = new RowTemplateStyleParser(columnTemplate);

            Assert.IsTrue(template.IsValid, $"'{columnTemplate}' should have parsed");

            grid.Styles.RowTemplate = template.Descriptor;

            if (gap > 0)
            {
                grid.Styles.Gap = new GapStyleParser($"0px {gap}px").Descriptor;
            }

            return grid;
        }

        private static VisualElement Cell(string name, float width = 0)
            => width > 0
                ? Element(name, LayoutType.Column, SizeUnit.Pixels, width, SizeUnit.Pixels, 20)
                : Element(name, LayoutType.Column, SizeUnit.Unset, 1, SizeUnit.Pixels, 20);

        private static void AssertColumns(VisualElement grid, params float[] widths)
        {
            Assert.AreEqual(widths.Length, grid.GridColumns.Length, "column count");

            for (int index = 0; index < widths.Length; index++)
            {
                Assert.AreEqual(widths[index], grid.GridColumns[index], 0.01f,
                    $"column {index}");
            }
        }

        [TestMethod]
        public void ARepeatIsTheSameGridAsWritingEveryTrack()
        {
            VisualElement repeated = Grid(300, "repeat(3, 1*)");
            repeated.AddChildren(Cell("a"), Cell("b"), Cell("c"));
            Layout(repeated);

            VisualElement spelt = Grid(300, "1* 1* 1*");
            spelt.AddChildren(Cell("a"), Cell("b"), Cell("c"));
            Layout(spelt);

            AssertColumns(repeated, 100, 100, 100);
            AssertColumns(spelt, 100, 100, 100);
        }

        [TestMethod]
        public void AFloorIsTakenWhenTheShareWouldBeSmaller()
        {
            VisualElement grid = Grid(300, "repeat(3, minmax(160px, 1*))");
            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"));
            Layout(grid);

            AssertColumns(grid, 160, 160, 160);
        }

        [TestMethod]
        public void TheShareIsTakenWhenItIsLargerThanTheFloor()
        {
            VisualElement grid = Grid(600, "repeat(3, minmax(160px, 1*))");
            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"));
            Layout(grid);

            AssertColumns(grid, 200, 200, 200);
        }

        [TestMethod]
        public void TheFloorsAreReservedAndOnlyTheSurplusIsShared()
        {
            VisualElement grid = Grid(500, "100px minmax(300px, 1*) 1*");
            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"));
            Layout(grid);

            AssertColumns(grid, 100, 350, 50);

            Assert.AreEqual(500, grid.GridColumns[0] + grid.GridColumns[1] + grid.GridColumns[2], 0.01f,
                "reserving the floor first is what makes a mixed template fit exactly");
        }

        [TestMethod]
        public void FloorsThatDoNotFitOverflowRatherThanShrinking()
        {
            VisualElement grid = Grid(200, "repeat(3, minmax(160px, 1*))");
            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"));
            Layout(grid);

            AssertColumns(grid, 160, 160, 160);
        }

        [TestMethod]
        public void ADefiniteMaxCapsTheContent()
        {
            VisualElement grid = Grid(900, "minmax(0px, 300px) 1*");
            grid.AddChildren(Cell("wide", 500), Cell("rest"));
            Layout(grid);

            AssertColumns(grid, 300, 600);
        }

        [TestMethod]
        public void AContentFloorMakesTheCapUnreachable()
        {
            VisualElement grid = Grid(900, "minmax(?, 300px) 1*");
            grid.AddChildren(Cell("wide", 500), Cell("rest"));
            Layout(grid);

            AssertColumns(grid, 500, 400);

            Assert.AreEqual(500, grid.GridColumns[0], 0.01f,
                "Ixen has one content size and it is the max-content size, so asking for at "
                + "least the content and at most 300 is an inverted range whenever the content "
                + "is wider - use a definite floor to cap");
        }

        [TestMethod]
        public void ADefiniteMinFloorsTheContent()
        {
            VisualElement grid = Grid(900, "minmax(200px, ?) 1*");
            grid.AddChildren(Cell("narrow", 40), Cell("rest"));
            Layout(grid);

            AssertColumns(grid, 200, 700);
        }

        [TestMethod]
        public void TwoDefiniteBoundsClampTheContentBetweenThem()
        {
            VisualElement inside = Grid(900, "minmax(100px, 300px) 1*");
            inside.AddChildren(Cell("mid", 180), Cell("rest"));
            Layout(inside);

            VisualElement over = Grid(900, "minmax(100px, 300px) 1*");
            over.AddChildren(Cell("big", 500), Cell("rest"));
            Layout(over);

            VisualElement under = Grid(900, "minmax(100px, 300px) 1*");
            under.AddChildren(Cell("small", 20), Cell("rest"));
            Layout(under);

            AssertColumns(inside, 180, 720);
            AssertColumns(over, 300, 600);
            AssertColumns(under, 100, 800);
        }

        [TestMethod]
        public void AnInvertedRangeGivesTheMinimum()
        {
            VisualElement grid = Grid(900, "minmax(300px, 100px) 1*");
            grid.AddChildren(Cell("a", 40), Cell("rest"));
            Layout(grid);

            AssertColumns(grid, 300, 600);
        }

        [TestMethod]
        public void AFloorWorksOnTheRowHeights()
        {
            VisualElement grid = Element("grid", LayoutType.Grid,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 400);

            grid.Styles.RowTemplate = new RowTemplateStyleParser("1*").Descriptor;
            grid.Styles.ColumnTemplate = new ColumnTemplateStyleParser("minmax(120px, ?)").Descriptor;

            grid.AddChildren(Cell("a"), Cell("b"));
            Layout(grid);

            Assert.AreEqual(2, grid.GridRows.Length);
            Assert.AreEqual(120, grid.GridRows[0], 0.01f,
                "a 20px cell is floored at 120");
            Assert.AreEqual(120, grid.GridRows[1], 0.01f);
        }

        [TestMethod]
        public void AutoFillDerivesTheCountFromTheWidth()
        {
            VisualElement grid = Grid(700, "repeat(auto-fill, minmax(160px, 1*))");
            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"), Cell("d"), Cell("e"));
            Layout(grid);

            Assert.AreEqual(4, grid.GridColumns.Length,
                "four floors of 160 fit in 700 and five do not");

            AssertColumns(grid, 175, 175, 175, 175);
        }

        [TestMethod]
        public void AutoFillCountsTheGap()
        {
            VisualElement grid = Grid(660, "repeat(auto-fill, minmax(160px, 1*))", 20);
            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"), Cell("d"), Cell("e"));
            Layout(grid);

            Assert.AreEqual(3, grid.GridColumns.Length,
                "3 * 160 + 2 * 20 is 520 and a fourth would need 700, where ignoring the "
                + "gap entirely would have fitted four");

            AssertColumns(grid, 206.667f, 206.667f, 206.667f);
        }

        [TestMethod]
        public void AutoFillRederivesWhenTheContainerNarrows()
        {
            VisualElement wide = Grid(700, "repeat(auto-fill, minmax(160px, 1*))");
            wide.AddChildren(Cell("a"), Cell("b"), Cell("c"), Cell("d"));
            Layout(wide);

            VisualElement narrow = Grid(340, "repeat(auto-fill, minmax(160px, 1*))");
            narrow.AddChildren(Cell("a"), Cell("b"), Cell("c"), Cell("d"));
            Layout(narrow);

            Assert.AreEqual(4, wide.GridColumns.Length);
            Assert.AreEqual(2, narrow.GridColumns.Length,
                "the same declaration, and the container decides the count");
        }

        [TestMethod]
        public void AutoFillAlwaysGivesAtLeastOneTrack()
        {
            VisualElement grid = Grid(40, "repeat(auto-fill, minmax(160px, 1*))");
            grid.AddChildren(Cell("a"));
            Layout(grid);

            AssertColumns(grid, 160);
        }

        [TestMethod]
        public void AutoFillCyclesAGroupOfSeveralTracks()
        {
            VisualElement grid = Grid(500, "repeat(auto-fill, 100px 60px)");
            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"));
            Layout(grid);

            Assert.AreEqual(6, grid.GridColumns.Length,
                "100 + 60 three times is 480, and a seventh track would need 580");

            AssertColumns(grid, 100, 60, 100, 60, 100, 60);
        }

        [TestMethod]
        public void AutoFillIsBoundedRatherThanTrusted()
        {
            VisualElement grid = Grid(60000, "repeat(auto-fill, 1px)");
            grid.AddChildren(Cell("a"));
            Layout(grid);

            Assert.AreEqual(SizeTemplateStyleParser.MAX_TRACKS, grid.GridColumns.Length,
                "a floor small enough to fit tens of thousands of times stops at the cap");
        }

        [TestMethod]
        public void AutoFillPlacesTheChildrenAcrossTheDerivedColumns()
        {
            VisualElement grid = Grid(700, "repeat(auto-fill, minmax(160px, 1*))");
            grid.AddChildren(Cell("a"), Cell("b"), Cell("c"), Cell("d"), Cell("e"));
            Layout(grid);

            Assert.AreEqual(0, grid.Children[0].GridColumn);
            Assert.AreEqual(3, grid.Children[3].GridColumn);
            Assert.AreEqual(0, grid.Children[4].GridColumn);
            Assert.AreEqual(1, grid.Children[4].GridRow,
                "the fifth card wraps onto a second row of the derived grid");
        }
    }
}
