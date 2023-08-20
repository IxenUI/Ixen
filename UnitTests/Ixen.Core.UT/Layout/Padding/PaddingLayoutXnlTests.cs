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
            => AssertVisual("aead6cc112183963c9ae827519babe28", new Component<PaddingLayoutTest1View>().View);
    }
}
