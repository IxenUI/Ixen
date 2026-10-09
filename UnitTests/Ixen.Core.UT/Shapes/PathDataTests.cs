using Ixen.Core.Visual.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Shapes
{
    [TestClass]
    public class PathDataTests
    {
        private const float TOLERANCE = 0.01f;

        private static PathGeometry Parse(string data)
            => PathData.Parse(data, false);

        private static void AssertBounds(PathGeometry geometry,
            float left, float top, float width, float height, string message = null)
        {
            Assert.IsNotNull(geometry, message);
            Assert.AreEqual(left, geometry.Left, TOLERANCE, message);
            Assert.AreEqual(top, geometry.Top, TOLERANCE, message);
            Assert.AreEqual(width, geometry.Width, TOLERANCE, message);
            Assert.AreEqual(height, geometry.Height, TOLERANCE, message);
        }

        [TestMethod]
        public void ARectangleIsFourLinesAndAClose()
        {
            PathGeometry geometry = Parse("M 2 3 L 12 3 L 12 9 L 2 9 Z");

            AssertBounds(geometry, 2, 3, 10, 6);

            CollectionAssert.AreEqual(
                new[] { PathVerb.Move, PathVerb.Line, PathVerb.Line, PathVerb.Line, PathVerb.Close },
                geometry.Verbs);
        }

        [TestMethod]
        public void ALowercaseVerbIsRelativeToWhereWeAre()
        {
            AssertBounds(Parse("M 10 10 l 5 0 l 0 5 z"), 10, 10, 5, 5);
        }

        [TestMethod]
        public void AMovetoFollowedByMorePairsIsAnImplicitLineto()
        {
            PathGeometry geometry = Parse("M 0 0 4 0 4 4");

            AssertBounds(geometry, 0, 0, 4, 4);

            CollectionAssert.AreEqual(
                new[] { PathVerb.Move, PathVerb.Line, PathVerb.Line },
                geometry.Verbs);
        }

        [TestMethod]
        public void ARelativeMovetoIsFollowedByRelativeLinetos()
        {
            AssertBounds(Parse("M 1 1 m 1 1 2 0"), 1, 1, 3, 1,
                "the second pair of a relative moveto is a relative lineto, so it lands at 4,2");
        }

        [TestMethod]
        public void HorizontalAndVerticalKeepTheOtherAxis()
        {
            AssertBounds(Parse("M 3 7 H 11 V 2"), 3, 2, 8, 5);
        }

        [TestMethod]
        public void ARepeatedVerbTakesSeveralParameterSets()
        {
            PathGeometry geometry = Parse("M 0 0 L 1 1 2 2 3 3");

            Assert.AreEqual(4, geometry.Verbs.Length);
            AssertBounds(geometry, 0, 0, 3, 3);
        }

        [TestMethod]
        public void NumbersNeedNoSeparatorWhenTheirShapeSaysWhereTheyEnd()
        {
            AssertBounds(Parse("M0 0L-1-2"), -1, -2, 1, 2,
                "a minus starts a new number");

            AssertBounds(Parse("M0 0L1.5.5"), 0, 0, 1.5f, 0.5f,
                "and so does a second decimal point");
        }

        [TestMethod]
        public void AnExponentIsPartOfItsNumber()
        {
            AssertBounds(Parse("M 0 0 L 1e2 5"), 0, 0, 100, 5);
        }

        [TestMethod]
        public void ACurveIsBoundedByItsInkRatherThanByItsControlPoints()
        {
            PathGeometry geometry = Parse("M 0 0 C 0 10 10 10 10 0");

            AssertBounds(geometry, 0, 0, 10, 7.5f,
                "the control points reach y = 10 while the curve itself only reaches 7.5");
        }

        [TestMethod]
        public void AQuadraticIsBoundedByItsInkToo()
        {
            AssertBounds(Parse("M 0 0 Q 5 10 10 0"), 0, 0, 10, 5,
                "a quadratic peaks half way to its control point");
        }

        [TestMethod]
        public void ASmoothCubicReflectsThePreviousControlPoint()
        {
            PathGeometry geometry = Parse("M 0 0 C 0 -5 5 -5 5 0 S 10 5 10 0");

            Assert.AreEqual(3, geometry.Verbs.Length);
            AssertBounds(geometry, 0, -3.75f, 10, 7.5f,
                "the reflection makes the second curve the mirror of the first");
        }

        [TestMethod]
        public void TheReflectionIsOnBothAxesAndNotOnlyTheOneASymmetricCurveShows()
        {
            PathGeometry geometry = Parse("M 0 0 C 0 -5 2 -5 5 0 S 10 5 10 0");

            Assert.AreEqual(8f, geometry.Points[8], TOLERANCE,
                "the previous control was at x 2 and the current point at 5, so the mirror is 8");
            Assert.AreEqual(5f, geometry.Points[9], TOLERANCE,
                "and -5 mirrored about 0 is 5");
        }

        [TestMethod]
        public void ACurveWhoseCubicTermSurvivesIsBoundedByItsRootsToo()
        {
            PathGeometry geometry = Parse("M 0 0 C 0 20 10 -10 10 0");

            AssertBounds(geometry, 0, -2.104f, 10, 9.146f,
                "a curve that is not symmetric reaches its extremes where its derivative "
                + "vanishes, which takes the quadratic rather than the linear branch");
        }

        [TestMethod]
        public void ASmoothCubicWithNoCubicBeforeItStartsFlat()
        {
            PathGeometry geometry = Parse("M 0 0 S 10 10 10 0");

            Assert.AreEqual(2, geometry.Verbs.Length);
            Assert.AreEqual(0f, geometry.Points[2], TOLERANCE,
                "with nothing to reflect the first control point is the current point");
            Assert.AreEqual(0f, geometry.Points[3], TOLERANCE);
        }

        [TestMethod]
        public void ASmoothQuadraticReflectsToo()
        {
            PathGeometry geometry = Parse("M 0 0 Q 2.5 -5 5 0 T 10 0");

            Assert.AreEqual(3, geometry.Verbs.Length);
            AssertBounds(geometry, 0, -2.5f, 10, 5,
                "the reflected control point sends the second arch the other way");
        }

        [TestMethod]
        public void AnArcBecomesCurvesAndKeepsItsRadius()
        {
            PathGeometry geometry = Parse("M 0 10 A 10 10 0 0 1 10 0");

            AssertBounds(geometry, 0, 0, 10, 10,
                "a quarter circle of radius ten spans ten by ten");

            Assert.AreEqual(PathVerb.Cubic, geometry.Verbs[1]);
        }

        [TestMethod]
        public void AHalfCircleTakesMoreThanOneCurve()
        {
            PathGeometry geometry = Parse("M 0 0 A 5 5 0 1 1 0 10");

            AssertBounds(geometry, 0, 0, 5, 10);
            Assert.AreEqual(3, geometry.Verbs.Length,
                "a sweep of 180 degrees is split into two quarter-turn curves");
        }

        [TestMethod]
        public void TheSweepFlagDecidesWhichWayTheArcGoes()
        {
            AssertBounds(Parse("M 0 0 A 5 5 0 0 1 0 10"), 0, 0, 5, 10);
            AssertBounds(Parse("M 0 0 A 5 5 0 0 0 0 10"), -5, 0, 5, 10,
                "the other sweep bulges to the left instead");
        }

        [TestMethod]
        public void AnArcWithNoRadiusIsALine()
        {
            PathGeometry geometry = Parse("M 0 0 A 0 0 0 0 1 10 10");

            AssertBounds(geometry, 0, 0, 10, 10);
            Assert.AreEqual(PathVerb.Line, geometry.Verbs[1]);
        }

        [TestMethod]
        public void RadiiTooSmallForTheArcAreGrownRatherThanRefused()
        {
            AssertBounds(Parse("M 0 0 A 1 1 0 0 1 10 0"), 0, -5, 10, 5,
                "the radius is scaled up until the arc reaches its end point, so a chord of ten "
                + "bulges by five rather than by one");
        }

        [TestMethod]
        public void TheArcFlagsAreSingleCharactersWithNoSeparator()
        {
            AssertBounds(Parse("M0 10a10 10 0 0110-10"), 0, 0, 10, 10,
                "0011 is four flag characters, not a number");
        }

        [TestMethod]
        public void AVerbThatIsNotFedEnoughNumbersIsRefused()
        {
            Assert.IsNull(Parse("M 0 0 L 5"));
            Assert.IsNull(Parse("M 0"));
            Assert.IsNull(Parse("M 0 0 A 5 5 0 1 5 5"));
        }

        [TestMethod]
        public void NumbersWithNoVerbInFrontAreRefused()
        {
            Assert.IsNull(Parse("5 5 10 10"));
        }

        [TestMethod]
        public void SomethingThatIsNotAPathIsRefusedRatherThanPartlyRead()
        {
            Assert.IsNull(Parse("wobble"));
            Assert.IsNull(Parse("M 0 0 L 5 5 wobble"));
        }

        [TestMethod]
        public void NothingIsNothing()
        {
            Assert.IsNull(Parse(null));
            Assert.IsNull(Parse(string.Empty));
            Assert.IsNull(Parse("   "));
        }

        [TestMethod]
        public void TheFillRuleTravelsWithTheGeometry()
        {
            Assert.IsFalse(PathData.Parse("M 0 0 L 1 1", false).EvenOdd);
            Assert.IsTrue(PathData.Parse("M 0 0 L 1 1", true).EvenOdd);
        }

        [TestMethod]
        public void ClosingAndCarryingOnGoesBackToTheSubpathStart()
        {
            AssertBounds(Parse("M 5 5 L 10 5 Z l 0 5"), 5, 5, 5, 5,
                "after a close the current point is where the subpath began");
        }
    }
}
