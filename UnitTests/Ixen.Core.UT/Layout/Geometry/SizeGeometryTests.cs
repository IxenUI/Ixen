using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class SizeGeometryTests : BaseGeometryTests
    {
        [TestMethod]
        public void PixelSize_IsUsedVerbatim()
        {
            var box = Element("box", LayoutType.Column, SizeUnit.Pixels, 100, SizeUnit.Pixels, 80);

            Layout(box);

            AssertBox(box, 0, 0, 100, 80);
        }

        [TestMethod]
        public void PercentSize_ResolvesAgainstContainerActualSize()
        {
            var box = Element("box", LayoutType.Column, SizeUnit.Pixels, 200, SizeUnit.Pixels, 200);
            var child = Element("child", LayoutType.Column, SizeUnit.Percents, 50, SizeUnit.Percents, 25);
            box.AddChild(child);

            Layout(box);

            AssertBox(child, 0, 0, 100, 50);
        }

        [TestMethod]
        public void WeightSize_SplitsSpaceLeftAfterFixedSiblings()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 200, SizeUnit.Pixels, 100);
            var fixed50 = Element("fixed50", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 10);
            var weight1 = Element("weight1", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 10);
            var weight2 = Element("weight2", LayoutType.Column, SizeUnit.Weight, 2, SizeUnit.Pixels, 10);
            box.AddChildren(fixed50, weight1, weight2);

            Layout(box);

            AssertBox(fixed50, 0, 0, 50, 10);
            AssertBox(weight1, 50, 0, 50, 10);
            AssertBox(weight2, 100, 0, 100, 10);
        }

        [TestMethod]
        public void UnsetSize_BehavesLikeWeightOne()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 200, SizeUnit.Pixels, 100);
            var unset = Element("unset", LayoutType.Column, SizeUnit.Unset, 1, SizeUnit.Pixels, 10);
            var weight1 = Element("weight1", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 10);
            box.AddChildren(unset, weight1);

            Layout(box);

            AssertBox(unset, 0, 0, 100, 10);
            AssertBox(weight1, 100, 0, 100, 10);
        }

        [TestMethod]
        public void ContentSize_InColumn_TakesWidestChildAndSumOfHeights()
        {
            var box = Element("box", LayoutType.Column, SizeUnit.Content, 0, SizeUnit.Content, 0);
            var first = Element("first", LayoutType.Column, SizeUnit.Pixels, 30, SizeUnit.Pixels, 40);
            var second = Element("second", LayoutType.Column, SizeUnit.Pixels, 70, SizeUnit.Pixels, 10);
            box.AddChildren(first, second);

            Layout(box);

            AssertBox(box, 0, 0, 70, 50);
            AssertBox(first, 0, 0, 30, 40);
            AssertBox(second, 0, 40, 70, 10);
        }

        [TestMethod]
        public void ContentSize_InRow_TakesSumOfWidthsAndTallestChild()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Content, 0, SizeUnit.Content, 0);
            var first = Element("first", LayoutType.Column, SizeUnit.Pixels, 30, SizeUnit.Pixels, 40);
            var second = Element("second", LayoutType.Column, SizeUnit.Pixels, 70, SizeUnit.Pixels, 10);
            box.AddChildren(first, second);

            Layout(box);

            AssertBox(box, 0, 0, 100, 40);
            AssertBox(first, 0, 0, 30, 40);
            AssertBox(second, 30, 0, 70, 10);
        }

        [TestMethod]
        public void FixedSize_OverflowsTheContainerInsteadOfBeingClamped()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 100, SizeUnit.Pixels, 100);
            var first = Element("first", LayoutType.Column, SizeUnit.Pixels, 80, SizeUnit.Pixels, 50);
            var second = Element("second", LayoutType.Column, SizeUnit.Pixels, 80, SizeUnit.Pixels, 50);
            box.AddChildren(first, second);

            Layout(box);

            AssertBox(first, 0, 0, 80, 50);
            AssertBox(second, 80, 0, 80, 50);
        }

        [TestMethod]
        public void OverflowingChild_IsClippedToTheContainer()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 100, SizeUnit.Pixels, 100);
            var first = Element("first", LayoutType.Column, SizeUnit.Pixels, 80, SizeUnit.Pixels, 50);
            var second = Element("second", LayoutType.Column, SizeUnit.Pixels, 80, SizeUnit.Pixels, 50);
            box.AddChildren(first, second);

            Layout(box);

            Assert.AreEqual(80, second.Clip.X, "second.Clip.X");
            Assert.AreEqual(20, second.Clip.Width, "second.Clip.Width");
        }

        [TestMethod]
        public void RootIsForcedToViewportSizeAndIgnoresItsOwnSizeStyles()
        {
            var root = Element("root", LayoutType.Column, SizeUnit.Pixels, 10, SizeUnit.Pixels, 10);

            var surface = new IxenSurface(root);
            surface.ComputeLayout(640, 480);

            AssertBox(root, 0, 0, 640, 480);
        }

        [TestMethod]
        public void ContentSizedSiblings_AreLaidOutSequentiallyWithoutOverlapping()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 300, SizeUnit.Pixels, 100);
            var firstAuto = Element("firstAuto", LayoutType.Row, SizeUnit.Content, 0, SizeUnit.Pixels, 50);
            firstAuto.AddChild(Element("fa1", LayoutType.Column, SizeUnit.Pixels, 40, SizeUnit.Pixels, 20));
            var secondAuto = Element("secondAuto", LayoutType.Row, SizeUnit.Content, 0, SizeUnit.Pixels, 50);
            secondAuto.AddChild(Element("sa1", LayoutType.Column, SizeUnit.Pixels, 30, SizeUnit.Pixels, 20));
            box.AddChildren(firstAuto, secondAuto);

            Layout(box);

            AssertBox(firstAuto, 0, 0, 40, 50);
            AssertBox(secondAuto, 40, 0, 30, 50);
        }

        [TestMethod]
        public void ContentSizedElement_ShrinksToItsChildrenEvenWhenWeightSiblingsArePresent()
        {
            var box = Element("box", LayoutType.Row, SizeUnit.Pixels, 200, SizeUnit.Pixels, 100);
            var auto = Element("auto", LayoutType.Row, SizeUnit.Content, 0, SizeUnit.Pixels, 50);
            auto.AddChild(Element("inner", LayoutType.Column, SizeUnit.Pixels, 60, SizeUnit.Pixels, 20));
            var filler = Element("filler", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 50);
            box.AddChildren(auto, filler);

            Layout(box);

            AssertBox(auto, 0, 0, 60, 50);
            AssertBox(filler, 60, 0, 140, 50);
        }
    }
}
