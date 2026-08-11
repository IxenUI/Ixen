using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class PointerEventsTests
    {
        private const int VIEWPORT = 200;

        private List<string> _log;

        [TestInitialize]
        public void Setup() => _log = new List<string>();

        private static VisualElement Element(string name, LayoutType layout = LayoutType.Column,
            SizeUnit widthUnit = SizeUnit.Unset, float widthValue = 1,
            SizeUnit heightUnit = SizeUnit.Unset, float heightValue = 1)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = layout };
            element.Styles.Width = new WidthStyleDescriptor { Unit = widthUnit, Value = widthValue };
            element.Styles.Height = new HeightStyleDescriptor { Unit = heightUnit, Value = heightValue };
            return element;
        }

        private static VisualElement Box(string name, float width, float height)
            => Element(name, LayoutType.Column, SizeUnit.Pixels, width, SizeUnit.Pixels, height);

        private static IxenSurface Laid(VisualElement root)
        {
            var surface = new IxenSurface(root);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return surface;
        }

        private void Watch(VisualElement element, string tag)
        {
            element.PointerDown += (s, e) => _log.Add($"down:{tag}");
            element.PointerUp += (s, e) => _log.Add($"up:{tag}");
            element.PointerClick += (s, e) => _log.Add($"click:{tag}");
            element.PointerEnter += (s, e) => _log.Add($"enter:{tag}");
            element.PointerLeave += (s, e) => _log.Add($"leave:{tag}");
        }

        private string Log => string.Join(" ", _log);

        [TestMethod]
        public void ADownReachesTheElementUnderThePointer()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 60, 60);
            root.AddChild(box);
            Watch(box, "box");

            Laid(root).PointerDown(30, 30, PointerButton.Left);

            Assert.AreEqual("enter:box down:box", Log);
        }

        [TestMethod]
        public void AnEventBubblesToTheAncestors()
        {
            VisualElement root = Element("root");
            VisualElement middle = Box("middle", 100, 100);
            VisualElement leaf = Box("leaf", 40, 40);
            middle.AddChild(leaf);
            root.AddChild(middle);

            leaf.PointerDown += (s, e) => _log.Add("leaf");
            middle.PointerDown += (s, e) => _log.Add("middle");
            root.PointerDown += (s, e) => _log.Add("root");

            Laid(root).PointerDown(20, 20, PointerButton.Left);

            Assert.AreEqual("leaf middle root", Log, "bubbling goes from the hit element outwards");
        }

        [TestMethod]
        public void HandledStopsTheBubbling()
        {
            VisualElement root = Element("root");
            VisualElement middle = Box("middle", 100, 100);
            VisualElement leaf = Box("leaf", 40, 40);
            middle.AddChild(leaf);
            root.AddChild(middle);

            leaf.PointerDown += (s, e) => { _log.Add("leaf"); e.Handled = true; };
            middle.PointerDown += (s, e) => _log.Add("middle");
            root.PointerDown += (s, e) => _log.Add("root");

            Laid(root).PointerDown(20, 20, PointerButton.Left);

            Assert.AreEqual("leaf", Log);
        }

        [TestMethod]
        public void TheSourceIsTheHitElementEvenWhileBubbling()
        {
            VisualElement root = Element("root");
            VisualElement leaf = Box("leaf", 40, 40);
            root.AddChild(leaf);

            VisualElement seenSource = null;
            object seenSender = null;

            root.PointerDown += (s, e) => { seenSource = e.Source; seenSender = s; };

            Laid(root).PointerDown(20, 20, PointerButton.Left);

            Assert.AreSame(leaf, seenSource, "Source stays the element that was hit");
            Assert.AreSame(root, seenSender, "sender is the element the handler is attached to");
        }

        [TestMethod]
        public void TheButtonAndPositionAreCarried()
        {
            VisualElement root = Element("root");
            PointerEventArgs seen = null;
            root.PointerDown += (s, e) => seen = e;

            Laid(root).PointerDown(12, 34, PointerButton.Right);

            Assert.AreEqual(12f, seen.X);
            Assert.AreEqual(34f, seen.Y);
            Assert.AreEqual(PointerButton.Right, seen.Button);
        }

        [TestMethod]
        public void ADownThenUpOnTheSameElementIsAClick()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 60, 60);
            root.AddChild(box);
            Watch(box, "box");

            IxenSurface surface = Laid(root);
            surface.PointerDown(30, 30, PointerButton.Left);
            _log.Clear();
            surface.PointerUp(30, 30, PointerButton.Left);

            Assert.AreEqual("up:box click:box", Log, "the click comes after the up");
        }

        [TestMethod]
        public void AnUpElsewhereIsNotAClick()
        {
            VisualElement root = Element("root", LayoutType.Row);
            VisualElement first = Box("first", 60, 60);
            VisualElement second = Box("second", 60, 60);
            root.AddChildren(first, second);
            Watch(first, "first");
            Watch(second, "second");

            IxenSurface surface = Laid(root);
            surface.PointerDown(30, 30, PointerButton.Left);
            _log.Clear();
            surface.PointerUp(90, 30, PointerButton.Left);

            Assert.IsFalse(Log.Contains("click"), $"no click expected, got: {Log}");
        }

        [TestMethod]
        public void AClickBubbles()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 60, 60);
            root.AddChild(box);

            box.PointerClick += (s, e) => _log.Add("box");
            root.PointerClick += (s, e) => _log.Add("root");

            IxenSurface surface = Laid(root);
            surface.PointerDown(30, 30, PointerButton.Left);
            surface.PointerUp(30, 30, PointerButton.Left);

            Assert.AreEqual("box root", Log);
        }

        [TestMethod]
        public void MovingInAndOutRaisesEnterThenLeave()
        {
            VisualElement root = Element("root", LayoutType.Row);
            VisualElement box = Box("box", 60, 60);
            VisualElement other = Box("other", 60, 60);
            root.AddChildren(box, other);
            Watch(box, "box");

            IxenSurface surface = Laid(root);

            surface.PointerMove(30, 30);
            surface.PointerMove(90, 30);

            Assert.AreEqual("enter:box leave:box", Log);
        }

        [TestMethod]
        public void MovingWithinTheSameElementDoesNotRepeatEnter()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 100, 100);
            root.AddChild(box);
            Watch(box, "box");

            IxenSurface surface = Laid(root);

            surface.PointerMove(10, 10);
            surface.PointerMove(20, 20);
            surface.PointerMove(30, 30);

            Assert.AreEqual("enter:box", Log);
        }

        [TestMethod]
        public void EnterAndLeaveReachEveryAncestorCrossed()
        {
            VisualElement root = Element("root", LayoutType.Row);
            VisualElement card = Box("card", 100, 100);
            VisualElement label = Box("label", 40, 40);
            card.AddChild(label);
            VisualElement outside = Box("outside", 60, 60);
            root.AddChildren(card, outside);

            Watch(root, "root");
            Watch(card, "card");
            Watch(label, "label");

            IxenSurface surface = Laid(root);

            surface.PointerMove(20, 20);

            Assert.AreEqual("enter:root enter:card enter:label", Log,
                "entering goes outermost first");

            _log.Clear();
            surface.PointerMove(150, 150);

            Assert.AreEqual("leave:label leave:card", Log,
                "only the elements actually left are notified, deepest first");
        }

        [TestMethod]
        public void TheCommonAncestorIsNotLeftAndReEntered()
        {
            VisualElement root = Element("root");
            VisualElement card = Element("card", LayoutType.Row, SizeUnit.Pixels, 120, SizeUnit.Pixels, 60);
            VisualElement left = Box("left", 60, 60);
            VisualElement right = Box("right", 60, 60);
            card.AddChildren(left, right);
            root.AddChild(card);

            Watch(card, "card");
            Watch(left, "left");
            Watch(right, "right");

            IxenSurface surface = Laid(root);

            surface.PointerMove(30, 30);
            _log.Clear();
            surface.PointerMove(90, 30);

            Assert.AreEqual("leave:left enter:right", Log, "the shared card is untouched");
        }

        [TestMethod]
        public void LeavingTheSurfaceLeavesEverything()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 60, 60);
            root.AddChild(box);
            Watch(root, "root");
            Watch(box, "box");

            IxenSurface surface = Laid(root);

            surface.PointerMove(30, 30);
            _log.Clear();
            surface.PointerMove(-5, -5);

            Assert.AreEqual("leave:box leave:root", Log);
            Assert.IsNull(surface.HoveredElement);
        }

        [TestMethod]
        public void AMoveBubblesLikeTheOthers()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 60, 60);
            root.AddChild(box);

            box.PointerMove += (s, e) => _log.Add("box");
            root.PointerMove += (s, e) => _log.Add("root");

            Laid(root).PointerMove(30, 30);

            Assert.AreEqual("box root", Log);
        }

        [TestMethod]
        public void ARoundedCornerFallsThroughToTheParent()
        {
            VisualElement root = Element("root");
            VisualElement card = Box("card", 100, 100);
            card.Styles.CornerRadius = new CornerRadiusStyleDescriptor { TopLeft = 30 };
            root.AddChild(card);

            card.PointerDown += (s, e) => _log.Add("card");
            root.PointerDown += (s, e) => _log.Add("root");

            Laid(root).PointerDown(2, 2, PointerButton.Left);

            Assert.AreEqual("root", Log, "the cut corner belongs to the parent");
        }

        [TestMethod]
        public void AHandlerCanRestyleAndTheNextLayoutPicksItUp()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 60, 60);
            root.AddChild(box);

            box.PointerClick += (s, e) =>
            {
                box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
                box.Invalidate();
            };

            IxenSurface surface = Laid(root);
            surface.PointerDown(30, 30, PointerButton.Left);
            surface.PointerUp(30, 30, PointerButton.Left);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(120f, box.Width, "mutating styles from a handler works");
        }

        [TestMethod]
        public void LeavingTheSurfaceClearsTheHover()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 60, 60);
            root.AddChild(box);
            Watch(box, "box");

            IxenSurface surface = Laid(root);

            surface.PointerMove(30, 30);
            _log.Clear();

            surface.PointerLeaveSurface();

            Assert.AreEqual("leave:box", Log);
            Assert.IsNull(surface.HoveredElement);
        }

        [TestMethod]
        public void TheSurfaceReportsWhetherAHandlerDirtiedTheTree()
        {
            VisualElement root = Element("root");
            VisualElement box = Box("box", 60, 60);
            root.AddChild(box);

            IxenSurface surface = Laid(root);

            Assert.IsFalse(surface.IsDirty, "a fresh layout is clean");

            surface.PointerMove(30, 30);

            Assert.IsFalse(surface.IsDirty, "a move with no handler changes nothing");

            box.PointerClick += (s, e) => box.Text = "changed";

            surface.PointerDown(30, 30, PointerButton.Left);
            surface.PointerUp(30, 30, PointerButton.Left);

            Assert.IsTrue(surface.IsDirty, "the host uses this to decide whether to repaint");
        }

        [TestMethod]
        public void AnElementCanBeFoundByName()
        {
            VisualElement root = Element("root");
            VisualElement card = Box("card", 100, 100);
            VisualElement label = Box("label", 40, 40);
            card.AddChild(label);
            root.AddChild(card);

            Assert.AreSame(label, root.FindByName("label"));
            Assert.AreSame(card, root.FindByName("card"));
            Assert.AreSame(root, root.FindByName("root"));
            Assert.IsNull(root.FindByName("nope"));
            Assert.IsNull(root.FindByName(null));
        }

        [TestMethod]
        public void APointerOnNothingRaisesNothing()
        {
            VisualElement root = Element("root");
            Watch(root, "root");

            IxenSurface surface = Laid(root);
            surface.PointerDown(-10, -10, PointerButton.Left);
            surface.PointerUp(-10, -10, PointerButton.Left);

            Assert.AreEqual(string.Empty, Log);
        }
    }
}
