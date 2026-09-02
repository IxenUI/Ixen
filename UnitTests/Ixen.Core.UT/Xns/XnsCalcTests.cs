using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsCalcTests
    {
        private static SizeStyleDescriptor Width(string value)
        {
            var source = new XnsSource($"box {{ width: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return (SizeStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value, string expected)
        {
            var source = new XnsSource($"box {{ width: {value} }}");
            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'{value}' should have been rejected");
            StringAssert.Contains(
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)), expected);
        }

        [TestMethod]
        public void MultiplicationKeepsTheUnit()
        {
            SizeStyleDescriptor width = Width("calc(8px * 2)");

            Assert.AreEqual(16, width.Value);
            Assert.AreEqual(SizeUnit.Pixels, width.Unit);
        }

        [TestMethod]
        public void TheUnitMayBeOnEitherSide()
        {
            Assert.AreEqual(16, Width("calc(2 * 8px)").Value);
        }

        [TestMethod]
        public void DivisionKeepsTheUnitOfTheDividend()
        {
            SizeStyleDescriptor width = Width("calc(30px / 4)");

            Assert.AreEqual(7.5f, width.Value);
            Assert.AreEqual(SizeUnit.Pixels, width.Unit);
        }

        [TestMethod]
        public void AdditionNeedsMatchingUnits()
        {
            SizeStyleDescriptor width = Width("calc(8px + 4px)");

            Assert.AreEqual(12, width.Value);
            Assert.AreEqual(SizeUnit.Pixels, width.Unit);
        }

        [TestMethod]
        public void PercentagesComputeAmongThemselves()
        {
            SizeStyleDescriptor width = Width("calc(50% / 2)");

            Assert.AreEqual(25, width.Value);
            Assert.AreEqual(SizeUnit.Percents, width.Unit);
        }

        [TestMethod]
        public void ProductBindsTighterThanSum()
        {
            Assert.AreEqual(14, Width("calc(2px + 4px * 3)").Value);
        }

        [TestMethod]
        public void ParenthesesOverridePrecedence()
        {
            Assert.AreEqual(18, Width("calc((2px + 4px) * 3)").Value);
        }

        [TestMethod]
        public void ADecimalResultIsEmittedWithoutAnExponent()
        {
            SizeStyleDescriptor width = Width("calc(10px / 3)");

            Assert.AreEqual(3.3333f, width.Value, 0.0001f);
        }

        [TestMethod]
        public void AVariableFeedsTheExpression()
        {
            var source = new XnsSource("$gap: 8px\r\nbox {\r\n    width: calc($gap * 2)\r\n}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var width = (SizeStyleDescriptor)set.Classes.Single().Styles.Single();

            Assert.AreEqual(16, width.Value);
            Assert.AreEqual(SizeUnit.Pixels, width.Unit);
        }

        [TestMethod]
        public void AVariableMayHoldAnAlreadyComputedValue()
        {
            var source = new XnsSource(
                "$gap: 8px\r\n$wide: calc($gap * 3)\r\nbox {\r\n    width: $wide\r\n}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            Assert.AreEqual(24, ((SizeStyleDescriptor)set.Classes.Single().Styles.Single()).Value);
        }

        [TestMethod]
        public void SeveralCallsInOneValueAllEvaluate()
        {
            var source = new XnsSource(
                "$gap: 8px\r\nbox {\r\n    margin: calc($gap * 2) calc($gap / 2)\r\n}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var margin = (MarginStyleDescriptor)set.Classes.Single().Styles.Single();

            Assert.AreEqual(16, margin.Top.Value);
            Assert.AreEqual(4, margin.Right.Value);
        }

        [TestMethod]
        public void ACallMixesWithOrdinaryValues()
        {
            var source = new XnsSource("box {\r\n    border: #CCCCCC calc(1px * 2) inner\r\n}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var border = (BorderStyleDescriptor)set.Classes.Single().Styles.Single();

            Assert.AreEqual("#CCCCCC", border.Color);
            Assert.AreEqual(2, border.Thickness);
        }

        [TestMethod]
        public void NestedCallsWork()
        {
            Assert.AreEqual(24, Width("calc(calc(4px * 2) * 3)").Value);
        }

        [TestMethod]
        public void ANegativeResultIsReportedRatherThanTurnedPositive()
        {
            AssertRejected("calc(4px - 10px)", "negative");
        }

        [TestMethod]
        public void MixedUnitsReduceToALinearForm()
        {
            SizeStyleDescriptor width = Width("calc(100% - 20px)");

            Assert.AreEqual(SizeUnit.Percents, width.Unit);
            Assert.AreEqual(100f, width.Value);

            Assert.AreEqual(-20f, width.Offset,
                "this used to be reported as unfoldable. Any calc over % and px with + - * / "
                + "by a scalar reduces to a * container + b, so two floats carry it to measure");
        }

        [TestMethod]
        public void ThePixelPartMayBePositive()
        {
            SizeStyleDescriptor width = Width("calc(50% + 10px)");

            Assert.AreEqual(50f, width.Value);
            Assert.AreEqual(10f, width.Offset);
        }

        [TestMethod]
        public void TheTermsMayComeInAnyOrder()
        {
            SizeStyleDescriptor width = Width("calc(20px + 100%)");

            Assert.AreEqual(100f, width.Value);
            Assert.AreEqual(20f, width.Offset);
        }

        [TestMethod]
        public void SeveralTermsCollapseIntoTheSameTwoNumbers()
        {
            SizeStyleDescriptor width = Width("calc(100% - 10px - 2 * 5px + 25%)");

            Assert.AreEqual(125f, width.Value);
            Assert.AreEqual(-20f, width.Offset);
        }

        [TestMethod]
        public void AScalarScalesBothParts()
        {
            SizeStyleDescriptor width = Width("calc((50% + 10px) * 2)");

            Assert.AreEqual(100f, width.Value);
            Assert.AreEqual(20f, width.Offset);
        }

        [TestMethod]
        public void ADivisorScalesBothParts()
        {
            SizeStyleDescriptor width = Width("calc((100% + 40px) / 2)");

            Assert.AreEqual(50f, width.Value);
            Assert.AreEqual(20f, width.Offset);
        }

        [TestMethod]
        public void APureResultKeepsItsOldShape()
        {
            SizeStyleDescriptor pixels = Width("calc(10px + 6px)");

            Assert.AreEqual(SizeUnit.Pixels, pixels.Unit);
            Assert.AreEqual(16f, pixels.Value);
            Assert.AreEqual(0f, pixels.Offset, "no offset when the units never mixed");

            SizeStyleDescriptor percents = Width("calc(20% * 3)");

            Assert.AreEqual(SizeUnit.Percents, percents.Unit);
            Assert.AreEqual(60f, percents.Value);
            Assert.AreEqual(0f, percents.Offset);
        }

        [TestMethod]
        public void APartThatCancelsOutLeavesAPureValue()
        {
            SizeStyleDescriptor width = Width("calc(100% + 20px - 20px)");

            Assert.AreEqual(SizeUnit.Percents, width.Unit);
            Assert.AreEqual(100f, width.Value);
            Assert.AreEqual(0f, width.Offset);
        }

        [TestMethod]
        public void APlainNumberStillCannotBeAddedToALength()
        {
            AssertRejected("calc(100% + 3)", "no unit");
            AssertRejected("calc(3 + 20px)", "no unit");
        }

        [TestMethod]
        public void ANegativePureValueIsStillRefused()
        {
            AssertRejected("calc(4px - 10px)", "negative");
            AssertRejected("calc(20% - 50%)", "negative");
        }

        [TestMethod]
        public void TwoUnitsCannotMultiply()
        {
            AssertRejected("calc(2px * 3px)", "plain number");
        }

        [TestMethod]
        public void DividingByAUnitIsReported()
        {
            AssertRejected("calc(20px / 2px)", "plain number");
        }

        [TestMethod]
        public void DivisionByZeroIsReported()
        {
            AssertRejected("calc(20px / 0)", "division by zero");
        }

        [TestMethod]
        public void AnUnclosedCallIsReported()
        {
            AssertRejected("calc(8px * 2", "closing parenthesis");
        }

        [TestMethod]
        public void NonsenseInsideIsReported()
        {
            AssertRejected("calc(px)", "not an expression");
        }

        [TestMethod]
        public void AWeightIsNotAQuantityToComputeWith()
        {
            AssertRejected("calc(1* + 2*)", "not an expression");
        }

        [TestMethod]
        public void AValueWithoutACallIsUntouched()
        {
            SizeStyleDescriptor width = Width("200px");

            Assert.AreEqual(200, width.Value);
            Assert.AreEqual(SizeUnit.Pixels, width.Unit);
        }

        [TestMethod]
        public void AWeightStillParsesNormally()
        {
            SizeStyleDescriptor width = Width("2*");

            Assert.AreEqual(2, width.Value);
            Assert.AreEqual(SizeUnit.Weight, width.Unit,
                "adding parentheses and '+' to the value set must not disturb the weight sigil");
        }
    }
}
