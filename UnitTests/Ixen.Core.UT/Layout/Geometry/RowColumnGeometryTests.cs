using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class RowColumnGeometryTests : BaseGeometryTests
    {
        [TestMethod]
        public void Row_PositionsChildrenLeftToRightAtTheSameTop()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 200, SizeUnit.Pixels, 200);
            var first = Element("first", LayoutType.Column, SizeUnit.Pixels, 30, SizeUnit.Pixels, 30);
            var second = Element("second", LayoutType.Column, SizeUnit.Pixels, 40, SizeUnit.Pixels, 50);
            var third = Element("third", LayoutType.Column, SizeUnit.Pixels, 20, SizeUnit.Pixels, 10);
            box.AddChildren(first, second, third);

            Layout(box);

            AssertBox(first, 0, 0, 30, 30);
            AssertBox(second, 30, 0, 40, 50);
            AssertBox(third, 70, 0, 20, 10);
        }

        [TestMethod]
        public void Column_PositionsChildrenTopToBottomAtTheSameLeft()
        {
            var box = Element("box", LayoutType.Column, SizeUnit.Pixels, 200, SizeUnit.Pixels, 200);
            var first = Element("first", LayoutType.Column, SizeUnit.Pixels, 30, SizeUnit.Pixels, 30);
            var second = Element("second", LayoutType.Column, SizeUnit.Pixels, 40, SizeUnit.Pixels, 50);
            var third = Element("third", LayoutType.Column, SizeUnit.Pixels, 20, SizeUnit.Pixels, 10);
            box.AddChildren(first, second, third);

            Layout(box);

            AssertBox(first, 0, 0, 30, 30);
            AssertBox(second, 0, 30, 40, 50);
            AssertBox(third, 0, 80, 20, 10);
        }

        [TestMethod]
        public void NestedContainer_OffsetsItsChildrenByItsOwnPosition()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);
            var spacer = Element("spacer", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 50);
            var inner = Element("inner", LayoutType.Column, SizeUnit.Pixels, 100, SizeUnit.Pixels, 100);
            var innerFirst = Element("innerFirst", LayoutType.Column, SizeUnit.Pixels, 20, SizeUnit.Pixels, 20);
            var innerSecond = Element("innerSecond", LayoutType.Column, SizeUnit.Pixels, 20, SizeUnit.Pixels, 30);
            inner.AddChildren(innerFirst, innerSecond);
            box.AddChildren(spacer, inner);

            Layout(box);

            AssertBox(inner, 50, 0, 100, 100);
            AssertBox(innerFirst, 50, 0, 20, 20);
            AssertBox(innerSecond, 50, 20, 20, 30);
        }

        [TestMethod]
        public void Clip_IsIntersectionWithAncestors()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 100, SizeUnit.Pixels, 40);
            var wide = Element("wide", LayoutType.Column, SizeUnit.Pixels, 100, SizeUnit.Pixels, 100);
            box.AddChild(wide);

            Layout(box);

            Assert.AreEqual(0, wide.Clip.X, "wide.Clip.X");
            Assert.AreEqual(0, wide.Clip.Y, "wide.Clip.Y");
            Assert.AreEqual(100, wide.Clip.Width, "wide.Clip.Width");
            Assert.AreEqual(40, wide.Clip.Height, "wide.Clip.Height");
        }
    }
}
