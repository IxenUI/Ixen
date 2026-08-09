using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class SpacingGeometryTests : BaseGeometryTests
    {
        [TestMethod]
        public void Margin_OffsetsTheElementAndPushesTheNextSibling()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 200, SizeUnit.Pixels, 200);
            var spaced = WithMargin(Element("spaced", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 50), 10);
            var next = Element("next", LayoutType.Column, SizeUnit.Pixels, 20, SizeUnit.Pixels, 20);
            box.AddChildren(spaced, next);

            Layout(box);

            AssertBox(spaced, 10, 10, 50, 50);
            AssertBoxSize(spaced, 70, 70);
            AssertBox(next, 70, 0, 20, 20);
        }

        [TestMethod]
        public void Padding_IsAppliedOnAllFourSidesOfAPixelSizedElement()
        {
            var box = WithPadding(Element("box", LayoutType.Column, SizeUnit.Pixels, 100, SizeUnit.Pixels, 100), 10);
            box.AddChild(Element("child", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 50));

            Layout(box);

            AssertPadding(box, 10, 10, 10, 10);
        }

        [TestMethod]
        public void Padding_IsAppliedOnWeightSizedElementsToo()
        {
            var box = WithPadding(Element("box", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 100), 10);
            box.AddChild(Element("child", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 50));

            Layout(box);

            AssertPadding(box, 10, 10, 10, 10);
        }

        [TestMethod]
        public void Padding_ReducesContentAreaAndKeepsTheDeclaredSize()
        {
            var box = WithPadding(Element("box", LayoutType.Column, SizeUnit.Pixels, 100, SizeUnit.Pixels, 100), 10);
            box.AddChild(Element("child", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 50));

            Layout(box);

            AssertBox(box, 0, 0, 100, 100);
            AssertActualSize(box, 100, 100);
            AssertContentSize(box, 80, 80);
            AssertBoxSize(box, 100, 100);
        }

        [TestMethod]
        public void Padding_IndentsChildrenByTheTopLeftPadding()
        {
            var box = WithPadding(Element("box", LayoutType.Column, SizeUnit.Pixels, 100, SizeUnit.Pixels, 100), 10);
            var child = Element("child", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 50);
            box.AddChild(child);

            Layout(box);

            AssertBox(child, 10, 10, 50, 50);
        }

        [TestMethod]
        public void Padding_IndentsChildrenInARowToo()
        {
            var box = WithPadding(Element("box", LayoutType.Row, SizeUnit.Pixels, 200, SizeUnit.Pixels, 100), 10);
            var first = Element("first", LayoutType.Column, SizeUnit.Pixels, 30, SizeUnit.Pixels, 30);
            var second = Element("second", LayoutType.Column, SizeUnit.Pixels, 40, SizeUnit.Pixels, 30);
            box.AddChildren(first, second);

            Layout(box);

            AssertBox(first, 10, 10, 30, 30);
            AssertBox(second, 40, 10, 40, 30);
        }

        [TestMethod]
        public void PercentChild_ResolvesAgainstTheContentAreaOfAPaddedBox()
        {
            var box = WithPadding(Element("box", LayoutType.Column, SizeUnit.Pixels, 100, SizeUnit.Pixels, 100), 10);
            var child = Element("child", LayoutType.Column, SizeUnit.Percents, 100, SizeUnit.Percents, 100);
            box.AddChild(child);

            Layout(box);

            AssertBox(box, 0, 0, 100, 100);
            AssertBox(child, 10, 10, 80, 80);
        }

        [TestMethod]
        public void WeightChild_ResolvesAgainstTheContentAreaOfAPaddedBox()
        {
            var box = WithPadding(Element("box", LayoutType.Row, SizeUnit.Pixels, 100, SizeUnit.Pixels, 100), 10);
            var child = Element("child", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 50);
            box.AddChild(child);

            Layout(box);

            AssertBox(box, 0, 0, 100, 100);
            AssertBox(child, 10, 10, 80, 50);
        }

        [TestMethod]
        public void MarginOnWeightSiblings_ComesOutOfTheSharedPool()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 200, SizeUnit.Pixels, 100);
            var spaced = WithMargin(Element("spaced", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 50), 10);
            var plain = Element("plain", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 50);
            box.AddChildren(spaced, plain);

            Layout(box);

            AssertBox(spaced, 10, 10, 90, 50);
            AssertBoxSize(spaced, 110, 70);
            AssertBox(plain, 110, 0, 90, 50);

            Assert.AreEqual(200, spaced.BoxWidth + plain.BoxWidth, "total consumed width");
        }

        [TestMethod]
        public void MarginOnCrossAxis_IsSubtractedFromEachChildIndependently()
        {
            var box = Element("box", LayoutType.Column, SizeUnit.Pixels, 200, SizeUnit.Pixels, 200);
            var first = WithMargin(Element("first", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 50), 10);
            var second = Element("second", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 50);
            box.AddChildren(first, second);

            Layout(box);

            AssertBox(first, 10, 10, 180, 50);
            AssertBox(second, 0, 70, 200, 50);
        }
    }
}
