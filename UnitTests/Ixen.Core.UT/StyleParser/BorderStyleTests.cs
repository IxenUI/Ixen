using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class BorderStyleTests
    {
        private static BorderStyleDescriptor Parse(string value)
        {
            var xnsSource = new XnsSource($"box {{ border: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (BorderStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var xnsSource = new XnsSource($"box {{ border: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'{value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void AColourAndAThicknessAreRead()
        {
            BorderStyleDescriptor border = Parse("#CCCCCC 1px");

            Assert.AreEqual("#CCCCCC", border.Color);
            Assert.AreEqual(1, border.Thickness);
        }

        [TestMethod]
        public void ThePartsAreOrderIndependent()
        {
            BorderStyleDescriptor border = Parse("2px #FF0000");

            Assert.AreEqual("#FF0000", border.Color);
            Assert.AreEqual(2, border.Thickness);
        }

        [TestMethod]
        public void TheUnitIsOptional()
        {
            Assert.AreEqual(3, Parse("#000000 3").Thickness);
        }

        [TestMethod]
        public void AFractionalThicknessIsAccepted()
        {
            Assert.AreEqual(0.5f, Parse("#000000 0.5px").Thickness);
        }

        [TestMethod]
        public void TheTypeDefaultsToOuter()
        {
            Assert.AreEqual(BorderType.Outer, Parse("#000000 1px").Type);
        }

        [TestMethod]
        public void TheTypeCanBeGivenAndIsCaseInsensitive()
        {
            Assert.AreEqual(BorderType.Inner, Parse("#000000 1px inner").Type);
            Assert.AreEqual(BorderType.Center, Parse("#000000 1px CENTER").Type);
        }

        [TestMethod]
        public void AMissingThicknessIsRejected()
        {
            AssertRejected("#CCCCCC");
        }

        [TestMethod]
        public void AMissingColourIsRejected()
        {
            AssertRejected("1px");
        }

        [TestMethod]
        public void AnUnknownWordIsRejected()
        {
            AssertRejected("#000000 1px dotted");
        }

        [TestMethod]
        public void ADuplicatedPartIsRejected()
        {
            AssertRejected("#000000 #FFFFFF 1px");
        }
    }
}
