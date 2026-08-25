using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class ShadowStyleTests
    {
        private static T Parse<T>(string style, string value)
            where T : ShadowStyleDescriptor
        {
            var xnsSource = new XnsSource($"box {{ {style}: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (T)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string style, string value)
        {
            var xnsSource = new XnsSource($"box {{ {style}: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'{style}: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void AnOffsetAndAColourAreEnough()
        {
            BoxShadowStyleDescriptor shadow = Parse<BoxShadowStyleDescriptor>("box-shadow", "0px 4px #40000000");

            Assert.AreEqual(0f, shadow.OffsetX);
            Assert.AreEqual(4f, shadow.OffsetY);
            Assert.AreEqual(0f, shadow.Blur);
            Assert.AreEqual(0f, shadow.Spread);
            Assert.AreEqual("#40000000", shadow.Color);
            Assert.IsTrue(shadow.IsDeclared);
        }

        [TestMethod]
        public void TheThirdLengthIsTheBlurAndTheFourthTheSpread()
        {
            BoxShadowStyleDescriptor shadow =
                Parse<BoxShadowStyleDescriptor>("box-shadow", "1px 2px 8px 3px #80112233");

            Assert.AreEqual(1f, shadow.OffsetX);
            Assert.AreEqual(2f, shadow.OffsetY);
            Assert.AreEqual(8f, shadow.Blur);
            Assert.AreEqual(3f, shadow.Spread);
        }

        [TestMethod]
        public void TheOffsetsMayBeNegative()
        {
            BoxShadowStyleDescriptor shadow =
                Parse<BoxShadowStyleDescriptor>("box-shadow", "-3px -5px 4px #40000000");

            Assert.AreEqual(-3f, shadow.OffsetX);
            Assert.AreEqual(-5f, shadow.OffsetY);
        }

        [TestMethod]
        public void TheColourMayComeFirst()
        {
            BoxShadowStyleDescriptor shadow =
                Parse<BoxShadowStyleDescriptor>("box-shadow", "#40000000 0px 4px 8px");

            Assert.AreEqual(4f, shadow.OffsetY);
            Assert.AreEqual(8f, shadow.Blur);
            Assert.AreEqual("#40000000", shadow.Color);
        }

        [TestMethod]
        public void ThePxSuffixIsOptional()
        {
            BoxShadowStyleDescriptor shadow = Parse<BoxShadowStyleDescriptor>("box-shadow", "0 4 8 #40000000");

            Assert.AreEqual(4f, shadow.OffsetY);
            Assert.AreEqual(8f, shadow.Blur);
        }

        [TestMethod]
        public void ANegativeBlurOrSpreadIsRejected()
        {
            AssertRejected("box-shadow", "0px 4px -8px #40000000");
            AssertRejected("box-shadow", "0px 4px 8px -2px #40000000");
        }

        [TestMethod]
        public void AColourIsRequired()
        {
            AssertRejected("box-shadow", "0px 4px 8px");
        }

        [TestMethod]
        public void TwoOffsetsAreRequired()
        {
            AssertRejected("box-shadow", "4px #40000000");
            AssertRejected("box-shadow", "#40000000");
        }

        [TestMethod]
        public void AFifthLengthIsRejected()
        {
            AssertRejected("box-shadow", "1px 2px 3px 4px 5px #40000000");
        }

        [TestMethod]
        public void TwoColoursAreRejected()
        {
            AssertRejected("box-shadow", "0px 4px #40000000 #FF0000");
        }

        [TestMethod]
        public void AKeywordIsRejected()
        {
            AssertRejected("box-shadow", "0px 4px 8px #40000000 inset");
        }

        [TestMethod]
        public void ATextShadowTakesNoSpread()
        {
            TextShadowStyleDescriptor shadow =
                Parse<TextShadowStyleDescriptor>("text-shadow", "0px 1px 3px #80000000");

            Assert.AreEqual(3f, shadow.Blur);
            Assert.AreEqual(0f, shadow.Spread);

            AssertRejected("text-shadow", "0px 1px 3px 2px #80000000");
        }

        [TestMethod]
        public void BothRoundTripThroughGeneratedSource()
        {
            string box = Parse<BoxShadowStyleDescriptor>("box-shadow", "1px 2px 3px 4px #80112233").ToSource();
            string text = Parse<TextShadowStyleDescriptor>("text-shadow", "-1px 2px 3px #FF445566").ToSource();

            StringAssert.Contains(box, "OffsetX = 1f");
            StringAssert.Contains(box, "Spread = 4f");
            StringAssert.Contains(box, "#80112233");

            StringAssert.Contains(text, "OffsetX = -1f");
            StringAssert.Contains(text, "#FF445566");
        }

        [TestMethod]
        public void AnUndeclaredShadowIsNotDeclared()
        {
            Assert.IsFalse(new BoxShadowStyleDescriptor().IsDeclared);
            Assert.IsFalse(new TextShadowStyleDescriptor().IsDeclared);
        }
    }
}
