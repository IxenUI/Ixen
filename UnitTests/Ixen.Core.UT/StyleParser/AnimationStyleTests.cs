using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class AnimationStyleTests
    {
        private static AnimationStyleDescriptor Valid(string value)
        {
            var parser = new AnimationStyleParser(value);

            Assert.IsTrue(parser.IsValid, $"'{value}' should parse");

            return parser.Descriptor;
        }

        private static void Invalid(string value)
            => Assert.IsFalse(new AnimationStyleParser(value).IsValid, $"'{value}' should not parse");

        [TestMethod]
        public void ANameAndADurationAreEnough()
        {
            AnimationStyleDescriptor descriptor = Valid("pulse 600ms");

            Assert.AreEqual("pulse", descriptor.Name);
            Assert.AreEqual(600, descriptor.Duration);
            Assert.AreEqual(0, descriptor.Delay);
            Assert.AreEqual(EasingKind.Linear, descriptor.Easing);
            Assert.AreEqual(1, descriptor.Iterations);
            Assert.IsFalse(descriptor.Alternate);
            Assert.IsTrue(descriptor.IsDeclared);
        }

        [TestMethod]
        public void SecondsWork()
        {
            Assert.AreEqual(1500, Valid("pulse 1.5s").Duration);
        }

        [TestMethod]
        public void ABareNumberIsMilliseconds()
        {
            Assert.AreEqual(600, Valid("pulse 600").Duration);
        }

        [TestMethod]
        public void AnEasingCanFollow()
        {
            Assert.AreEqual(EasingKind.EaseOut, Valid("pulse 600ms ease-out").Easing);
        }

        [TestMethod]
        public void InfiniteIsAKeyword()
        {
            Assert.AreEqual(AnimationStyleDescriptor.INFINITE, Valid("pulse 600ms infinite").Iterations);
        }

        [TestMethod]
        public void ACountIsSuffixed()
        {
            Assert.AreEqual(3, Valid("pulse 600ms 3x").Iterations);
        }

        [TestMethod]
        public void ABareSecondNumberIsADelayNotACount()
        {
            AnimationStyleDescriptor descriptor = Valid("pulse 600ms 40ms");

            Assert.AreEqual(40, descriptor.Delay,
                "a duration and a count would be ambiguous, so a count must carry its x");
            Assert.AreEqual(1, descriptor.Iterations);
        }

        [TestMethod]
        public void AlternateReversesEveryOtherPass()
        {
            Assert.IsTrue(Valid("pulse 600ms alternate").Alternate);
            Assert.IsFalse(Valid("pulse 600ms normal").Alternate);
        }

        [TestMethod]
        public void TheExtrasComeInAnyOrder()
        {
            AnimationStyleDescriptor one = Valid("pulse 600ms ease-in-out 40ms infinite alternate");
            AnimationStyleDescriptor two = Valid("pulse 600ms alternate infinite 40ms ease-in-out");

            Assert.IsTrue(one.Matches(two), "a curve, a duration and a keyword all have different shapes");
        }

        [TestMethod]
        public void ANameAloneIsNotEnough()
        {
            Invalid("pulse");
        }

        [TestMethod]
        public void ADurationIsRequiredAndMustBePositive()
        {
            Invalid("pulse 0ms");
            Invalid("pulse wobble");
        }

        [TestMethod]
        public void TheNameMustLookLikeAName()
        {
            Invalid("600ms 600ms");
            Invalid("-pulse 600ms");
        }

        [TestMethod]
        public void AnUnknownExtraIsRejected()
        {
            Invalid("pulse 600ms sideways");
        }

        [TestMethod]
        public void AnEmptyValueIsRejected()
        {
            Invalid("");
            Invalid("   ");
        }

        [TestMethod]
        public void ItRoundTripsThroughGeneratedSource()
        {
            AnimationStyleDescriptor descriptor = Valid("pulse 600ms ease-out 40ms infinite alternate");

            Assert.IsTrue(descriptor.CanGenerateSource);

            string source = descriptor.ToSource();

            StringAssert.Contains(source, "\"pulse\"");
            StringAssert.Contains(source, "600");
            StringAssert.Contains(source, "EaseOut");
            StringAssert.Contains(source, "true");
        }

        [TestMethod]
        public void TheRegistryKnowsIt()
        {
            StyleDefinition definition = StyleDefinitions.Find(StyleIdentifier.ANIMATION);

            Assert.IsNotNull(definition);
            Assert.IsTrue(definition.Keywords.Count > 0, "completion needs the keywords");
        }
    }
}
