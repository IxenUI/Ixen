using Ixen.Core.Components;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Padding
{
    [TestClass]
    public class PaddingLayoutXnlTests : BaseVisualTests
    {
        [TestMethod]
        public void TestPaddingLayout1() 
            => AssertVisual("3ae790ae8c088e36fb71e74d82498635", new Component<PaddingLayoutTest1View>().View);
    }
}
