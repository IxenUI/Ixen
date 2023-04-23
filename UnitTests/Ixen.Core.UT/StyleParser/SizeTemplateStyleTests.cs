using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class SizeTemplateStyleTests
    {
        [TestMethod]
        public void TestParse()
        {
            SizeTemplateStyleParser parser;
            SizeTemplateStyleDescriptor descriptor;

            parser = new SizeTemplateStyleParser("50px 1* 2% 60px");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(50, descriptor.Value[0].Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Value[0].Unit);
            Assert.AreEqual(1, descriptor.Value[1].Value);
            Assert.AreEqual(SizeUnit.Weight, descriptor.Value[1].Unit);
            Assert.AreEqual(2, descriptor.Value[2].Value);
            Assert.AreEqual(SizeUnit.Percents, descriptor.Value[2].Unit);
            Assert.AreEqual(60, descriptor.Value[3].Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Value[3].Unit);

            parser = new SizeTemplateStyleParser("50px 2");
            Assert.IsFalse(parser.IsValid);

            parser = new SizeTemplateStyleParser("50px 0");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(50, descriptor.Value[0].Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Value[0].Unit);
            Assert.AreEqual(0, descriptor.Value[1].Value);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Value[1].Unit);
        }
    }
}
