using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class ContentAlignGeometryTests : BaseGeometryTests
    {
        private static VisualElement Aligned(VisualElement element, ContentAlign horizontal,
            ContentVAlign vertical)
        {
            element.Styles.ContentAlign = new ContentAlignStyleDescriptor
            {
                Horizontal = horizontal,
                Vertical = vertical
            };

            return element;
        }

        private static VisualElement Box(string name, float width, float height)
            => Element(name,
                widthUnit: SizeUnit.Pixels, widthValue: width,
                heightUnit: SizeUnit.Pixels, heightValue: height);

        private static VisualElement Row(float width = 400, float height = 100)
            => Element("row", LayoutType.Row, SizeUnit.Pixels, width, SizeUnit.Pixels, height);

        private static VisualElement Column(float width = 100, float height = 400)
            => Element("column", LayoutType.Column, SizeUnit.Pixels, width, SizeUnit.Pixels, height);

        private static List<SizeStyleDescriptor> Tracks(params float[] pixels)
        {
            var sizes = new List<SizeStyleDescriptor>();

            foreach (float value in pixels)
            {
                sizes.Add(new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value });
            }

            return sizes;
        }

        [TestMethod]
        public void WithNothingDeclaredEverythingStaysWhereItWas()
        {
            VisualElement row = Row();
            VisualElement child = Box("child", 100, 40);

            row.AddChild(child);
            Layout(row);

            Assert.AreEqual(0f, child.X, "Unset means start, so no rule at all changes nothing");
            Assert.AreEqual(0f, child.Y);
        }

        [TestMethod]
        public void ARowCentresItsChildrenOnBothAxes()
        {
            VisualElement row = Aligned(Row(), ContentAlign.Center, ContentVAlign.Middle);
            VisualElement first = Box("first", 100, 40);
            VisualElement second = Box("second", 100, 40);

            row.AddChildren(first, second);
            Layout(row);

            Assert.AreEqual(100f, first.X, "400 wide holding 200 of children leaves 100 on each side");
            Assert.AreEqual(200f, second.X);
            Assert.AreEqual(30f, first.Y, "and 100 tall holding a 40 tall child leaves 30 above");
            Assert.AreEqual(30f, second.Y);
        }

        [TestMethod]
        public void TheMainAxisMovesTheGroupAndTheCrossAxisEachChild()
        {
            VisualElement row = Aligned(Row(), ContentAlign.Right, ContentVAlign.Bottom);
            VisualElement tall = Box("tall", 60, 80);
            VisualElement small = Box("small", 60, 20);

            row.AddChildren(tall, small);
            Layout(row);

            Assert.AreEqual(280f, tall.X, "the group is pushed to the end as one block");
            Assert.AreEqual(340f, small.X, "so the children keep their order and their spacing");

            Assert.AreEqual(20f, tall.Y, "while each child is placed on the cross axis by its own size");
            Assert.AreEqual(80f, small.Y);
        }

        [TestMethod]
        public void AColumnSwapsTheTwoRoles()
        {
            VisualElement column = Aligned(Column(), ContentAlign.Right, ContentVAlign.Middle);
            VisualElement first = Box("first", 40, 100);
            VisualElement second = Box("second", 60, 100);

            column.AddChildren(first, second);
            Layout(column);

            Assert.AreEqual(100f, first.Y, "400 tall holding 200 leaves 100 above the group");
            Assert.AreEqual(200f, second.Y);

            Assert.AreEqual(60f, first.X, "and each child is pushed right by its own width");
            Assert.AreEqual(40f, second.X);
        }

        [TestMethod]
        public void TheGapIsCountedInTheSlack()
        {
            VisualElement row = Aligned(Row(), ContentAlign.Center, ContentVAlign.Unset);
            row.Styles.Gap = new GapStyleDescriptor { Column = 20 };

            VisualElement first = Box("first", 100, 40);
            VisualElement second = Box("second", 100, 40);

            row.AddChildren(first, second);
            Layout(row);

            Assert.AreEqual(90f, first.X,
                "200 of children plus one 20 gap is 220, so 180 is left and 90 goes in front - "
                + "forgetting the gap would put the group 10 units too far left");
            Assert.AreEqual(210f, second.X);
        }

        [TestMethod]
        public void PaddingIsRespected()
        {
            VisualElement row = WithPadding(
                Aligned(Row(), ContentAlign.Center, ContentVAlign.Middle), 20);

            VisualElement child = Box("child", 100, 40);

            row.AddChild(child);
            Layout(row);

            Assert.AreEqual(150f, child.X,
                "the content box is 360 wide starting at 20, so the slack is 260 and half is 130 - "
                + "the alignment starts from the content origin, not from the element box");
            Assert.AreEqual(30f, child.Y, "and 60 tall of content starting at 20 leaves 10 above");
        }

        [TestMethod]
        public void AWeightedChildLeavesNoSlackSoNothingMoves()
        {
            VisualElement row = Aligned(Row(), ContentAlign.Center, ContentVAlign.Middle);

            VisualElement fixedWidth = Box("fixed", 100, 40);
            VisualElement filling = Element("filling",
                widthUnit: SizeUnit.Weight, widthValue: 1,
                heightUnit: SizeUnit.Pixels, heightValue: 40);

            row.AddChildren(fixedWidth, filling);
            Layout(row);

            Assert.AreEqual(0f, fixedWidth.X,
                "a 1* child eats the whole pool, so the main axis has nothing to distribute - "
                + "the rule falls out of the arithmetic rather than needing a special case");
            Assert.AreEqual(100f, filling.X);
            Assert.AreEqual(30f, fixedWidth.Y, "the cross axis still has room and still aligns");
        }

        [TestMethod]
        public void AnOverflowingRowIsNotPulledBackwards()
        {
            VisualElement row = Aligned(Row(200), ContentAlign.Center, ContentVAlign.Unset);

            VisualElement first = Box("first", 150, 40);
            VisualElement second = Box("second", 150, 40);

            row.AddChildren(first, second);
            Layout(row);

            Assert.AreEqual(0f, first.X,
                "the slack is negative, and moving by half of it would push the first child out "
                + "of the container on the near side where it can never be scrolled back into view");
            Assert.AreEqual(150f, second.X);
        }

        [TestMethod]
        public void AChildThatFillsTheCrossAxisDoesNotMove()
        {
            VisualElement row = Aligned(Row(), ContentAlign.Unset, ContentVAlign.Bottom);

            VisualElement filling = Element("filling",
                widthUnit: SizeUnit.Pixels, widthValue: 100,
                heightUnit: SizeUnit.Unset, heightValue: 1);

            row.AddChild(filling);
            Layout(row);

            Assert.AreEqual(100f, filling.ActualHeight, "Unset on the cross axis means fill");
            Assert.AreEqual(0f, filling.Y, "so there is no slack and the alignment has nothing to do");
        }

        [TestMethod]
        public void AGridAlignsEachChildInsideItsOwnCell()
        {
            VisualElement grid = Aligned(
                Element("grid", LayoutType.Grid, SizeUnit.Pixels, 400, SizeUnit.Pixels, 200),
                ContentAlign.Center, ContentVAlign.Middle);

            grid.Styles.RowTemplate = new RowTemplateStyleDescriptor { Value = Tracks(200, 200) };
            grid.Styles.ColumnTemplate = new ColumnTemplateStyleDescriptor { Value = Tracks(100) };

            VisualElement cell = Box("cell", 40, 20);

            grid.AddChild(cell);
            Layout(grid);

            Assert.AreEqual(80f, cell.X,
                "a 40 wide child in a 200 wide track is centred at 80 - a grid child smaller than "
                + "its cell used to sit at the cell top left, and this is what changes that");
            Assert.AreEqual(40f, cell.Y, "and 20 tall in a 100 tall track is centred at 40");
        }
    }
}
