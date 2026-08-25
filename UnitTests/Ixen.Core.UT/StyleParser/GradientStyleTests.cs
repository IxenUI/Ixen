using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class GradientStyleTests
    {
        private static BackgroundStyleDescriptor Parse(string value)
        {
            var xnsSource = new XnsSource($"box {{ background: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (BackgroundStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static Gradient Gradient(string value) => Parse(value).Gradient;

        private static void AssertRejected(string value)
        {
            var xnsSource = new XnsSource($"box {{ background: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'{value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void ACallWithSpacesInsteadOfCommasTokenizesAndParses()
        {
            Gradient gradient = Gradient("linear-gradient(to bottom #4C6EF5 #E8590C)");

            Assert.IsNotNull(gradient, "the value character set already allows parentheses and spaces");
            Assert.AreEqual(GradientKind.Linear, gradient.Kind);
            Assert.AreEqual(2, gradient.Stops.Count);
            Assert.AreEqual("#4C6EF5", gradient.Stops[0].Color);
            Assert.AreEqual("#E8590C", gradient.Stops[1].Color);
        }

        [TestMethod]
        public void TheFourSidesBecomeAngles()
        {
            Assert.AreEqual(180f, Gradient("linear-gradient(to bottom #000000 #FFFFFF)").Angle);
            Assert.AreEqual(0f, Gradient("linear-gradient(to top #000000 #FFFFFF)").Angle);
            Assert.AreEqual(90f, Gradient("linear-gradient(to right #000000 #FFFFFF)").Angle);
            Assert.AreEqual(270f, Gradient("linear-gradient(to left #000000 #FFFFFF)").Angle);
        }

        [TestMethod]
        public void TheFourDiagonalsWorkToo()
        {
            Assert.AreEqual(135f, Gradient("linear-gradient(to bottom right #000000 #FFFFFF)").Angle);
            Assert.AreEqual(225f, Gradient("linear-gradient(to bottom left #000000 #FFFFFF)").Angle);
            Assert.AreEqual(45f, Gradient("linear-gradient(to top right #000000 #FFFFFF)").Angle);
            Assert.AreEqual(315f, Gradient("linear-gradient(to top left #000000 #FFFFFF)").Angle);
        }

        [TestMethod]
        public void AnExplicitAngleIsAccepted()
        {
            Assert.AreEqual(45f, Gradient("linear-gradient(45deg #000000 #FFFFFF)").Angle);
            Assert.AreEqual(-30f, Gradient("linear-gradient(-30deg #000000 #FFFFFF)").Angle);
        }

        [TestMethod]
        public void TheDefaultDirectionIsDownwards()
        {
            Assert.AreEqual(180f, Gradient("linear-gradient(#000000 #FFFFFF)").Angle,
                "matching CSS, where a gradient with no direction runs to bottom");
        }

        [TestMethod]
        public void StopsAreReadAsPercentages()
        {
            Gradient gradient = Gradient("linear-gradient(to right #000000 0% #E8590C 25% #FFFFFF)");

            Assert.AreEqual(3, gradient.Stops.Count);
            Assert.AreEqual(0f, gradient.Stops[0].Offset);
            Assert.AreEqual(0.25f, gradient.Stops[1].Offset);
            Assert.IsFalse(gradient.Stops[2].HasOffset, "the last one was left to the even spread");
        }

        [TestMethod]
        public void MoreThanTwoColoursAreAllowed()
        {
            Assert.AreEqual(4, Gradient("linear-gradient(#000000 #FF0000 #00FF00 #FFFFFF)").Stops.Count);
        }

        [TestMethod]
        public void ARadialGradientNeedsNoDirection()
        {
            Gradient gradient = Gradient("radial-gradient(#FFFFFF #4C6EF5)");

            Assert.AreEqual(GradientKind.Radial, gradient.Kind);
            Assert.AreEqual(2, gradient.Stops.Count);
        }

        [TestMethod]
        public void ARadialGradientRefusesADirection()
        {
            AssertRejected("radial-gradient(to bottom #FFFFFF #4C6EF5)");
            AssertRejected("radial-gradient(45deg #FFFFFF #4C6EF5)");
        }

        [TestMethod]
        public void OneColourIsNotAGradient()
        {
            AssertRejected("linear-gradient(#4C6EF5)");
            AssertRejected("linear-gradient(to bottom)");
        }

        [TestMethod]
        public void StopsMustNotGoBackwards()
        {
            AssertRejected("linear-gradient(#000000 60% #FFFFFF 20%)");
        }

        [TestMethod]
        public void AStopOverAHundredIsRejected()
        {
            AssertRejected("linear-gradient(#000000 #FFFFFF 140%)");
        }

        [TestMethod]
        public void AStopBeforeAnyColourIsRejected()
        {
            AssertRejected("linear-gradient(20% #000000 #FFFFFF)");
        }

        [TestMethod]
        public void TwoDirectionsAreRejected()
        {
            AssertRejected("linear-gradient(45deg 90deg #000000 #FFFFFF)");
            AssertRejected("linear-gradient(to bottom 45deg #000000 #FFFFFF)");
        }

        [TestMethod]
        public void ADirectionAfterTheColoursIsRejected()
        {
            AssertRejected("linear-gradient(#000000 #FFFFFF to bottom)");
        }

        [TestMethod]
        public void AnUnknownWordIsRejected()
        {
            AssertRejected("linear-gradient(to sideways #000000 #FFFFFF)");
            AssertRejected("conic-gradient(#000000 #FFFFFF)");
        }

        [TestMethod]
        public void TwoGradientsAreRejected()
        {
            AssertRejected("linear-gradient(#000000 #FFFFFF) radial-gradient(#000000 #FFFFFF)");
        }

        [TestMethod]
        public void AGradientTakesNoRepeatFitOrPosition()
        {
            AssertRejected("linear-gradient(to right #000000 #333333) no-repeat");
            AssertRejected("linear-gradient(to right #000000 #333333) cover");
            AssertRejected("linear-gradient(to right #000000 #333333) bottom");
        }

        [TestMethod]
        public void AColourAndAGradientCanBothBeGiven()
        {
            BackgroundStyleDescriptor background =
                Parse("#FF0000 linear-gradient(to right #000000 #333333)");

            Assert.AreEqual("#FF0000", background.Color);
            Assert.IsNotNull(background.Gradient);
        }

        [TestMethod]
        public void TheOrdinaryBackgroundGrammarStillWorks()
        {
            BackgroundStyleDescriptor background = Parse("#FF0000 Assets/Images/logo.png repeat-x bottom");

            Assert.AreEqual("#FF0000", background.Color);
            Assert.AreEqual("Assets/Images/logo.png", background.ImageUrl);
            Assert.IsTrue(background.RepeatX);
            Assert.IsNull(background.Gradient,
                "the paren-aware split must not change how a value with no parentheses is read");
        }

        [TestMethod]
        public void AnUnbalancedParenthesisIsRejected()
        {
            AssertRejected("linear-gradient(to bottom #000000 #FFFFFF");
        }

        [TestMethod]
        public void ItRoundTripsThroughGeneratedSource()
        {
            string source = Parse("linear-gradient(45deg #4C6EF5 0% #E8590C 60% #FFFFFF)").ToSource();

            StringAssert.Contains(source, "GradientKind.Linear");
            StringAssert.Contains(source, "Angle = 45f");
            StringAssert.Contains(source, "#4C6EF5");
            StringAssert.Contains(source, "Offset = 0.6f");
        }
    }
}
