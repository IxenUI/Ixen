using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class RelayoutGeometryTests : BaseGeometryTests
    {
        [TestMethod]
        public void ComputingTheSameLayoutTwice_IsIdempotent()
        {
            var root = Element("root", LayoutType.Row);
            var half = Element("half", LayoutType.Column, SizeUnit.Percents, 50, SizeUnit.Pixels, 20);
            var filler = Element("filler", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 20);
            root.AddChildren(half, filler);

            var surface = new IxenSurface(root);

            surface.ComputeLayout(400, 400);
            AssertBox(half, 0, 0, 200, 20);
            AssertBox(filler, 200, 0, 200, 20);

            surface.ComputeLayout(400, 400);
            AssertBox(half, 0, 0, 200, 20);
            AssertBox(filler, 200, 0, 200, 20);
        }

        [TestMethod]
        public void ResizingTheViewport_RecomputesTheLayout()
        {
            var root = Element("root", LayoutType.Row);
            var half = Element("half", LayoutType.Column, SizeUnit.Percents, 50, SizeUnit.Pixels, 20);
            var filler = Element("filler", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 20);
            root.AddChildren(half, filler);

            var surface = new IxenSurface(root);

            surface.ComputeLayout(400, 400);
            AssertBox(half, 0, 0, 200, 20);

            surface.ComputeLayout(200, 200);
            AssertBox(half, 0, 0, 100, 20);
            AssertBox(filler, 100, 0, 100, 20);
        }

        [TestMethod]
        public void AddingAChildAfterTheFirstLayout_IsPickedUpOnTheNextPass()
        {
            var root = Element("root", LayoutType.Row);
            var first = Element("first", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 20);
            root.AddChild(first);

            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);

            AssertBox(first, 0, 0, 50, 20);

            var second = Element("second", LayoutType.Column, SizeUnit.Pixels, 70, SizeUnit.Pixels, 20);
            root.AddChild(second);

            surface.ComputeLayout(400, 400);

            AssertBox(first, 0, 0, 50, 20);
            AssertBox(second, 50, 0, 70, 20);
        }

        [TestMethod]
        public void RemovingAChildAfterTheFirstLayout_ReflowsTheSiblings()
        {
            var root = Element("root", LayoutType.Row);
            var first = Element("first", LayoutType.Column, SizeUnit.Pixels, 50, SizeUnit.Pixels, 20);
            var second = Element("second", LayoutType.Column, SizeUnit.Pixels, 70, SizeUnit.Pixels, 20);
            root.AddChildren(first, second);

            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);

            AssertBox(second, 50, 0, 70, 20);

            root.RemoveChild(first);
            surface.ComputeLayout(400, 400);

            AssertBox(second, 0, 0, 70, 20);
        }

        [TestMethod]
        public void ChangingAWeightAfterTheFirstLayout_RedistributesTheSpace()
        {
            var root = Element("root", LayoutType.Row);
            var first = Element("first", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 20);
            var second = Element("second", LayoutType.Column, SizeUnit.Weight, 1, SizeUnit.Pixels, 20);
            root.AddChildren(first, second);

            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);

            AssertBox(first, 0, 0, 200, 20);
            AssertBox(second, 200, 0, 200, 20);

            first.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 3 };
            first.Invalidate();
            surface.ComputeLayout(400, 400);

            AssertBox(first, 0, 0, 300, 20);
            AssertBox(second, 300, 0, 100, 20);
        }
    }
}
