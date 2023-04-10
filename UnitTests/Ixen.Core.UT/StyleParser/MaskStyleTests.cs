using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class MaskStyleTests
    {
        [TestMethod]
        public void TestParse()
        {
            MaskStyleParser parser;
            MaskStyleDescriptor descriptor;

            parser = new MaskStyleParser("mask");
            descriptor = parser.Descriptor;
            Assert.IsFalse(parser.IsValid);

            parser = new MaskStyleParser("10");
            descriptor = parser.Descriptor;
            Assert.IsFalse(parser.IsValid);

            parser = new MaskStyleParser("1");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.IsTrue(descriptor.Top);
            Assert.IsTrue(descriptor.Right);
            Assert.IsTrue(descriptor.Bottom);
            Assert.IsTrue(descriptor.Left);

            parser = new MaskStyleParser("1 0");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.IsTrue(descriptor.Top);
            Assert.IsFalse(descriptor.Right);
            Assert.IsTrue(descriptor.Bottom);
            Assert.IsFalse(descriptor.Left);

            parser = new MaskStyleParser("0 1 1");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.IsFalse(descriptor.Top);
            Assert.IsTrue(descriptor.Right);
            Assert.IsTrue(descriptor.Bottom);
            Assert.IsTrue(descriptor.Left);

            parser = new MaskStyleParser("1 1 0 1");
            descriptor = parser.Descriptor;
            Assert.IsTrue(parser.IsValid);
            Assert.IsTrue(descriptor.Top);
            Assert.IsTrue(descriptor.Right);
            Assert.IsFalse(descriptor.Bottom);
            Assert.IsTrue(descriptor.Left);
        }
    }
}
