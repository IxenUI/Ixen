using Ixen.Core.Components;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Row
{
    [TestClass]
    public class RowLayoutXnlTests : BaseVisualTests
    {
        [TestMethod]
        public void TestRowLayout1() 
            => AssertVisual("781d03724ac9caab1c140b3729ffccd6", new Component<RowLayoutTest1View>().View);
    }
}
