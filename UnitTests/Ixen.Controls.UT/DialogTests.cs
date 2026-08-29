using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class DialogTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private Button _behind;
        private Dialog _dialog;
        private Button _ok;
        private Button _cancel;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _behind = new Button { Name = "behind", Text = "Delete" };
            _behind.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 90 };
            _behind.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _dialog = new Dialog { Name = "confirm", Title = "Delete the file?" };

            _ok = new Button { Name = "ok", Text = "Delete", Result = "ok" };
            _cancel = new Button { Name = "cancel", Text = "Cancel", Result = "cancel" };

            foreach (Button button in new[] { _ok, _cancel })
            {
                button.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
                button.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 28 };
            }

            _dialog.AddChildren(_ok, _cancel);
            _root.AddChildren(_behind, _dialog);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void ItsContentGoesIntoTheSheet()
        {
            Assert.AreSame(_dialog.Sheet, _ok.Parent,
                "ContentHost again: what is written under a Dialog is its content, not a sibling "
                + "of its scrim");
            Assert.IsFalse(_dialog.Open);
        }

        [TestMethod]
        public void ItTakesNoSpaceUntilItIsOpened()
        {
            Assert.AreEqual(0f, _dialog.BoxHeight);
            Assert.AreEqual(0f, _behind.Y, "a closed dialog does not push the page around");

            Assert.IsNull(FindDialog(_surface.BuildAccessibilityTree()),
                "and it is out of the accessibility tree too, because hidden means both");
        }

        [TestMethod]
        public void TheScrimCoversTheWholeViewport()
        {
            _dialog.Show();
            Layout();

            Assert.AreEqual(0f, _dialog.Scrim.X);
            Assert.AreEqual(0f, _dialog.Scrim.Y);
            Assert.AreEqual(VIEWPORT, (int)_dialog.Scrim.ActualWidth);
            Assert.AreEqual(VIEWPORT, (int)_dialog.Scrim.ActualHeight,
                "it is a layer, so it covers the window rather than whatever it was declared in");
        }

        [TestMethod]
        public void AButtonClosesItWithItsOwnResult()
        {
            _dialog.Show();
            Layout();

            int closes = 0;
            _dialog.Closed += (sender, e) => closes++;

            _ok.PerformClick();

            Assert.IsFalse(_dialog.Open);
            Assert.AreEqual("ok", _dialog.Result,
                "which is the whole point of the helper: the answer comes back without the "
                + "application wiring a handler per button");
            Assert.AreEqual(1, closes);
        }

        [TestMethod]
        public void AnotherButtonGivesAnotherAnswer()
        {
            _dialog.Show();
            _cancel.PerformClick();

            Assert.AreEqual("cancel", _dialog.Result);
        }

        [TestMethod]
        public void ShowingAgainClearsTheLastAnswer()
        {
            _dialog.Show();
            _ok.PerformClick();

            _dialog.Show();

            Assert.IsNull(_dialog.Result,
                "a stale answer read after a second Show would be worse than none");
        }

        [TestMethod]
        public void TheScrimDismissesItAndTheSheetDoesNot()
        {
            _dialog.Show();
            Layout();

            _dialog.Sheet.PerformClick();
            Assert.IsTrue(_dialog.Open, "clicking the sheet must not close what you are reading");

            _dialog.Scrim.PerformClick();
            Assert.IsFalse(_dialog.Open);
            Assert.IsNull(_dialog.Result, "dismissing is not answering");
        }

        [TestMethod]
        public void DismissingCanBeTurnedOff()
        {
            _dialog.DismissOnScrim = false;
            _dialog.Show();
            Layout();

            _dialog.Scrim.PerformClick();

            Assert.IsTrue(_dialog.Open, "a dialog that must be answered says so");
        }

        [TestMethod]
        public void EscapeDismissesIt()
        {
            _dialog.Show();
            Layout();

            _surface.KeyDown(Key.Escape, KeyModifiers.None);

            Assert.IsFalse(_dialog.Open);
        }

        [TestMethod]
        public void OpeningTakesTheFocusAndClosingGivesItBack()
        {
            _surface.Focus(_behind);

            _dialog.Show();
            Layout();

            Assert.AreSame(_ok, _surface.FocusedElement,
                "the first focusable thing in the sheet, so the keyboard is already inside");

            _cancel.PerformClick();

            Assert.AreSame(_behind, _surface.FocusedElement,
                "and the focus goes back where it was, not to the top of the page");
        }

        [TestMethod]
        public void TabCannotLeaveAnOpenDialog()
        {
            _surface.Focus(_behind);
            _dialog.Show();
            Layout();

            for (int i = 0; i < 6; i++)
            {
                _surface.KeyDown(Key.Tab, KeyModifiers.None);

                Assert.AreNotSame(_behind, _surface.FocusedElement,
                    $"Tab {i + 1} left the dialog. Modal is what stops it, and it is a plain "
                    + "property on the layer rather than anything the dialog implements - but "
                    + "asserting only after the LAST tab passes either way, since the cycle "
                    + "comes back round.");
            }
        }

        [TestMethod]
        public void TheSheetIsTheDialogForAScreenReader()
        {
            _dialog.Show();
            Layout();

            AccessibleNode node = FindDialog(_surface.BuildAccessibilityTree());

            Assert.IsNotNull(node);
            Assert.AreEqual("Delete the file?", node.Name, "the title names it");
        }

        private static AccessibleNode FindDialog(AccessibleNode node)
        {
            if (node.Role == AccessibleRole.Dialog)
            {
                return node;
            }

            foreach (AccessibleNode child in node.Children)
            {
                AccessibleNode found = FindDialog(child);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
