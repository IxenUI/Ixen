using Ixen.Core.Components;
using Ixen.Core.UT.Layout.Geometry;
using Ixen.Core.Visual;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Grid
{
    [TestClass]
    public class GridLayoutXnsTests : BaseGeometryTests
    {
        private static VisualElement Container(VisualElement view)
        {
            Layout(view);
            return view.Children[0];
        }

        [TestMethod]
        public void FixedTracksDeclaredInXnsPlaceTheCells()
        {
            VisualElement grid = Container(new Component<GridLayoutTest1View>().View);

            AssertBox(grid, 0, 0, 300, 200);
            AssertBox(grid.Children[0], 0, 0, 100, 50);
            AssertBox(grid.Children[1], 100, 0, 200, 50);
            AssertBox(grid.Children[2], 0, 50, 100, 150);
            AssertBox(grid.Children[3], 100, 50, 200, 150);
        }

        [TestMethod]
        public void AContentTrackDeclaredInXnsTakesItsWidestCell()
        {
            VisualElement grid = Container(new Component<GridLayoutTest2View>().View);

            AssertBox(grid, 0, 0, 180, 80);
            AssertBox(grid.Children[0], 0, 0, 100, 40);
            AssertBox(grid.Children[1], 100, 0, 80, 40);
            AssertBox(grid.Children[2], 0, 40, 100, 40);
            AssertBox(grid.Children[3], 100, 40, 60, 40);
        }
    }
}
