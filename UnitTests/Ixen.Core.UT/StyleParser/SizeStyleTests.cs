using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class SizeStyleTests
    {
        [TestMethod]
        public void TestParse()
        {
            SizeStyleParser parser;
            SizeStyleDescriptor descriptor;

            parser = new SizeStyleParser("50px");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(50, descriptor.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Unit);

            parser = new SizeStyleParser("50");
            descriptor = parser.Descriptor;
            Assert.IsFalse(parser.IsValid);

            parser = new SizeStyleParser("0");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(0, descriptor.Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Unit);

            parser = new SizeStyleParser("40%");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(40, descriptor.Value);
            Assert.AreEqual(SizeUnit.Percents, descriptor.Unit);

            parser = new SizeStyleParser("2*");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(2, descriptor.Value);
            Assert.AreEqual(SizeUnit.Weight, descriptor.Unit);
        }
    }
}
