using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class MediaQueryTests
    {
        private static MediaQuery Parse(string source)
        {
            MediaQuery query = MediaQuery.Parse(source);

            Assert.IsNotNull(query, $"'{source}' should have parsed");

            return query;
        }

        [TestMethod]
        public void AMaxWidthMatchesUpToAndIncludingItsBound()
        {
            MediaQuery query = Parse("(max-width: 600px)");

            Assert.IsTrue(query.Matches(599, 800));
            Assert.IsTrue(query.Matches(600, 800), "the bound itself is inside, as in CSS");
            Assert.IsFalse(query.Matches(601, 800));
        }

        [TestMethod]
        public void AMinWidthMatchesFromItsBound()
        {
            MediaQuery query = Parse("(min-width: 600px)");

            Assert.IsFalse(query.Matches(599, 800));
            Assert.IsTrue(query.Matches(600, 800));
        }

        [TestMethod]
        public void HeightsWorkTheSameWay()
        {
            Assert.IsTrue(Parse("(max-height: 400px)").Matches(999, 400));
            Assert.IsFalse(Parse("(max-height: 400px)").Matches(999, 401));
            Assert.IsTrue(Parse("(min-height: 400px)").Matches(999, 400));
            Assert.IsFalse(Parse("(min-height: 400px)").Matches(999, 399));
        }

        [TestMethod]
        public void ClausesCombineWithAnd()
        {
            MediaQuery query = Parse("(min-width: 400px) and (max-width: 800px)");

            Assert.IsFalse(query.Matches(399, 100));
            Assert.IsTrue(query.Matches(400, 100));
            Assert.IsTrue(query.Matches(800, 100));
            Assert.IsFalse(query.Matches(801, 100));
        }

        [TestMethod]
        public void OrientationComparesTheTwoAxes()
        {
            Assert.IsTrue(Parse("(orientation: portrait)").Matches(400, 800));
            Assert.IsFalse(Parse("(orientation: portrait)").Matches(800, 400));

            Assert.IsTrue(Parse("(orientation: landscape)").Matches(800, 400));
            Assert.IsFalse(Parse("(orientation: landscape)").Matches(400, 800));
        }

        [TestMethod]
        public void ASquareViewportIsBothOrientations()
        {
            Assert.IsTrue(Parse("(orientation: portrait)").Matches(500, 500));
            Assert.IsTrue(Parse("(orientation: landscape)").Matches(500, 500),
                "neither axis is greater, so a square satisfies both rather than neither");
        }

        [TestMethod]
        public void TheParenthesesAreOptional()
        {
            Assert.IsTrue(Parse("max-width: 600px").Matches(500, 500));
            Assert.IsTrue(Parse("min-width:400px and max-width:800px").Matches(500, 500));
        }

        [TestMethod]
        public void TheUnitIsOptionalAndDecimalsWork()
        {
            Assert.IsTrue(Parse("(max-width: 600)").Matches(600, 100));
            Assert.IsTrue(Parse("(max-width: 600.5px)").Matches(600.5f, 100));
            Assert.IsFalse(Parse("(max-width: 600.5px)").Matches(600.6f, 100));
        }

        [TestMethod]
        public void TheFeatureNameIsCaseInsensitiveAndAndMayBeCapitalised()
        {
            Assert.IsTrue(Parse("(MAX-WIDTH: 600px) AND (min-width: 100px)").Matches(500, 500));
        }

        [TestMethod]
        public void NonsenseIsRejectedRatherThanIgnored()
        {
            Assert.IsNull(MediaQuery.Parse("(max-girth: 600px)"), "an unknown feature");
            Assert.IsNull(MediaQuery.Parse("(max-width: wide)"), "a non-numeric length");
            Assert.IsNull(MediaQuery.Parse("(max-width)"), "no value at all");
            Assert.IsNull(MediaQuery.Parse("(orientation: sideways)"), "an unknown orientation");
            Assert.IsNull(MediaQuery.Parse("(max-width: -10px)"), "a negative length");
            Assert.IsNull(MediaQuery.Parse(""), "nothing");
            Assert.IsNull(MediaQuery.Parse("   "), "whitespace");
        }

        [TestMethod]
        public void ContradictoryOrientationsAreRejected()
        {
            Assert.IsNull(MediaQuery.Parse("(orientation: portrait) and (orientation: landscape)"),
                "it could never match, so it is a mistake rather than a rule");
        }

        [TestMethod]
        public void NestingCombinesTheTighterBound()
        {
            MediaQuery outer = Parse("(max-width: 800px)");
            MediaQuery inner = Parse("(max-width: 600px)");

            MediaQuery combined = outer.And(inner);

            Assert.IsTrue(combined.Matches(600, 100));
            Assert.IsFalse(combined.Matches(700, 100),
                "the inner block cannot widen what the outer one already narrowed");
        }

        [TestMethod]
        public void NestingKeepsBothAxes()
        {
            MediaQuery combined = Parse("(max-width: 800px)").And(Parse("(min-height: 500px)"));

            Assert.IsTrue(combined.Matches(700, 600));
            Assert.IsFalse(combined.Matches(700, 400));
            Assert.IsFalse(combined.Matches(900, 600));
        }
    }
}
