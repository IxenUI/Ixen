using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Style
{
    [TestClass]
    public class MarginStyleTests
    {
        [TestMethod]
        public void TestParse()
        {
            var margin1Arg = new MarginStyle("20px");
            Assert.IsTrue(margin1Arg.IsValid);
            Assert.AreEqual(20, margin1Arg.Top.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin1Arg.Top.Unit);
            Assert.AreEqual(20, margin1Arg.Right.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin1Arg.Right.Unit);
            Assert.AreEqual(20, margin1Arg.Bottom.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin1Arg.Bottom.Unit);
            Assert.AreEqual(20, margin1Arg.Left.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin1Arg.Left.Unit);

            var margin2Arg = new MarginStyle("40px 0");
            Assert.IsTrue(margin2Arg.IsValid);
            Assert.AreEqual(40, margin2Arg.Top.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin2Arg.Top.Unit);
            Assert.AreEqual(0, margin2Arg.Right.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin2Arg.Right.Unit);
            Assert.AreEqual(40, margin2Arg.Bottom.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin2Arg.Bottom.Unit);
            Assert.AreEqual(0, margin2Arg.Left.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin2Arg.Left.Unit);

            var margin3Arg = new MarginStyle("50px 10% 0");
            Assert.IsTrue(margin3Arg.IsValid);
            Assert.AreEqual(50, margin3Arg.Top.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin3Arg.Top.Unit);
            Assert.AreEqual(10, margin3Arg.Right.Value);
            Assert.AreEqual(SizeUnit.Percents, margin3Arg.Right.Unit);
            Assert.AreEqual(0, margin3Arg.Bottom.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin3Arg.Bottom.Unit);
            Assert.AreEqual(10, margin3Arg.Left.Value);
            Assert.AreEqual(SizeUnit.Percents, margin3Arg.Left.Unit);

            var margin4Args = new MarginStyle("10px 0 5px 50%");
            Assert.IsTrue(margin4Args.IsValid);
            Assert.AreEqual(10, margin4Args.Top.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin4Args.Top.Unit);
            Assert.AreEqual(0, margin4Args.Right.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin4Args.Right.Unit);
            Assert.AreEqual(5, margin4Args.Bottom.Value);
            Assert.AreEqual(SizeUnit.Pixels, margin4Args.Bottom.Unit);
            Assert.AreEqual(50, margin4Args.Left.Value);
            Assert.AreEqual(SizeUnit.Percents, margin4Args.Left.Unit);
        }
    }
}
