using Ixen.Core.Components;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Margin
{
    [TestClass]
    public class MarginLayoutXnlTests : BaseVisualTests
    {
        [TestMethod]
        public void TestMarginLayout1() 
            => AssertVisual("844d7bbaa2fb7e684aeeb9e4fa8dd7b8", new Component<MarginLayoutTest1View>().View);

        [TestMethod]
        public void TestMarginLayout2()
            => AssertVisual("427eb920da121a828eb5e506f1eb45c2", new Component<MarginLayoutTest2View>().View);
    }
}
