using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class CornerRadiusStyleTests
    {
        private static CornerRadiusStyleDescriptor Parse(string value)
        {
            var xnsSource = new XnsSource($"box {{ corner-radius: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (CornerRadiusStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertCorners(CornerRadiusStyleDescriptor d,
            float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            Assert.AreEqual(topLeft, d.TopLeft, "TopLeft");
            Assert.AreEqual(topRight, d.TopRight, "TopRight");
            Assert.AreEqual(bottomRight, d.BottomRight, "BottomRight");
            Assert.AreEqual(bottomLeft, d.BottomLeft, "BottomLeft");
        }

        [TestMethod]
        public void OneValue_AppliesToEveryCorner()
        {
            AssertCorners(Parse("8px"), 8, 8, 8, 8);
        }

        [TestMethod]
        public void TheUnitIsOptional()
        {
            AssertCorners(Parse("8"), 8, 8, 8, 8);
        }

        [TestMethod]
        public void TwoValues_PairOppositeCorners()
        {
            AssertCorners(Parse("8px 4px"), 8, 4, 8, 4);
        }

        [TestMethod]
        public void ThreeValues_ShareTheSecondBetweenTopRightAndBottomLeft()
        {
            AssertCorners(Parse("8px 4px 2px"), 8, 4, 2, 4);
        }

        [TestMethod]
        public void FourValues_GoClockwiseFromTopLeft()
        {
            AssertCorners(Parse("1px 2px 3px 4px"), 1, 2, 3, 4);
        }

        [TestMethod]
        public void FractionalValuesAreAccepted()
        {
            AssertCorners(Parse("2.5px"), 2.5f, 2.5f, 2.5f, 2.5f);
        }

        [TestMethod]
        public void ZeroMeansNoRadius()
        {
            Assert.IsFalse(Parse("0").HasRadius);
        }

        [TestMethod]
        public void ANonZeroValueMeansThereIsARadius()
        {
            Assert.IsTrue(Parse("0 0 0 3px").HasRadius);
        }

        [TestMethod]
        public void AnInvalidValueIsReported()
        {
            var xnsSource = new XnsSource("box { corner-radius: fat }");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors);
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void MoreThanFourValuesAreReported()
        {
            var xnsSource = new XnsSource("box { corner-radius: 1px 2px 3px 4px 5px }");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors);
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }
    }
}
