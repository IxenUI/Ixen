using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class GapStyleTests
    {
        private static GapStyleDescriptor Parse(string value)
        {
            var xnsSource = new XnsSource($"box {{ gap: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (GapStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var xnsSource = new XnsSource($"box {{ gap: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'gap: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void OneValueSetsBothAxes()
        {
            GapStyleDescriptor gap = Parse("12px");

            Assert.AreEqual(12f, gap.Row);
            Assert.AreEqual(12f, gap.Column);
        }

        [TestMethod]
        public void TwoValuesAreRowThenColumn()
        {
            GapStyleDescriptor gap = Parse("8px 16px");

            Assert.AreEqual(8f, gap.Row, "the row gap comes first, as in CSS");
            Assert.AreEqual(16f, gap.Column);
        }

        [TestMethod]
        public void ThePxSuffixIsOptional()
        {
            Assert.AreEqual(10f, Parse("10").Column);
        }

        [TestMethod]
        public void ADecimalValueWorks()
        {
            Assert.AreEqual(2.5f, Parse("2.5px").Row);
        }

        [TestMethod]
        public void ZeroIsNotADeclaredGap()
        {
            Assert.IsFalse(Parse("0px").IsDeclared);
            Assert.IsTrue(Parse("0px 4px").IsDeclared, "but one axis alone counts");
        }

        [TestMethod]
        public void APercentageOrWeightIsRejected()
        {
            AssertRejected("10%");
            AssertRejected("1*");
            AssertRejected("?");
        }

        [TestMethod]
        public void ANegativeGapIsRejected()
        {
            AssertRejected("-4px");
        }

        [TestMethod]
        public void ThreeValuesAreRejected()
        {
            AssertRejected("4px 8px 12px");
        }

        [TestMethod]
        public void ItRoundTripsThroughGeneratedSource()
        {
            string source = Parse("8px 16px").ToSource();

            StringAssert.Contains(source, "Row = 8f");
            StringAssert.Contains(source, "Column = 16f");
        }
    }
}
