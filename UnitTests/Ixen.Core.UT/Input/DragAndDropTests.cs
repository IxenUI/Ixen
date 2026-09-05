using Ixen.Core.Input;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class DragAndDropTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private VisualElement _source;
        private VisualElement _zone;
        private VisualElement _label;
        private IxenSurface _surface;
        private List<string> _log;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();

            _root = Box("root", 0, 0, VIEWPORT, VIEWPORT);
            _source = Box("source", 0, 0, 60, 60);
            _zone = Box("zone", 100, 0, 60, 60);
            _label = Box("label", 0, 0, 40, 40);

            _zone.AllowDrop = true;
            _zone.AddChild(_label);
            _root.AddChildren(_source, _zone);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Watch(_zone, "zone");
        }

        private static VisualElement Box(string name, float x, float y, float width, float height)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            element.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = x };
            element.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = y };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        private void Watch(VisualElement element, string tag)
        {
            element.DragEnter += (s, e) => _log.Add($"enter:{tag}");
            element.DragOver += (s, e) => _log.Add($"over:{tag}");
            element.DragLeave += (s, e) => _log.Add($"leave:{tag}");
            element.Drop += (s, e) => _log.Add($"drop:{tag}:{e.Data}");
        }

        private void Offers(object payload)
            => _source.PointerDragStart += (s, e) => e.Data = payload;

        private string Log => string.Join(" ", _log);

        private void Press() => _surface.PointerDown(20, 20, PointerButton.Left);

        private void MoveTo(float x, float y) => _surface.PointerMove(x, y);

        private void Release(float x, float y) => _surface.PointerUp(x, y, PointerButton.Left);

        private void TrackStates()
        {
            var xnsSource = new XnsSource("zone {\r\n    background: #111111\r\n}\r\n"
                + "zone:dragover {\r\n    background: #222222\r\n}");

            ClassesSet set = xnsSource.Compile();
            var registry = new StyleRegistry();

            registry.Add(set);

            _surface.Styles = registry;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        [TestMethod]
        public void ADragThatOffersNothingIsStillJustADrag()
        {
            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            Release(120, 20);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void OfferingAPayloadTurnsTheDragIntoADropGesture()
        {
            Offers("poem");

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);

            Assert.AreEqual("enter:zone", Log);
        }

        [TestMethod]
        public void TheTargetIsTheNearestAncestorThatAllowsIt()
        {
            Offers("poem");
            Watch(_label, "label");

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            Release(120, 20);

            Assert.AreEqual("enter:zone drop:zone:poem", Log);
        }

        [TestMethod]
        public void ADragThatBeginsOverTheTargetEntersItAtOnce()
        {
            _label.PointerDragStart += (s, e) => e.Data = "poem";

            _surface.PointerDown(120, 20, PointerButton.Left);
            _surface.PointerMove(130, 20);
            _surface.PointerUp(130, 20, PointerButton.Left);

            Assert.AreEqual("enter:zone drop:zone:poem", Log);
        }

        [TestMethod]
        public void MovingWithinTheTargetRaisesOverRatherThanASecondEnter()
        {
            Offers("poem");

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            MoveTo(130, 30);
            MoveTo(140, 40);

            Assert.AreEqual("enter:zone over:zone over:zone", Log);
        }

        [TestMethod]
        public void LeavingTheTargetRaisesLeaveAndNothingElse()
        {
            Offers("poem");

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            MoveTo(180, 120);
            Release(180, 120);

            Assert.AreEqual("enter:zone leave:zone", Log);
        }

        [TestMethod]
        public void ADropCarriesThePayloadTheSourceOffered()
        {
            Offers(42);

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            Release(120, 20);

            Assert.AreEqual("enter:zone drop:zone:42", Log);
        }

        [TestMethod]
        public void ReleasingOverNoTargetDropsNothing()
        {
            Offers("poem");

            Press();
            MoveTo(30, 20);
            Release(30, 20);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void ATargetThatRefusesGetsNoDrop()
        {
            Offers("poem");
            _zone.DragEnter += (s, e) => e.Accepted = false;

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            Release(120, 20);

            Assert.AreEqual("enter:zone leave:zone", Log);
        }

        [TestMethod]
        public void ATargetMayChangeItsMindWhileTheDragMoves()
        {
            Offers("poem");
            _zone.DragOver += (s, e) => e.Accepted = e.X < 130;

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            MoveTo(150, 20);
            Release(150, 20);

            Assert.AreEqual("enter:zone over:zone leave:zone", Log);
        }

        [TestMethod]
        public void TheSourceLearnsThatTheDropHappened()
        {
            bool? accepted = null;

            Offers("poem");
            _source.PointerDragEnd += (s, e) => accepted = e.Accepted;

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            Release(120, 20);

            Assert.AreEqual(true, accepted);
        }

        [TestMethod]
        public void TheSourceLearnsThatItDidNot()
        {
            bool? accepted = null;

            Offers("poem");
            _source.PointerDragEnd += (s, e) => accepted = e.Accepted;

            Press();
            MoveTo(30, 20);
            MoveTo(180, 120);
            Release(180, 120);

            Assert.AreEqual(false, accepted);
        }

        [TestMethod]
        public void TheEndOfTheDragStillCarriesThePayload()
        {
            object carried = null;

            Offers("poem");
            _source.PointerDragEnd += (s, e) => carried = e.Data;

            Press();
            MoveTo(30, 20);
            Release(30, 20);

            Assert.AreEqual("poem", carried);
        }

        [TestMethod]
        public void APayloadDoesNotSurviveIntoTheNextDrag()
        {
            bool offering = true;

            _source.PointerDragStart += (s, e) =>
            {
                if (offering)
                {
                    e.Data = "poem";
                }
            };

            Press();
            MoveTo(30, 20);
            MoveTo(180, 120);
            Release(180, 120);

            offering = false;

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            Release(120, 20);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void TheTargetCarriesTheDragoverStateWhileItIsUnderThePointer()
        {
            TrackStates();
            Offers("poem");

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);

            Assert.IsTrue(_zone.HasState("dragover"));

            MoveTo(180, 120);

            Assert.IsFalse(_zone.HasState("dragover"));
        }

        [TestMethod]
        public void ATargetThatRefusesCarriesNoDragoverState()
        {
            TrackStates();
            Offers("poem");
            _zone.DragEnter += (s, e) => e.Accepted = false;

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);

            Assert.IsFalse(_zone.HasState("dragover"));
        }

        [TestMethod]
        public void ADropClearsTheDragoverState()
        {
            TrackStates();
            Offers("poem");

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);
            Release(120, 20);

            Assert.IsFalse(_zone.HasState("dragover"));
        }

        [TestMethod]
        public void ALostCaptureLeavesTheTargetAndDropsNothing()
        {
            Offers("poem");

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);

            _surface.PointerCaptureLost();

            Assert.AreEqual("enter:zone leave:zone", Log);
        }

        [TestMethod]
        public void DetachingTheTargetMidDragDropsNothing()
        {
            Offers("poem");

            Press();
            MoveTo(30, 20);
            MoveTo(120, 20);

            _root.RemoveChild(_zone);

            Release(120, 20);

            Assert.AreEqual("enter:zone", Log);
        }

        private static VisualElement ScrollingList(out IxenSurface surface)
        {
            var root = new VisualElement { Name = "root" };
            var viewport = new VisualElement { Name = "viewport" };

            viewport.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            viewport.Scrollable = true;

            for (int index = 0; index < 5; index++)
            {
                var row = new VisualElement { Name = $"row{index}" };

                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
                viewport.AddChild(row);
            }

            root.AddChild(viewport);

            surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return viewport;
        }

        private static void SwipeUp(IxenSurface surface)
        {
            surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);
            surface.PointerMove(20, 20, PointerKind.Touch);
            surface.PointerUp(20, 20, PointerButton.Left, PointerKind.Touch);
        }

        [TestMethod]
        public void ATouchDragThatOffersNothingStillScrolls()
        {
            VisualElement viewport = ScrollingList(out IxenSurface surface);

            SwipeUp(surface);

            Assert.IsTrue(viewport.ScrollY > 0f, $"expected a scroll, got {viewport.ScrollY}");
        }

        [TestMethod]
        public void APayloadClaimsTheGestureSoATouchDragDoesNotScroll()
        {
            VisualElement viewport = ScrollingList(out IxenSurface surface);

            viewport.PointerDragStart += (s, e) => e.Data = "row";

            SwipeUp(surface);

            Assert.AreEqual(0f, viewport.ScrollY);
        }
    }
}
