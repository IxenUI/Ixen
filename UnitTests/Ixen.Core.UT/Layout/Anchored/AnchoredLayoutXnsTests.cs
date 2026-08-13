using Ixen.Core.Components;
using Ixen.Core.UT.Layout.Geometry;
using Ixen.Core.Visual;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Anchored
{
    [TestClass]
    public class AnchoredLayoutXnsTests : BaseGeometryTests
    {
        [TestMethod]
        public void AnchorsDeclaredInXnsPlaceTheChildren()
        {
            VisualElement view = new Component<AnchoredLayoutTest1View>().View;
            Layout(view);

            VisualElement canvas = view.Children[0];

            AssertBox(canvas, 0, 0, 300, 200);
            AssertBox(canvas.Children[0], 30, 15, 40, 20);
            AssertBox(canvas.Children[1], 180, 150, 100, 40);
            AssertBox(canvas.Children[2], 10, 160, 270, 40);
        }
    }
}
