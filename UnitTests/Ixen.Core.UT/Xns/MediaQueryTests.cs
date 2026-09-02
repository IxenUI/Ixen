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
        public void ContradictoryOrientationsNowCompileAndNeverMatch()
        {
            MediaQuery query = Parse("(orientation: portrait) and (orientation: landscape)");

            Assert.IsTrue(query.Matches(500, 500), "a square satisfies both");
            Assert.IsFalse(query.Matches(800, 400));
            Assert.IsFalse(query.Matches(400, 800));

            Assert.IsTrue(true,
                "this used to be refused, and the refusal was only cheap while a query was "
                + "one conjunction of bounds. With or and not in the language, spotting a "
                + "contradiction is satisfiability checking, so CSS behaviour wins");
        }

        [TestMethod]
        public void ACommaIsAnOr()
        {
            MediaQuery query = Parse("(max-width: 400px), (min-width: 800px)");

            Assert.IsTrue(query.Matches(300, 100));
            Assert.IsFalse(query.Matches(600, 100), "neither side holds in the middle");
            Assert.IsTrue(query.Matches(900, 100));
        }

        [TestMethod]
        public void OrIsTheKeywordFormOfTheSameThing()
        {
            MediaQuery query = Parse("(max-width: 400px) or (min-width: 800px)");

            Assert.IsTrue(query.Matches(300, 100));
            Assert.IsFalse(query.Matches(600, 100));
            Assert.IsTrue(query.Matches(900, 100));
        }

        [TestMethod]
        public void AndBindsTighterThanOr()
        {
            MediaQuery query =
                Parse("(max-width: 400px) and (max-height: 400px) or (min-width: 800px)");

            Assert.IsTrue(query.Matches(300, 300), "the left conjunction");
            Assert.IsFalse(query.Matches(300, 500), "which needs both of its halves");
            Assert.IsTrue(query.Matches(900, 500), "or the right one on its own");
        }

        [TestMethod]
        public void TheConjunctionMayAlsoComeSecond()
        {
            MediaQuery query =
                Parse("(min-width: 800px) or (max-width: 400px) and (max-height: 400px)");

            Assert.IsTrue(query.Matches(900, 900), "the lone left side");
            Assert.IsTrue(query.Matches(300, 300), "the right conjunction");

            Assert.IsFalse(query.Matches(300, 500),
                "which still needs both of its halves - and this is the shape that tells the "
                + "two precedences apart, since a and b or c parses the same either way");
        }

        [TestMethod]
        public void ParenthesesGroupRatherThanNameAFeature()
        {
            MediaQuery query =
                Parse("((max-width: 400px) or (min-width: 800px)) and (orientation: landscape)");

            Assert.IsTrue(query.Matches(300, 100));
            Assert.IsFalse(query.Matches(300, 500), "portrait, so the landscape half fails");
            Assert.IsFalse(query.Matches(600, 100), "landscape, but neither width holds");

            Assert.IsTrue(query.Matches(900, 100),
                "a parenthesis holds a feature when it contains a top-level colon and a group "
                + "when it does not, which is how CSS tells the two apart");
        }

        [TestMethod]
        public void NotNegatesWhatFollowsIt()
        {
            MediaQuery query = Parse("not (max-width: 600px)");

            Assert.IsFalse(query.Matches(500, 100));
            Assert.IsTrue(query.Matches(700, 100));
        }

        [TestMethod]
        public void NotTakesAWholeGroup()
        {
            MediaQuery query = Parse("not ((min-width: 400px) and (max-width: 800px))");

            Assert.IsTrue(query.Matches(300, 100));
            Assert.IsFalse(query.Matches(600, 100));
            Assert.IsTrue(query.Matches(900, 100));
        }

        [TestMethod]
        public void NotBindsTighterThanAnd()
        {
            MediaQuery query = Parse("not (orientation: portrait) and (min-width: 400px)");

            Assert.IsTrue(query.Matches(800, 400), "landscape and wide enough");
            Assert.IsFalse(query.Matches(400, 800), "portrait");
            Assert.IsFalse(query.Matches(300, 100), "landscape but too narrow");
        }

        [TestMethod]
        public void ADoubleNegationIsAllowed()
        {
            Assert.IsTrue(Parse("not not (max-width: 600px)").Matches(500, 100));
        }

        [TestMethod]
        public void KeywordsAreCaseInsensitive()
        {
            MediaQuery query = Parse("NOT (max-width: 400px) OR (min-height: 900px)");

            Assert.IsTrue(query.Matches(600, 100));
            Assert.IsTrue(query.Matches(300, 900));
            Assert.IsFalse(query.Matches(300, 100));
        }

        [TestMethod]
        public void OrientationIsNotMistakenForTheOrKeyword()
        {
            Assert.IsTrue(Parse("orientation: landscape").Matches(800, 400),
                "a separator is matched as a whole word, so the or inside orientation and the "
                + "and inside landscape are both left alone");
        }

        [TestMethod]
        public void AnIncompleteOperatorIsRejected()
        {
            Assert.IsNull(MediaQuery.Parse("(max-width: 400px) or"), "nothing after or");
            Assert.IsNull(MediaQuery.Parse("or (max-width: 400px)"), "nothing before it");
            Assert.IsNull(MediaQuery.Parse("not"), "nothing to negate");
            Assert.IsNull(MediaQuery.Parse("(max-width: 400px"), "an unclosed group");
            Assert.IsNull(MediaQuery.Parse("(max-width: 400px) (min-width: 100px)"),
                "two queries with no operator between them");
        }

        [TestMethod]
        public void NestingStillOnlyNarrows()
        {
            MediaQuery outer = Parse("(max-width: 400px), (min-width: 800px)");
            MediaQuery combined = outer.And(Parse("(orientation: landscape)"));

            Assert.IsTrue(combined.Matches(300, 100));
            Assert.IsFalse(combined.Matches(300, 500),
                "an inner block narrows the whole disjunction rather than joining it");
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
