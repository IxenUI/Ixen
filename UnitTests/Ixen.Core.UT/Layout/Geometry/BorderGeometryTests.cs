using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class BorderGeometryTests : BaseGeometryTests
    {
        private static VisualElement WithBorder(VisualElement element, float thickness, BorderType type)
        {
            element.Styles.Border = new BorderStyleDescriptor
            {
                Color = "#000000",
                Thickness = thickness,
                Type = type
            };

            return element;
        }

        private static VisualElement Box(string name, float width, float height)
            => Element(name, LayoutType.Column, SizeUnit.Pixels, width, SizeUnit.Pixels, height);

        [TestMethod]
        public void AnInnerBorderShrinksTheContentArea()
        {
            VisualElement box = WithBorder(Box("box", 200, 120), 10, BorderType.Inner);
            VisualElement child = Element("child");
            box.AddChild(child);

            Layout(box);

            AssertBox(box, 0, 0, 200, 120);
            AssertContentSize(box, 180, 100);
            AssertBox(child, 10, 10, 180, 100);
        }

        [TestMethod]
        public void AnInnerBorderCostsTheParentNothing()
        {
            VisualElement box = WithBorder(Box("box", 200, 120), 10, BorderType.Inner);
            Layout(box);

            AssertBoxSize(box, 200, 120);
        }

        [TestMethod]
        public void AnOuterBorderKeepsTheContentArea()
        {
            VisualElement box = WithBorder(Box("box", 200, 120), 10, BorderType.Outer);
            VisualElement child = Element("child");
            box.AddChild(child);

            Layout(box);

            AssertContentSize(box, 200, 120);
            AssertBox(child, 10, 10, 200, 120);
        }

        [TestMethod]
        public void AnOuterBorderCostsTheParentItsThickness()
        {
            VisualElement box = WithBorder(Box("box", 200, 120), 10, BorderType.Outer);
            Layout(box);

            AssertBoxSize(box, 220, 140);
            AssertBox(box, 10, 10, 200, 120);
        }

        [TestMethod]
        public void ACenterBorderSplitsTheThickness()
        {
            VisualElement box = WithBorder(Box("box", 200, 120), 10, BorderType.Center);
            VisualElement child = Element("child");
            box.AddChild(child);

            Layout(box);

            AssertContentSize(box, 190, 110);
            AssertBoxSize(box, 210, 130);
            AssertBox(box, 5, 5, 200, 120);
            AssertBox(child, 10, 10, 190, 110);
        }

        [TestMethod]
        public void ABorderStacksWithThePadding()
        {
            VisualElement box = WithPadding(WithBorder(Box("box", 200, 120), 10, BorderType.Inner), 20);
            VisualElement child = Element("child");
            box.AddChild(child);

            Layout(box);

            AssertContentSize(box, 140, 60);
            AssertBox(child, 30, 30, 140, 60);
        }

        [TestMethod]
        public void AContentSizedElementGrowsByItsInnerBorder()
        {
            VisualElement box = WithBorder(
                Element("box", LayoutType.Column, SizeUnit.Content, 1, SizeUnit.Content, 1),
                8, BorderType.Inner);

            box.AddChild(Box("child", 100, 40));

            var host = Element("host");
            host.AddChild(box);
            Layout(host);

            AssertBox(box, 0, 0, 116, 56);
            AssertContentSize(box, 100, 40);
        }

        [TestMethod]
        public void AContentSizedElementIgnoresItsOuterBorder()
        {
            VisualElement box = WithBorder(
                Element("box", LayoutType.Column, SizeUnit.Content, 1, SizeUnit.Content, 1),
                8, BorderType.Outer);

            box.AddChild(Box("child", 100, 40));

            var host = Element("host");
            host.AddChild(box);
            Layout(host);

            AssertBox(box, 8, 8, 100, 40);
            AssertBoxSize(box, 116, 56);
        }

        [TestMethod]
        public void OuterBordersComeOutOfTheWeightPool()
        {
            VisualElement row = Element("row", LayoutType.Row, SizeUnit.Pixels, 300, SizeUnit.Pixels, 60);

            row.AddChildren(
                WithBorder(Element("a"), 10, BorderType.Outer),
                WithBorder(Element("b"), 10, BorderType.Outer));

            Layout(row);

            AssertBox(row.Children[0], 10, 10, 130, 40);
            AssertBox(row.Children[1], 160, 10, 130, 40);
            AssertBoxSize(row.Children[0], 150, 60);
        }

        [TestMethod]
        public void AnElementWithoutABorderIsUnaffected()
        {
            VisualElement box = Box("box", 200, 120);
            VisualElement child = Element("child");
            box.AddChild(child);

            Layout(box);

            AssertContentSize(box, 200, 120);
            AssertBoxSize(box, 200, 120);
            AssertBox(child, 0, 0, 200, 120);
        }
    }
}
