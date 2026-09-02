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
            AssertRejected("#000000 1px wobbly");
        }

        [TestMethod]
        public void TheStyleDefaultsToSolid()
        {
            Assert.AreEqual(BorderStyle.Solid, Parse("#CCCCCC 1px").Style);
        }

        [TestMethod]
        public void DashedAndDottedAreRead()
        {
            Assert.AreEqual(BorderStyle.Dashed, Parse("#CCCCCC 1px dashed").Style);
            Assert.AreEqual(BorderStyle.Dotted, Parse("#CCCCCC 1px dotted").Style);
            Assert.AreEqual(BorderStyle.Solid, Parse("#CCCCCC 1px solid").Style);
        }

        [TestMethod]
        public void AStyleAndATypeAreTwoDifferentWords()
        {
            BorderStyleDescriptor border = Parse("#CCCCCC 1px dashed inner");

            Assert.AreEqual(BorderStyle.Dashed, border.Style);

            Assert.AreEqual(BorderType.Inner, border.Type,
                "the type and the style are two disjoint closed sets of words, so a value may "
                + "carry one of each and the order between them does not matter");
        }

        [TestMethod]
        public void TheOrderOfTheTwoWordsDoesNotMatter()
        {
            BorderStyleDescriptor border = Parse("#CCCCCC 1px outer dotted");

            Assert.AreEqual(BorderType.Outer, border.Type);
            Assert.AreEqual(BorderStyle.Dotted, border.Style);
        }

        [TestMethod]
        public void NeitherWordMayBeRepeated()
        {
            AssertRejected("#CCCCCC 1px dashed dotted");
            AssertRejected("#CCCCCC 1px inner outer");
        }

        [TestMethod]
        public void ADuplicatedTypeIsRejected()
        {
            AssertRejected("#000000 1px inner outer");
        }

        [TestMethod]
        public void OneColourLeavesTheSidesAlone()
        {
            BorderStyleDescriptor border = Parse("#CCCCCC 1px");

            Assert.IsNull(border.TopColor);
            Assert.IsTrue(border.IsOneColor);
            Assert.AreEqual("#CCCCCC", border.ColorLeft, "every side falls back to it");
        }

        [TestMethod]
        public void TwoColoursAreVerticalThenHorizontal()
        {
            BorderStyleDescriptor border = Parse("#000000 #FFFFFF 1px");

            AssertColors(border, "#000000", "#FFFFFF", "#000000", "#FFFFFF");

            Assert.IsFalse(border.IsOneColor,
                "this used to be a duplicated part and therefore a diagnostic");
        }

        [TestMethod]
        public void ThreeColoursGiveTheMiddleToBothSides()
        {
            BorderStyleDescriptor border = Parse("#111111 #222222 #333333 1px");

            AssertColors(border, "#111111", "#222222", "#333333", "#222222");
        }

        [TestMethod]
        public void FourColoursGoClockwise()
        {
            BorderStyleDescriptor border = Parse("#111111 #222222 #333333 #444444 1px");

            AssertColors(border, "#111111", "#222222", "#333333", "#444444");

            Assert.AreEqual("#111111", border.Color,
                "the shared colour stays the first one, so anything reading it still works");
        }

        [TestMethod]
        public void AFifthColourIsRejected()
        {
            AssertRejected("#111111 #222222 #333333 #444444 #555555 1px");
        }

        [TestMethod]
        public void ColoursAndThicknessesAreCountedApart()
        {
            BorderStyleDescriptor border = Parse("#111111 #222222 2px 4px inner");

            AssertColors(border, "#111111", "#222222", "#111111", "#222222");
            AssertSides(border, 2, 4, 2, 4);

            Assert.AreEqual(BorderType.Inner, border.Type,
                "a colour is a #, a thickness is a length and the type is a word, so each "
                + "group runs its own one-to-four rule");
        }

        private static void AssertColors(BorderStyleDescriptor border,
            string top, string right, string bottom, string left)
        {
            Assert.AreEqual(top, border.ColorTop, "top");
            Assert.AreEqual(right, border.ColorRight, "right");
            Assert.AreEqual(bottom, border.ColorBottom, "bottom");
            Assert.AreEqual(left, border.ColorLeft, "left");
        }

        private static void AssertSides(BorderStyleDescriptor border,
            float top, float right, float bottom, float left)
        {
            Assert.AreEqual(top, border.Top, "top");
            Assert.AreEqual(right, border.Right, "right");
            Assert.AreEqual(bottom, border.Bottom, "bottom");
            Assert.AreEqual(left, border.Left, "left");
        }

        [TestMethod]
        public void OneThicknessAppliesToEverySide()
        {
            BorderStyleDescriptor border = Parse("#CCCCCC 2px");

            AssertSides(border, 2, 2, 2, 2);
            Assert.IsTrue(border.IsUniform);
        }

        [TestMethod]
        public void TwoThicknessesAreVerticalThenHorizontal()
        {
            AssertSides(Parse("#CCCCCC 1px 2px"), 1, 2, 1, 2);
        }

        [TestMethod]
        public void ThreeThicknessesAreTopHorizontalBottom()
        {
            AssertSides(Parse("#CCCCCC 1px 2px 3px"), 1, 2, 3, 2);
        }

        [TestMethod]
        public void FourThicknessesGoClockwiseFromTheTop()
        {
            BorderStyleDescriptor border = Parse("#CCCCCC 1px 2px 3px 4px");

            AssertSides(border, 1, 2, 3, 4);
            Assert.IsFalse(border.IsUniform);
        }

        [TestMethod]
        public void ASingleSideIsExpressible()
        {
            BorderStyleDescriptor border = Parse("#445566 0px 0px 1px 0px inner");

            AssertSides(border, 0, 0, 1, 0);
            Assert.AreEqual(BorderType.Inner, border.Type);
            Assert.IsTrue(border.HasBorder, "one non-zero side is still a border");
        }

        [TestMethod]
        public void EverySideAtZeroIsNotABorder()
        {
            Assert.IsFalse(Parse("#445566 0px 0px 0px 0px").HasBorder);
        }

        [TestMethod]
        public void TheThicknessesMayBeWrittenInAnyOrderAmongTheOtherParts()
        {
            AssertSides(Parse("1px 2px 3px 4px #CCCCCC inner"), 1, 2, 3, 4);
            AssertSides(Parse("inner 1px #CCCCCC 2px"), 1, 2, 1, 2);
        }

        [TestMethod]
        public void AFifthThicknessIsRejected()
        {
            AssertRejected("#CCCCCC 1px 2px 3px 4px 5px");
        }

        [TestMethod]
        public void DecimalsWorkPerSide()
        {
            AssertSides(Parse("#CCCCCC 0.5px 1.25px 2px 3px"), 0.5f, 1.25f, 2, 3);
        }
    }
}
