using Ixen.Core.Components;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Mask
{
    [TestClass]
    public class MaskLayoutXnlTests : BaseVisualTests
    {
        [TestMethod]
        public void TestMaskLayout1() 
            => AssertVisual("0192c2d99d732698e53cd8a116817e3d", new Component<MaskLayoutTest1View>().View);
    }
}
