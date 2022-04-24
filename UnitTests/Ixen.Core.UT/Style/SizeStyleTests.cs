using Ixen.Core.Visual.Styles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Style
{
    [TestClass]
    public class SizeStyleTests
    {
        [TestMethod]
        public void TestParse()
        {
            var pixelSize = new SizeStyle("50px");
            Assert.AreEqual(50, pixelSize.Value);
            Assert.AreEqual(SizeUnit.Pixels, pixelSize.Unit);

            var percentSize = new SizeStyle("40%");
            Assert.AreEqual(40, percentSize.Value);
            Assert.AreEqual(SizeUnit.Percents, percentSize.Unit);

            var weightSize = new SizeStyle("2*");
            Assert.AreEqual(2, weightSize.Value);
            Assert.AreEqual(SizeUnit.Weight, weightSize.Unit);
        }
    }
}
