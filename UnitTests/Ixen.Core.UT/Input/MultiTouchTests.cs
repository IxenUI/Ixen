using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class MultiTouchTests
    {
        private const float TOLERANCE = 0.001f;

        private VisualElement _root;
        private VisualElement _card;
        private VisualElement _left;
        private VisualElement _right;
        private VisualElement _page;
        private IxenSurface _surface;

        private readonly List<PinchEventArgs> _starts = new List<PinchEventArgs>();
        private readonly List<PinchEventArgs> _pinches = new List<PinchEventArgs>();
        private readonly List<PinchEventArgs> _ends = new List<PinchEventArgs>();

        [TestInitialize]
        public void Setup()
        {
            _root = Element("root", LayoutType.Row);

            _card = Element("card", LayoutType.Row);
            Size(_card, 200, 200);

            _left = Element("left", LayoutType.Column);
            Size(_left, 100, 200);

            _right = Element("right", LayoutType.Column);
            Size(_right, 100, 200);

            _card.AddChildren(_left, _right);

            _page = Element("page", LayoutType.Column);
            Size(_page, 200, 200);
            _page.Scrollable = true;

            for (int index = 0; index < 5; index++)
            {
                VisualElement row = Element("row" + index, LayoutType.Column);
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
                _page.AddChild(row);
            }

            _root.AddChildren(_card, _page);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(400, 200);
        }

        private static VisualElement Element(string name, LayoutType layout)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = layout };
            return element;
        }

        private static void Size(VisualElement element, float width, float height)
        {
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
        }

        private void Down(float x, float y, int id = 0)
            => _surface.PointerDown(x, y, PointerButton.Left, PointerKind.Touch, id);

        private void Move(float x, float y, int id = 0)
            => _surface.PointerMove(x, y, PointerKind.Touch, id);

        private void Up(float x, float y, int id = 0)
            => _surface.PointerUp(x, y, PointerButton.Left, PointerKind.Touch, id);

        private void Listen(VisualElement element, bool claim = true)
        {
            element.PointerPinchStart += (sender, args) =>
            {
                _starts.Add(args);
                args.Handled = claim;
            };

            element.PointerPinch += (sender, args) => _pinches.Add(args);
            element.PointerPinchEnd += (sender, args) => _ends.Add(args);
        }

        private PinchEventArgs LastPinch => _pinches[_pinches.Count - 1];

        [TestMethod]
        public void TwoFingersPinchWhatTheyBothTouched()
        {
            Listen(_root);

            Down(40, 100);
            Down(160, 100, 1);
            Move(60, 100);

            Assert.AreEqual(1, _starts.Count, "the gesture started once");
            Assert.AreSame(_card, _starts[0].Source,
                "the two fingers landed in two children, so the card is what they share");
            Assert.AreEqual(2, _starts[0].PointerCount, "two fingers");
        }

        [TestMethod]
        public void ASingleFingerNeverPinches()
        {
            Listen(_root);

            Down(40, 100);
            Move(60, 100);
            Move(80, 100);
            Up(80, 100);

            Assert.AreEqual(0, _starts.Count, "one finger is a drag, never a pinch");
        }

        [TestMethod]
        public void RestingTwoFingersIsNotAPinchUntilOneOfThemMoves()
        {
            Listen(_root);

            Down(70, 100);
            Down(130, 100, 1);
            Move(71, 100);
            Move(131, 100, 1);

            Assert.AreEqual(0, _starts.Count, "neither finger passed the threshold");

            Move(60, 100);

            Assert.AreEqual(1, _starts.Count, "that one did");
        }

        [TestMethod]
        public void SpreadingTwoFingersReportsTheScale()
        {
            Listen(_root);

            Down(70, 100);
            Down(130, 100, 1);

            Move(40, 100);
            Move(160, 100, 1);

            Assert.AreEqual(2f, LastPinch.Scale, TOLERANCE, "the span went from 60 to 120");
        }

        [TestMethod]
        public void PinchingTwoFingersTogetherReportsAScaleBelowOne()
        {
            Listen(_root);

            Down(40, 100);
            Down(160, 100, 1);

            Move(70, 100);
            Move(130, 100, 1);

            Assert.AreEqual(0.5f, LastPinch.Scale, TOLERANCE, "the span went from 120 to 60");
        }

        [TestMethod]
        public void TwistingTwoFingersReportsTheRotationAndLeavesTheScaleAlone()
        {
            Listen(_root);

            Down(100, 60);
            Down(100, 140, 1);

            Move(60, 100);
            Move(140, 100, 1);

            Assert.AreEqual(-90f, LastPinch.Rotation, 0.01f, "a quarter turn");
            Assert.AreEqual(1f, LastPinch.Scale, TOLERANCE,
                "both fingers stayed forty units from the centre");
        }

        [TestMethod]
        public void TheRotationSurvivesPastHalfATurn()
        {
            Listen(_root);

            Down(60, 100);
            Down(140, 100, 1);

            Move(100, 60);
            Move(100, 140, 1);
            Move(140, 100);
            Move(60, 100, 1);
            Move(100, 140);
            Move(100, 60, 1);

            Assert.AreEqual(270f, LastPinch.Rotation, 0.01f,
                "three quarter turns accumulate rather than wrapping");
        }

        [TestMethod]
        public void MovingBothFingersTogetherReportsTheTranslation()
        {
            Listen(_root);

            Down(70, 100);
            Down(130, 100, 1);

            Move(80, 110);
            Move(140, 110, 1);

            Assert.AreEqual(10f, LastPinch.TotalX, TOLERANCE, "the centroid moved ten right");
            Assert.AreEqual(10f, LastPinch.TotalY, TOLERANCE, "and ten down");
            Assert.AreEqual(5f, LastPinch.DeltaX, TOLERANCE, "five of them since the last event");
            Assert.AreEqual(5f, LastPinch.DeltaY, TOLERANCE);
            Assert.AreEqual(1f, LastPinch.Scale, TOLERANCE, "a translation is not a zoom");
        }

        [TestMethod]
        public void TheGestureIsCentredOnTheFingers()
        {
            Listen(_root);

            Down(70, 100);
            Down(130, 140, 1);
            Move(70, 108);

            Assert.AreEqual(100f, _starts[0].X, TOLERANCE, "halfway between the two");
            Assert.AreEqual(124f, _starts[0].Y, TOLERANCE);
        }

        [TestMethod]
        public void APinchNobodyClaimsIsNeverOfferedAgain()
        {
            Listen(_root, claim: false);

            Down(70, 100);
            Down(130, 100, 1);

            Move(40, 100);
            Move(160, 100, 1);
            Move(20, 100);

            Assert.AreEqual(1, _starts.Count, "offered once");
            Assert.AreEqual(0, _pinches.Count, "and refused, so nothing follows");
            Assert.AreEqual(0, _ends.Count, "a gesture that never started never ends");
        }

        [TestMethod]
        public void LiftingOneFingerEndsThePinch()
        {
            Listen(_root);

            Down(70, 100);
            Down(130, 100, 1);
            Move(40, 100);

            Assert.AreEqual(0, _ends.Count, "still going");

            Up(130, 100, 1);

            Assert.AreEqual(1, _ends.Count, "one finger cannot transform anything");
        }

        [TestMethod]
        public void TheEndCarriesTheScaleTheGestureReached()
        {
            Listen(_root);

            Down(70, 100);
            Down(130, 100, 1);
            Move(40, 100);
            Move(160, 100, 1);

            Up(160, 100, 1);

            Assert.AreEqual(1, _ends.Count);
            Assert.AreEqual(2f, _ends[0].Scale, TOLERANCE,
                "the accumulators are folded before the finger leaves the table");
        }

        [TestMethod]
        public void AddingAThirdFingerKeepsTheScaleItReached()
        {
            Listen(_root);

            Down(70, 100);
            Down(130, 100, 1);
            Move(40, 100);
            Move(160, 100, 1);

            Down(100, 180, 2);
            Move(100, 180, 2);

            Assert.AreEqual(2f, LastPinch.Scale, TOLERANCE,
                "a new base must not throw the zoom away");
            Assert.AreEqual(3, LastPinch.PointerCount);
        }

        [TestMethod]
        public void AThirdFingerCountsTowardsTheSpan()
        {
            Listen(_root);

            Down(70, 100);
            Down(130, 100, 1);
            Move(40, 100);

            Assert.AreEqual(1.5f, _starts[0].Scale, TOLERANCE, "the span went from 60 to 90");

            Down(100, 100, 2);
            Move(100, 100, 2);

            Assert.AreEqual(1.5f, LastPinch.Scale, TOLERANCE,
                "a finger that lands and stays changes nothing");

            Move(100, 200, 2);

            Assert.IsTrue(LastPinch.Scale > 2f,
                "the span is the mean distance from the centroid, so the third finger counts");
        }

        [TestMethod]
        public void LiftingOneOfThreeFingersKeepsTheGesture()
        {
            Listen(_root);

            Down(70, 100);
            Down(130, 100, 1);
            Down(100, 180, 2);
            Move(40, 100);

            Up(100, 180, 2);

            Assert.AreEqual(0, _ends.Count, "two fingers are still enough");

            Move(20, 100);

            Assert.AreEqual(2, LastPinch.PointerCount, "and the gesture carries on with them");
        }

        [TestMethod]
        public void AFingerThatLandedOnNothingRefusesThePinch()
        {
            Listen(_root);

            Down(40, 100);
            Down(700, 700, 1);
            Move(60, 100);

            Assert.AreEqual(0, _starts.Count, "there is no element the two fingers share");
        }

        [TestMethod]
        public void ASecondFingerPressesNothing()
        {
            int rightDowns = 0;
            int rightUps = 0;
            int rightClicks = 0;
            int leftClicks = 0;

            _right.PointerDown += (sender, args) => rightDowns++;
            _right.PointerUp += (sender, args) => rightUps++;
            _right.PointerClick += (sender, args) => rightClicks++;
            _left.PointerClick += (sender, args) => leftClicks++;

            Down(40, 100);
            Down(160, 100, 1);
            Up(160, 100, 1);
            Up(40, 100);

            Assert.AreEqual(0, rightDowns, "the second finger is not a press");
            Assert.AreEqual(0, rightUps);
            Assert.AreEqual(0, rightClicks);
            Assert.AreEqual(1, leftClicks, "the first one still clicks");
        }

        [TestMethod]
        public void NoNewPrimaryAppearsUntilEveryFingerIsUp()
        {
            int downs = 0;

            _left.PointerDown += (sender, args) => downs++;

            Down(40, 100);
            Down(160, 100, 1);
            Up(40, 100);

            Down(40, 100, 2);

            Assert.AreEqual(1, downs, "a finger is still on the glass, so the gesture is over");

            Up(160, 100, 1);
            Up(40, 100, 2);

            Down(40, 100);

            Assert.AreEqual(2, downs, "now it presses again");
        }

        [TestMethod]
        public void ASecondFingerDoesNotDisturbTheFocus()
        {
            _left.Focusable = true;
            _right.Focusable = true;

            Down(40, 100);

            Assert.AreSame(_left, _surface.FocusedElement, "the press focused what it pressed");

            _right.Focus();

            Down(160, 100, 1);

            Assert.AreSame(_right, _surface.FocusedElement,
                "a second finger does not re-run the focus decision");
        }

        [TestMethod]
        public void TwoFingersScrollByTheirCentroid()
        {
            Listen(_page, claim: false);

            Assert.IsTrue(_page.MaxScrollY > 0, "there is something to scroll");

            Down(300, 150);
            Down(340, 150, 1);

            Move(300, 100);
            Move(340, 100, 1);

            Assert.AreEqual(50f, _page.ScrollY, TOLERANCE,
                "the centroid moved fifty and the content followed it");
        }

        [TestMethod]
        public void AddingASecondFingerDoesNotJumpTheScroll()
        {
            Listen(_page, claim: false);

            Down(300, 150);
            Move(300, 100);

            Assert.AreEqual(50f, _page.ScrollY, TOLERANCE, "one finger scrolled fifty");

            Down(340, 150, 1);
            Move(300, 90);

            Assert.AreEqual(55f, _page.ScrollY, TOLERANCE,
                "the reference follows the centroid, so only the five extra units count");
        }

        [TestMethod]
        public void LiftingTheSecondFingerDoesNotJumpTheScrollEither()
        {
            Listen(_page, claim: false);

            Down(300, 150);
            Move(300, 100);
            Down(340, 150, 1);
            Move(300, 90);
            Up(340, 150, 1);

            Move(300, 80);

            Assert.AreEqual(65f, _page.ScrollY, TOLERANCE,
                "back to one finger, and the content did not lurch");
        }

        [TestMethod]
        public void AClaimedPinchStopsThePan()
        {
            Listen(_page);

            Down(300, 150);
            Move(300, 100);

            Assert.AreEqual(50f, _page.ScrollY, TOLERANCE);

            Down(340, 150, 1);
            Move(340, 100, 1);
            Move(340, 60, 1);

            Assert.AreEqual(1, _starts.Count, "the pinch was offered");
            Assert.AreEqual(50f, _page.ScrollY, TOLERANCE,
                "and claiming it takes the movement away from the scroll");
        }

        [TestMethod]
        public void AClaimedPinchNeverLetsTheScrollStart()
        {
            Listen(_page);

            Down(300, 150);
            Down(340, 150, 1);

            Move(300, 100);
            Move(340, 100, 1);

            Assert.AreEqual(1, _starts.Count, "the gesture was offered");
            Assert.AreEqual(0f, _page.ScrollY, TOLERANCE,
                "the recogniser runs before the drag, so the pan never even begins");
        }

        [TestMethod]
        public void AClaimedPinchReleasesThePress()
        {
            Listen(_root);

            Down(40, 100);

            Assert.AreSame(_left, _surface.PressedElement);

            Down(160, 100, 1);
            Move(60, 100);

            Assert.IsNull(_surface.PressedElement,
                "two fingers are a different gesture, so the press is over");
        }

        [TestMethod]
        public void APinchBubblesToAnAncestor()
        {
            Listen(_root);

            Down(40, 100);
            Down(160, 100, 1);
            Move(60, 100);

            Assert.AreEqual(1, _starts.Count, "the root heard it");
            Assert.AreSame(_card, _starts[0].Source, "while the source stays what was pinched");
        }

        [TestMethod]
        public void LosingTheCaptureEndsThePinch()
        {
            Listen(_root);

            Down(40, 100);
            Down(160, 100, 1);
            Move(60, 100);

            _surface.PointerCaptureLost();

            Assert.AreEqual(1, _ends.Count, "a stolen capture ends the gesture");
        }

        [TestMethod]
        public void LeavingTheSurfaceEndsThePinch()
        {
            Listen(_root);

            Down(40, 100);
            Down(160, 100, 1);
            Move(60, 100);

            _surface.PointerLeaveSurface();

            Assert.AreEqual(1, _ends.Count);
        }

        [TestMethod]
        public void DetachingWhatIsBeingPinchedEndsItSilently()
        {
            Listen(_card);

            Down(40, 100);
            Down(160, 100, 1);
            Move(60, 100);

            Assert.AreEqual(1, _starts.Count, "the card is what is being pinched");

            _root.RemoveChild(_card);

            Move(50, 100);
            Move(150, 100, 1);

            Assert.AreEqual(0, _ends.Count, "nothing is raised on the way out");
            Assert.AreEqual(0, _pinches.Count,
                "and an element out of the tree is not driven any further");
        }

        [TestMethod]
        public void AClaimedPinchCancelsTheLongPress()
        {
            var scheduler = new FakeScheduler();
            int presses = 0;

            _surface.Scheduler = scheduler;
            _left.PointerLongPress += (sender, args) => presses++;

            Listen(_root);

            Down(40, 100);

            Assert.AreEqual(1, scheduler.PendingCount, "the long press is armed");

            Down(160, 100, 1);
            Move(60, 100);

            Assert.AreEqual(0, scheduler.PendingCount, "and the pinch disarmed it");

            scheduler.FireAll();

            Assert.AreEqual(0, presses);
        }
    }
}
