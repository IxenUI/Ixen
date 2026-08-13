using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class DockGeometryTests : BaseGeometryTests
    {
        private static VisualElement Shell()
            => Element("shell", LayoutType.Dock, SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

        private static VisualElement Docked(string name, DockSide side,
            SizeUnit widthUnit = SizeUnit.Unset, float widthValue = 1,
            SizeUnit heightUnit = SizeUnit.Unset, float heightValue = 1)
        {
            VisualElement element = Element(name, LayoutType.Column, widthUnit, widthValue, heightUnit, heightValue);
            element.Styles.Dock = new DockStyleDescriptor { Side = side };
            return element;
        }

        [TestMethod]
        public void TheFourSidesCarveTheContainerInOrder()
        {
            VisualElement shell = Shell();

            VisualElement header = Docked("header", DockSide.Top, heightUnit: SizeUnit.Pixels, heightValue: 30);
            VisualElement footer = Docked("footer", DockSide.Bottom, heightUnit: SizeUnit.Pixels, heightValue: 20);
            VisualElement sidebar = Docked("sidebar", DockSide.Left, widthUnit: SizeUnit.Pixels, widthValue: 60);
            VisualElement body = Docked("body", DockSide.Fill);

            shell.AddChildren(header, footer, sidebar, body);

            Layout(shell);

            AssertBox(header, 0, 0, 300, 30);
            AssertBox(footer, 0, 180, 300, 20);
            AssertBox(sidebar, 0, 30, 60, 150);
            AssertBox(body, 60, 30, 240, 150);
        }

        [TestMethod]
        public void DeclarationOrderDecidesWhoGetsTheCorner()
        {
            VisualElement shell = Shell();

            VisualElement sidebar = Docked("sidebar", DockSide.Left, widthUnit: SizeUnit.Pixels, widthValue: 60);
            VisualElement header = Docked("header", DockSide.Top, heightUnit: SizeUnit.Pixels, heightValue: 30);

            shell.AddChildren(sidebar, header);

            Layout(shell);

            AssertBox(sidebar, 0, 0, 60, 200);
            AssertBox(header, 60, 0, 240, 30);
        }

        [TestMethod]
        public void RightDocksFromTheOppositeEdge()
        {
            VisualElement shell = Shell();

            VisualElement aside = Docked("aside", DockSide.Right, widthUnit: SizeUnit.Pixels, widthValue: 80);
            VisualElement body = Docked("body", DockSide.Fill);

            shell.AddChildren(aside, body);

            Layout(shell);

            AssertBox(aside, 220, 0, 80, 200);
            AssertBox(body, 0, 0, 220, 200);
        }

        [TestMethod]
        public void TwoDocksOnTheSameSideStack()
        {
            VisualElement shell = Shell();

            VisualElement first = Docked("first", DockSide.Top, heightUnit: SizeUnit.Pixels, heightValue: 30);
            VisualElement second = Docked("second", DockSide.Top, heightUnit: SizeUnit.Pixels, heightValue: 20);

            shell.AddChildren(first, second);

            Layout(shell);

            AssertBox(first, 0, 0, 300, 30);
            AssertBox(second, 0, 30, 300, 20);
        }

        [TestMethod]
        public void AFillingChildConsumesWhatIsLeft()
        {
            VisualElement shell = Shell();

            VisualElement body = Docked("body", DockSide.Fill);
            VisualElement late = Docked("late", DockSide.Top, heightUnit: SizeUnit.Pixels, heightValue: 30);

            shell.AddChildren(body, late);

            Layout(shell);

            AssertBox(body, 0, 0, 300, 200);
            AssertBox(late, 300, 200, 0, 30);
            Assert.IsTrue(late.Clip.IsVoidOrInvalid, "nothing is left for it, so it is not even drawn");
        }

        [TestMethod]
        public void AChildWithNoDockFills()
        {
            VisualElement shell = Shell();

            VisualElement header = Docked("header", DockSide.Top, heightUnit: SizeUnit.Pixels, heightValue: 30);
            VisualElement body = Element("body");

            shell.AddChildren(header, body);

            Layout(shell);

            AssertBox(body, 0, 30, 300, 170);
        }

        [TestMethod]
        public void APercentageResolvesAgainstTheContainerNotTheBand()
        {
            VisualElement shell = Shell();

            VisualElement header = Docked("header", DockSide.Top, heightUnit: SizeUnit.Pixels, heightValue: 100);
            VisualElement half = Docked("half", DockSide.Top, heightUnit: SizeUnit.Percents, heightValue: 50);

            shell.AddChildren(header, half);

            Layout(shell);

            AssertBox(half, 0, 100, 300, 100);
        }

        [TestMethod]
        public void AContentSizedDockTakesItsChildren()
        {
            VisualElement shell = Shell();

            VisualElement header = Docked("header", DockSide.Top, heightUnit: SizeUnit.Content);
            header.AddChild(Element("inner", LayoutType.Column, SizeUnit.Pixels, 40, SizeUnit.Pixels, 25));

            VisualElement body = Docked("body", DockSide.Fill);

            shell.AddChildren(header, body);

            Layout(shell);

            AssertBox(header, 0, 0, 300, 25);
            AssertBox(body, 0, 25, 300, 175);
        }

        [TestMethod]
        public void PaddingAndMarginBothCount()
        {
            VisualElement shell = WithPadding(Shell(), 10);

            VisualElement header = WithMargin(
                Docked("header", DockSide.Top, heightUnit: SizeUnit.Pixels, heightValue: 30), 5);

            VisualElement body = Docked("body", DockSide.Fill);

            shell.AddChildren(header, body);

            Layout(shell);

            AssertBox(header, 15, 15, 270, 30);
            AssertBox(body, 10, 50, 280, 140);
        }

        [TestMethod]
        public void AContentSizedDockContainerTakesItsPlacedExtent()
        {
            VisualElement shell = Element("shell", LayoutType.Dock, SizeUnit.Content, 0, SizeUnit.Content, 0);

            VisualElement header = Docked("header", DockSide.Top,
                SizeUnit.Pixels, 120, SizeUnit.Pixels, 30);

            shell.AddChild(header);

            Layout(shell);

            AssertBox(shell, 0, 0, 120, 30);
        }
    }
}
