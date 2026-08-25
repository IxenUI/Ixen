using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class TransformStyleTests
    {
        private static T Parse<T>(string style, string value)
            where T : StyleDescriptor
        {
            var xnsSource = new XnsSource($"box {{ {style}: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (T)set.Classes.Single().Styles.Single();
        }

        private static TransformStyleDescriptor Transform(string value)
            => Parse<TransformStyleDescriptor>("transform", value);

        private static TransformOriginStyleDescriptor Origin(string value)
            => Parse<TransformOriginStyleDescriptor>("transform-origin", value);

        private static void AssertRejected(string style, string value)
        {
            var xnsSource = new XnsSource($"box {{ {style}: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'{style}: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void ATranslationReadsBothAxes()
        {
            TransformOperation operation = Transform("translate(10px 20px)").Operations.Single();

            Assert.AreEqual(TransformKind.Translate, operation.Kind);
            Assert.AreEqual(10f, operation.X);
            Assert.AreEqual(20f, operation.Y);
            Assert.AreEqual(SizeUnit.Pixels, operation.XUnit);
        }

        [TestMethod]
        public void ASingleArgumentTranslationLeavesTheOtherAxisAlone()
        {
            TransformOperation operation = Transform("translate(10px)").Operations.Single();

            Assert.AreEqual(10f, operation.X);
            Assert.AreEqual(0f, operation.Y);
        }

        [TestMethod]
        public void TheSingleAxisFormsPickTheirOwnAxis()
        {
            Assert.AreEqual(12f, Transform("translateX(12px)").Operations.Single().X);
            Assert.AreEqual(0f, Transform("translateX(12px)").Operations.Single().Y);

            Assert.AreEqual(0f, Transform("translateY(12px)").Operations.Single().X);
            Assert.AreEqual(12f, Transform("translateY(12px)").Operations.Single().Y);
        }

        [TestMethod]
        public void ATranslationTakesAPercentage()
        {
            TransformOperation operation = Transform("translate(-50% 25%)").Operations.Single();

            Assert.AreEqual(SizeUnit.Percents, operation.XUnit);
            Assert.AreEqual(-50f, operation.X);
            Assert.AreEqual(SizeUnit.Percents, operation.YUnit);
            Assert.AreEqual(25f, operation.Y);
        }

        [TestMethod]
        public void ABareNumberIsPixelsOnATranslation()
        {
            TransformOperation operation = Transform("translate(8)").Operations.Single();

            Assert.AreEqual(SizeUnit.Pixels, operation.XUnit);
            Assert.AreEqual(8f, operation.X);
        }

        [TestMethod]
        public void OneScaleFactorAppliesToBothAxes()
        {
            TransformOperation operation = Transform("scale(1.5)").Operations.Single();

            Assert.AreEqual(TransformKind.Scale, operation.Kind);
            Assert.AreEqual(1.5f, operation.X);
            Assert.AreEqual(1.5f, operation.Y);
        }

        [TestMethod]
        public void ASingleAxisScaleLeavesTheOtherAtOne()
        {
            Assert.AreEqual(1f, Transform("scaleX(3)").Operations.Single().Y,
                "the untouched axis keeps its natural size, so its factor is 1 rather than 0");

            Assert.AreEqual(1f, Transform("scaleY(3)").Operations.Single().X);
        }

        [TestMethod]
        public void AScaleRefusesAUnit()
        {
            AssertRejected("transform", "scale(2px)");
            AssertRejected("transform", "scale(200%)");
        }

        [TestMethod]
        public void ARotationNeedsItsDegrees()
        {
            Assert.AreEqual(-15f, Transform("rotate(-15deg)").Operations.Single().X);

            AssertRejected("transform", "rotate(15)");
            AssertRejected("transform", "rotate(15px)");
        }

        [TestMethod]
        public void ASkewTakesOneOrTwoAngles()
        {
            TransformOperation both = Transform("skew(10deg 4deg)").Operations.Single();

            Assert.AreEqual(TransformKind.Skew, both.Kind);
            Assert.AreEqual(10f, both.X);
            Assert.AreEqual(4f, both.Y);

            Assert.AreEqual(0f, Transform("skewY(7deg)").Operations.Single().X);
            Assert.AreEqual(7f, Transform("skewY(7deg)").Operations.Single().Y);
        }

        [TestMethod]
        public void SeveralFunctionsKeepTheirOrder()
        {
            var operations = Transform("translate(10px) rotate(45deg) scale(2)").Operations;

            Assert.AreEqual(3, operations.Count);
            Assert.AreEqual(TransformKind.Translate, operations[0].Kind);
            Assert.AreEqual(TransformKind.Rotate, operations[1].Kind);
            Assert.AreEqual(TransformKind.Scale, operations[2].Kind);
        }

        [TestMethod]
        public void NoneIsAValidAndEmptyTransform()
        {
            TransformStyleDescriptor descriptor = Transform("none");

            Assert.AreEqual(0, descriptor.Operations.Count);
            Assert.IsFalse(descriptor.IsDeclared,
                "transform is not inherited, so an empty list needs no flag to mean nothing");
        }

        [TestMethod]
        public void TooManyArgumentsAreRejected()
        {
            AssertRejected("transform", "translate(1px 2px 3px)");
            AssertRejected("transform", "translateX(1px 2px)");
            AssertRejected("transform", "rotate(1deg 2deg)");
        }

        [TestMethod]
        public void NonsenseIsRejected()
        {
            AssertRejected("transform", "wobble(2)");
            AssertRejected("transform", "translate()");
            AssertRejected("transform", "rotate(45deg");
        }

        [TestMethod]
        public void AValueCannotStartWithAParenthesis()
        {
            var xnsSource = new XnsSource("box { transform: (45deg) }");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, xnsSource.Diagnostics[0].Code,
                "the parenthesis is in the continuation set only, so this never reaches the parser");
        }

        [TestMethod]
        public void CalcFoldsInsideAFunction()
        {
            var xnsSource = new XnsSource("$gap: 6px\nbox { transform: translate(calc($gap * 2)) }");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            var descriptor = (TransformStyleDescriptor)set.Classes.Single().Styles.Single();

            Assert.AreEqual(12f, descriptor.Operations.Single().X,
                "calc is word-bounded and folds before the transform parser sees the value");
        }

        [TestMethod]
        public void TheOriginDefaultsToTheCentre()
        {
            var descriptor = new TransformOriginStyleDescriptor();

            Assert.IsTrue(descriptor.IsDefault);
            Assert.AreEqual(50f, descriptor.X);
            Assert.AreEqual(SizeUnit.Percents, descriptor.XUnit);
        }

        [TestMethod]
        public void TheOriginTakesKeywordsOnEitherAxis()
        {
            TransformOriginStyleDescriptor topLeft = Origin("left top");

            Assert.AreEqual(0f, topLeft.X);
            Assert.AreEqual(0f, topLeft.Y);
            Assert.IsFalse(topLeft.IsDefault);

            TransformOriginStyleDescriptor bottomRight = Origin("bottom right");

            Assert.AreEqual(100f, bottomRight.X, "the keywords name their own axis, so order is free");
            Assert.AreEqual(100f, bottomRight.Y);
        }

        [TestMethod]
        public void TheOriginKeepsTextAlignsCentreAndMiddleSplit()
        {
            Assert.AreEqual(50f, Origin("center").X);
            Assert.AreEqual(50f, Origin("middle").Y);
            Assert.IsTrue(Origin("center middle").IsDefault);
        }

        [TestMethod]
        public void TheOriginTakesLengthsAndPercentages()
        {
            TransformOriginStyleDescriptor pixels = Origin("10px 20px");

            Assert.AreEqual(SizeUnit.Pixels, pixels.XUnit);
            Assert.AreEqual(10f, pixels.X);
            Assert.AreEqual(20f, pixels.Y);

            Assert.AreEqual(SizeUnit.Percents, Origin("25% 75%").XUnit);
            Assert.AreEqual(-8f, Origin("-8px").X, "an origin outside the box is legal");
        }

        [TestMethod]
        public void TheOriginRefusesTwoValuesOnOneAxis()
        {
            AssertRejected("transform-origin", "left right");
            AssertRejected("transform-origin", "top bottom");
            AssertRejected("transform-origin", "left center");
        }

        [TestMethod]
        public void TheOriginRefusesNonsense()
        {
            AssertRejected("transform-origin", "sideways");
            AssertRejected("transform-origin", "10px 20px 30px");
            AssertRejected("transform-origin", "1*");
        }

        [TestMethod]
        public void BothRoundTripThroughGeneratedSource()
        {
            string transform = Transform("translate(4px -2%) rotate(30deg)").ToSource();

            StringAssert.Contains(transform, "TransformKind.Translate");
            StringAssert.Contains(transform, "SizeUnit.Percents");
            StringAssert.Contains(transform, "TransformKind.Rotate");

            StringAssert.Contains(Origin("left bottom").ToSource(), "Y = 100f");
        }
    }
}
