using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class ContextRequestedTests
    {
        private const int VIEWPORT = 300;

        private List<string> _log;
        private FakeScheduler _scheduler;
        private VisualElement _root;
        private VisualElement _card;
        private VisualElement _cover;
        private VisualElement _other;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();
            _scheduler = new FakeScheduler();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _card = new VisualElement { Name = "card", Focusable = true };
            _card.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _card.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            _cover = new VisualElement { Name = "cover" };
            _cover.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            _cover.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            _card.AddChild(_cover);

            _other = new VisualElement { Name = "other", Focusable = true };
            _other.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _other.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };

            _root.AddChildren(_card, _other);

            _card.ContextRequested += (sender, e) =>
            {
                _log.Add($"menu({e.X},{e.Y},{e.Kind},{(e.FromKeyboard ? "key" : "pointer")},{e.Source.Name})");
                e.Handled = true;
            };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = _scheduler };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private string Log => string.Join(" ", _log);

        [TestMethod]
        public void ARightClickAsksForTheMenuWhereItHappened()
        {
            _card.PointerClick += (sender, e) => _log.Add("click");

            _surface.PointerDown(10, 20, PointerButton.Right);
            _surface.PointerUp(10, 20, PointerButton.Right);

            Assert.AreEqual("menu(10,20,Mouse,pointer,cover)", Log, "it bubbles from the cover to the card, and is no click");
        }

        [TestMethod]
        public void ARightPressReleasedElsewhereAsksForNothing()
        {
            _surface.PointerDown(10, 20, PointerButton.Right);
            _surface.PointerUp(10, 130, PointerButton.Right);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void ALeftClickAsksForNothing()
        {
            _surface.PointerDown(10, 20, PointerButton.Left);
            _surface.PointerUp(10, 20, PointerButton.Left);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void AFingerHeldStillAsksForTheMenuAndDoesNotClick()
        {
            _card.PointerClick += (sender, e) => _log.Add("click");

            _surface.PointerDown(10, 20, PointerButton.Left, PointerKind.Touch);
            _scheduler.FireAll();
            _surface.PointerUp(10, 20, PointerButton.Left, PointerKind.Touch);

            Assert.AreEqual("menu(10,20,Touch,pointer,cover)", Log);
        }

        [TestMethod]
        public void AMouseHeldStillIsNotAMenu()
        {
            _surface.PointerDown(10, 20, PointerButton.Left);
            _scheduler.FireAll();
            _surface.PointerUp(10, 20, PointerButton.Left);

            Assert.AreEqual(string.Empty, Log, "a mouse has its right button");
        }

        [TestMethod]
        public void AHandledLongPressKeepsTheMenuClosed()
        {
            _cover.PointerLongPress += (sender, e) =>
            {
                _log.Add("long");
                e.Handled = true;
            };

            _surface.PointerDown(10, 20, PointerButton.Left, PointerKind.Touch);
            _scheduler.FireAll();

            Assert.AreEqual("long", Log);
        }

        [TestMethod]
        public void TheMenuKeyAndShiftF10AskOnTheFocusedElement()
        {
            _surface.Focus(_card);

            Assert.IsTrue(_surface.KeyDown(Key.ContextMenu, KeyModifiers.None));
            Assert.IsTrue(_surface.KeyDown(Key.F10, KeyModifiers.Shift));

            Assert.AreEqual("menu(50,50,Mouse,key,card) menu(50,50,Mouse,key,card)", Log,
                "from the keyboard the menu opens at the centre of what has the focus");
        }

        [TestMethod]
        public void F10AloneOrAMenuKeyNobodyAnswersIsNotHandled()
        {
            _surface.Focus(_card);

            Assert.IsFalse(_surface.KeyDown(Key.F10, KeyModifiers.None));

            _surface.Focus(_other);

            Assert.IsFalse(_surface.KeyDown(Key.ContextMenu, KeyModifiers.None));
            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void AScreenReaderCanAskForTheMenu()
        {
            AccessibleNode card = Find(_surface.BuildAccessibilityTree(), _card);
            AccessibleNode other = Find(_surface.BuildAccessibilityTree(), _other);

            Assert.IsTrue(card.Supports(AccessibleActions.ShowMenu));
            Assert.IsFalse(other.Supports(AccessibleActions.ShowMenu), "only what listens offers it");
            Assert.IsTrue(_surface.Perform(card, AccessibleActions.ShowMenu));
            Assert.AreEqual("menu(50,50,Mouse,key,card)", Log);
        }

        [TestMethod]
        public void AnElementThatOnlyHasAMenuIsStillInTheTree()
        {
            var caption = new VisualElement { Name = "caption", Text = "Track 1" };
            caption.ContextRequested += (sender, e) => e.Handled = true;
            _root.AddChild(caption);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            AccessibleNode node = Find(_surface.BuildAccessibilityTree(), caption);

            Assert.IsNotNull(node, "pruned as decoration, its menu would be out of a screen reader's reach");
            Assert.IsTrue(node.Supports(AccessibleActions.ShowMenu));
        }

        private static AccessibleNode Find(AccessibleNode node, VisualElement element)
        {
            if (node.Element == element)
            {
                return node;
            }

            foreach (AccessibleNode child in node.Children)
            {
                AccessibleNode found = Find(child, element);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
