using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class CompositionTests
    {
        private const int VIEWPORT = 200;

        private TextField _field;
        private IxenSurface _surface;
        private int _changes;

        [TestInitialize]
        public void Setup()
        {
            _changes = 0;

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _field = new TextField { Name = "field" };
            _field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 160 };
            _field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };
            _field.TextChanged += (sender, args) => _changes++;

            root.AddChild(_field);

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        [TestMethod]
        public void AComposingRunIsShownWithoutBeingInTheValue()
        {
            _field.Text = "ab";
            _field.CaretIndex = 2;

            _field.SetComposition("にほ", 2);

            Assert.AreEqual("ab", _field.Text, "nothing is committed until the IME says so");
            Assert.AreEqual("abにほ", _field.DisplayText, "but it is what gets measured and drawn");
            Assert.AreEqual(0, _changes, "and it is not an edit, so no TextChanged");
        }

        [TestMethod]
        public void TheCaretSitsInsideTheComposingRun()
        {
            _field.Text = "ab";
            _field.CaretIndex = 2;

            _field.SetComposition("にほん", 1);

            Assert.AreEqual(2, _field.CaretIndex, "the real caret has not moved");
            Assert.AreEqual(3, _field.DisplayCaret,
                "but the drawn one is inside the run, where the IME put it");
        }

        [TestMethod]
        public void ReplacingTheRunDoesNotStack()
        {
            _field.Text = "ab";
            _field.CaretIndex = 2;

            _field.SetComposition("に", 1);
            _field.SetComposition("にほ", 2);
            _field.SetComposition("日本", 2);

            Assert.AreEqual("ab日本", _field.DisplayText,
                "each update replaces the run; an IME sends the whole thing every time");
        }

        [TestMethod]
        public void CommittingPutsItInTheValueAsOneEdit()
        {
            _field.Text = "ab";
            _field.CaretIndex = 2;

            _field.SetComposition("にほん", 3);
            _field.CommitComposition("日本");

            Assert.AreEqual("ab日本", _field.Text);
            Assert.AreEqual("ab日本", _field.DisplayText, "the run is gone, the value carries it");
            Assert.AreEqual(4, _field.CaretIndex);
            Assert.AreEqual(1, _changes, "one edit, not one per keystroke of the composition");
        }

        [TestMethod]
        public void CommittingIsOneUndoStep()
        {
            _field.Text = "ab";
            _field.CaretIndex = 2;

            _field.SetComposition("にほん", 3);
            _field.CommitComposition("日本");

            _field.Undo();

            Assert.AreEqual("ab", _field.Text,
                "the composition went through Insert, so it is a single mutation like any other");
        }

        [TestMethod]
        public void CommittingNothingJustDropsTheRun()
        {
            _field.Text = "ab";
            _field.CaretIndex = 2;

            _field.SetComposition("にほ", 2);
            _field.CommitComposition(null);

            Assert.AreEqual("ab", _field.Text);
            Assert.IsFalse(_field.IsComposing);
            Assert.AreEqual(0, _changes, "an IME that ends with nothing chosen has changed nothing");
        }

        [TestMethod]
        public void CancellingDropsTheRun()
        {
            _field.Text = "ab";
            _field.CaretIndex = 2;

            _field.SetComposition("にほ", 2);
            _field.CancelComposition();

            Assert.AreEqual("ab", _field.DisplayText);
            Assert.IsFalse(_field.IsComposing);
        }

        [TestMethod]
        public void LosingTheFocusCancelsIt()
        {
            _field.Focusable = true;

            _surface.Focus(_field);

            _field.Text = "ab";
            _field.CaretIndex = 2;
            _field.SetComposition("にほ", 2);

            _surface.Focus(null);

            Assert.IsFalse(_field.IsComposing,
                "a run left behind would be drawn over text the user can no longer edit");
        }

        [TestMethod]
        public void StartingOverASelectionReplacesIt()
        {
            _field.Text = "hello";
            _field.Select(0, 5);

            _field.SetComposition("に", 1);

            Assert.AreEqual("", _field.Text, "the selection goes when the run opens, as everywhere else");
            Assert.AreEqual("に", _field.DisplayText);
        }

        [TestMethod]
        public void AMaskedFieldMasksTheRunToo()
        {
            _field.Password = true;
            _field.Text = "ab";
            _field.CaretIndex = 2;

            _field.SetComposition("にほ", 2);

            Assert.AreEqual(4, _field.DisplayText.Length);
            Assert.IsFalse(_field.DisplayText.Contains("に"),
                "a password field must not leak the run any more than it leaks the value");
        }

        [TestMethod]
        public void TheRunIsMeasuredSoTheCaretMoves()
        {
            _field.Text = "ab";
            _field.CaretIndex = 2;

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            float before = _field.OffsetAt(_field.DisplayCaret);

            _field.SetComposition("にほん", 3);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsTrue(_field.OffsetAt(_field.DisplayCaret) > before,
                "the run goes through DisplayText, so measure, caret offsets and drawing all see "
                + "it with no separate path");
        }
    }
}
