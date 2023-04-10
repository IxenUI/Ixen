using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class SpacingStyleTests
    {
        [TestMethod]
        public void TestParse()
        {
            SpaceStyleParser parser;
            SpaceStyleDescriptor descriptor;

            parser = new SpaceStyleParser("20px");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(20, descriptor.Top.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Top.Unit);
            Assert.AreEqual(20, descriptor.Right.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Right.Unit);
            Assert.AreEqual(20, descriptor.Bottom.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Bottom.Unit);
            Assert.AreEqual(20, descriptor.Left.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Left.Unit);

            parser = new SpaceStyleParser("40px 0");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(40, descriptor.Top.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Top.Unit);
            Assert.AreEqual(0, descriptor.Right.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Right.Unit);
            Assert.AreEqual(40, descriptor.Bottom.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Bottom.Unit);
            Assert.AreEqual(0, descriptor.Left.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Left.Unit);

            parser = new SpaceStyleParser("50px 10% 0");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(50, descriptor.Top.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Top.Unit);
            Assert.AreEqual(10, descriptor.Right.Value);
            Assert.AreEqual(SizeUnit.Percents, descriptor.Right.Unit);
            Assert.AreEqual(0, descriptor.Bottom.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Bottom.Unit);
            Assert.AreEqual(10, descriptor.Left.Value);
            Assert.AreEqual(SizeUnit.Percents, descriptor.Left.Unit);

            parser = new SpaceStyleParser("10px 0 5px 50%");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(10, descriptor.Top.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Top.Unit);
            Assert.AreEqual(0, descriptor.Right.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Right.Unit);
            Assert.AreEqual(5, descriptor.Bottom.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Bottom.Unit);
            Assert.AreEqual(50, descriptor.Left.Value);
            Assert.AreEqual(SizeUnit.Percents, descriptor.Left.Unit);
        }
    }
}
