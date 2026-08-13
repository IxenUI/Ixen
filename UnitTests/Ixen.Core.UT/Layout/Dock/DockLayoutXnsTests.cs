using Ixen.Core.Components;
using Ixen.Core.UT.Layout.Geometry;
using Ixen.Core.Visual;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Dock
{
    [TestClass]
    public class DockLayoutXnsTests : BaseGeometryTests
    {
        [TestMethod]
        public void DockSidesDeclaredInXnsCarveTheContainer()
        {
            VisualElement view = new Component<DockLayoutTest1View>().View;
            Layout(view);

            VisualElement shell = view.Children[0];

            AssertBox(shell, 0, 0, 300, 200);
            AssertBox(shell.Children[0], 0, 0, 300, 30);
            AssertBox(shell.Children[1], 0, 180, 300, 20);
            AssertBox(shell.Children[2], 0, 30, 60, 150);
            AssertBox(shell.Children[3], 60, 30, 240, 150);
        }
    }
}
