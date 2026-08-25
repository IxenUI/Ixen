using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class GapGeometryTests : BaseGeometryTests
    {
        private static VisualElement WithGap(VisualElement element, float row, float column)
        {
            element.Styles.Gap = new GapStyleDescriptor { Row = row, Column = column };
            return element;
        }

        private static VisualElement Box(string name, float width = 0, float height = 0)
            => Element(name,
                widthUnit: width > 0 ? SizeUnit.Pixels : SizeUnit.Unset, widthValue: width > 0 ? width : 1,
                heightUnit: height > 0 ? SizeUnit.Pixels : SizeUnit.Unset, heightValue: height > 0 ? height : 1);

        private static List<SizeStyleDescriptor> Template(params float[] pixels)
        {
            var template = new List<SizeStyleDescriptor>();

            foreach (float value in pixels)
            {
                template.Add(new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value });
            }

            return template;
        }

        [TestMethod]
        public void ARowPushesItsChildrenApart()
        {
            VisualElement row = WithGap(
                Element("row", LayoutType.Row, SizeUnit.Pixels, 400, SizeUnit.Pixels, 50), 0, 20);

            VisualElement first = Box("first", 100, 30);
            VisualElement second = Box("second", 100, 30);

            row.AddChildren(first, second);
            Layout(row);

            Assert.AreEqual(0f, first.X);
            Assert.AreEqual(120f, second.X, "the second child starts one gap after the first ends");
        }

        [TestMethod]
        public void AColumnUsesTheOtherAxis()
        {
            VisualElement column = WithGap(
                Element("column", LayoutType.Column, SizeUnit.Pixels, 200, SizeUnit.Pixels, 400), 14, 0);

            VisualElement first = Box("first", 50, 40);
            VisualElement second = Box("second", 50, 40);

            column.AddChildren(first, second);
            Layout(column);

            Assert.AreEqual(0f, first.Y);
            Assert.AreEqual(54f, second.Y);
        }

        [TestMethod]
        public void ARowIgnoresTheRowGapAndAColumnTheColumnGap()
        {
            VisualElement row = WithGap(
                Element("row", LayoutType.Row, SizeUnit.Pixels, 400, SizeUnit.Pixels, 50), 40, 0);

            VisualElement first = Box("first", 100, 30);
            VisualElement second = Box("second", 100, 30);

            row.AddChildren(first, second);
            Layout(row);

            Assert.AreEqual(100f, second.X,
                "row-gap separates rows, so a row layout is spaced by column-gap only");
        }

        [TestMethod]
        public void OneValueSetsBothAxes()
        {
            var descriptor = new GapStyleDescriptor();

            Assert.IsFalse(descriptor.IsDeclared);

            VisualElement row = WithGap(
                Element("row", LayoutType.Row, SizeUnit.Pixels, 400, SizeUnit.Pixels, 50), 12, 12);

            row.AddChildren(Box("a", 50, 20), Box("b", 50, 20));
            Layout(row);

            Assert.AreEqual(62f, row.FindByName("b").X);
        }

        [TestMethod]
        public void NoGapIsAddedBeforeTheFirstOrAfterTheLast()
        {
            VisualElement row = WithGap(
                Element("row", LayoutType.Row, SizeUnit.Pixels, 400, SizeUnit.Pixels, 50), 0, 30);

            VisualElement only = Box("only", 100, 30);

            row.AddChild(only);
            Layout(row);

            Assert.AreEqual(0f, only.X, "a lone child is not pushed in");
        }

        [TestMethod]
        public void TheGapsComeOutOfTheWeightPool()
        {
            VisualElement row = WithGap(
                Element("row", LayoutType.Row, SizeUnit.Pixels, 400, SizeUnit.Pixels, 50), 0, 20);

            VisualElement first = Box("first", height: 30);
            VisualElement second = Box("second", height: 30);

            row.AddChildren(first, second);
            Layout(row);

            Assert.AreEqual(190f, first.Width, "400 less one 20 gap, halved");
            Assert.AreEqual(190f, second.Width);
            Assert.AreEqual(210f, second.X);
        }

        [TestMethod]
        public void ThreeChildrenTakeTwoGaps()
        {
            VisualElement row = WithGap(
                Element("row", LayoutType.Row, SizeUnit.Pixels, 400, SizeUnit.Pixels, 50), 0, 20);

            row.AddChildren(Box("a", height: 30), Box("b", height: 30), Box("c", height: 30));
            Layout(row);

            Assert.AreEqual(120f, row.FindByName("a").Width, "(400 - 40) / 3");
            Assert.AreEqual(140f, row.FindByName("b").X);
            Assert.AreEqual(280f, row.FindByName("c").X);
        }

        [TestMethod]
        public void AContentSizedRowGrowsByItsGaps()
        {
            VisualElement row = WithGap(
                Element("row", LayoutType.Row, SizeUnit.Content, heightUnit: SizeUnit.Pixels, heightValue: 50),
                0, 25);

            row.AddChildren(Box("a", 60, 30), Box("b", 60, 30));

            VisualElement holder = Element("holder", LayoutType.Column,
                SizeUnit.Pixels, 400, SizeUnit.Pixels, 200);

            holder.AddChild(row);
            Layout(holder);

            Assert.AreEqual(145f, row.Width, "60 + 25 + 60, so a ? container reserves the gap too");
        }

        [TestMethod]
        public void AGridSpacesItsTracks()
        {
            VisualElement grid = WithGap(
                Element("grid", LayoutType.Grid, SizeUnit.Pixels, 400, SizeUnit.Pixels, 300), 10, 20);

            grid.Styles.RowTemplate = new RowTemplateStyleDescriptor { Value = Template(100, 100) };
            grid.Styles.ColumnTemplate = new ColumnTemplateStyleDescriptor { Value = Template(40, 40) };

            grid.AddChildren(Box("a"), Box("b"), Box("c"), Box("d"));
            Layout(grid);

            Assert.AreEqual(0f, grid.FindByName("a").X);
            Assert.AreEqual(120f, grid.FindByName("b").X, "one column gap after the first track");
            Assert.AreEqual(0f, grid.FindByName("a").Y);
            Assert.AreEqual(50f, grid.FindByName("c").Y, "one row gap after the first row");
        }

        [TestMethod]
        public void AGridsWeightTracksLoseTheGapFirst()
        {
            VisualElement grid = WithGap(
                Element("grid", LayoutType.Grid, SizeUnit.Pixels, 400, SizeUnit.Pixels, 200), 0, 40);

            grid.Styles.RowTemplate = new RowTemplateStyleDescriptor
            {
                Value = new List<SizeStyleDescriptor>
                {
                    new SizeStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 },
                    new SizeStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 }
                }
            };

            grid.AddChildren(Box("a"), Box("b"));
            Layout(grid);

            Assert.AreEqual(180f, grid.FindByName("a").Width, "(400 - 40) / 2");
            Assert.AreEqual(220f, grid.FindByName("b").X);
        }

        [TestMethod]
        public void ASpanningCellCoversTheGapItStraddles()
        {
            VisualElement grid = WithGap(
                Element("grid", LayoutType.Grid, SizeUnit.Pixels, 400, SizeUnit.Pixels, 200), 0, 30);

            grid.Styles.RowTemplate = new RowTemplateStyleDescriptor { Value = Template(100, 100) };

            VisualElement wide = Box("wide");
            wide.Styles.ColumnSpan = new ColumnSpanStyleDescriptor { Value = 2 };

            grid.AddChild(wide);
            Layout(grid);

            Assert.AreEqual(230f, wide.Width,
                "100 + 30 + 100 - a span swallows the gap between the tracks it covers");
        }

        [TestMethod]
        public void AbsoluteAndDockIgnoreTheGap()
        {
            VisualElement dock = WithGap(
                Element("dock", LayoutType.Dock, SizeUnit.Pixels, 400, SizeUnit.Pixels, 200), 30, 30);

            VisualElement top = Box("top", height: 40);
            top.Styles.Dock = new DockStyleDescriptor { Side = DockSide.Top };

            VisualElement fill = Box("fill");
            fill.Styles.Dock = new DockStyleDescriptor { Side = DockSide.Fill };

            dock.AddChildren(top, fill);
            Layout(dock);

            Assert.AreEqual(40f, fill.Y,
                "docked bands are adjacent; a gap has no meaning there and is not applied");
        }

        [TestMethod]
        public void TheGapSurvivesARelayout()
        {
            VisualElement row = WithGap(
                Element("row", LayoutType.Row, SizeUnit.Pixels, 400, SizeUnit.Pixels, 50), 0, 20);

            row.AddChildren(Box("a", 100, 30), Box("b", 100, 30));
            Layout(row);

            Assert.AreEqual(120f, row.FindByName("b").X);

            row.InvalidateLayout();
            Layout(row);

            Assert.AreEqual(120f, row.FindByName("b").X, "the passes stay idempotent");
        }
    }
}
