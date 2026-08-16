using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class TextFieldTests
    {
        private const int VIEWPORT = 400;

        private TextField _field;
        private VisualElement _root;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _field = new TextField { Name = "field" };
            _field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            _root.AddChild(_field);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                Scheduler = _scheduler
            };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Paint()
        {
            Layout();

            using (var bitmap = new SkiaSharp.SKBitmap(VIEWPORT, VIEWPORT))
            using (var canvas = new SkiaSharp.SKCanvas(bitmap))
            {
                _surface.Render(canvas);
            }
        }

        private void Type(string text)
        {
            _surface.TextInput(text);
            Layout();
        }

        private void Press(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            _surface.KeyDown(key, modifiers);
            Layout();
        }

        [TestMethod]
        public void AFieldIsFocusableAndTakesTheFocusOnAClick()
        {
            _surface.PointerDown(10, 10, PointerButton.Left);

            Assert.IsTrue(_field.Focusable);
            Assert.AreSame(_field, _surface.FocusedElement);
        }

        [TestMethod]
        public void TypingInsertsAtTheCaret()
        {
            _surface.Focus(_field);

            Type("a");
            Type("b");

            Assert.AreEqual("ab", _field.Text);
            Assert.AreEqual(2, _field.CaretIndex);
        }

        [TestMethod]
        public void BackspaceAndDeleteRemoveOneCharacter()
        {
            _surface.Focus(_field);
            _field.Text = "abcd";
            _field.CaretIndex = 2;
            Layout();

            Press(Key.Backspace);

            Assert.AreEqual("acd", _field.Text);
            Assert.AreEqual(1, _field.CaretIndex);

            Press(Key.Delete);

            Assert.AreEqual("ad", _field.Text);
            Assert.AreEqual(1, _field.CaretIndex);
        }

        [TestMethod]
        public void BackspaceAtTheStartAndDeleteAtTheEndDoNothing()
        {
            _surface.Focus(_field);
            _field.Text = "ab";
            _field.CaretIndex = 0;
            Layout();

            Press(Key.Backspace);

            Assert.AreEqual("ab", _field.Text);

            _field.CaretIndex = 2;
            Press(Key.Delete);

            Assert.AreEqual("ab", _field.Text);
        }

        [TestMethod]
        public void ArrowsMoveTheCaretAndHomeEndJump()
        {
            _surface.Focus(_field);
            _field.Text = "hello";
            _field.CaretIndex = 5;
            Layout();

            Press(Key.Left);
            Assert.AreEqual(4, _field.CaretIndex);

            Press(Key.Home);
            Assert.AreEqual(0, _field.CaretIndex);

            Press(Key.Right);
            Assert.AreEqual(1, _field.CaretIndex);

            Press(Key.End);
            Assert.AreEqual(5, _field.CaretIndex);
        }

        [TestMethod]
        public void ShiftArrowsExtendTheSelection()
        {
            _surface.Focus(_field);
            _field.Text = "hello";
            _field.CaretIndex = 0;
            Layout();

            Press(Key.Right, KeyModifiers.Shift);
            Press(Key.Right, KeyModifiers.Shift);

            Assert.AreEqual(0, _field.SelectionStart);
            Assert.AreEqual(2, _field.SelectionLength);
            Assert.AreEqual("he", _field.SelectedText);
        }

        [TestMethod]
        public void TypingReplacesTheSelection()
        {
            _surface.Focus(_field);
            _field.Text = "hello";
            _field.Select(4, 1);
            Layout();

            Type("i");

            Assert.AreEqual("hio", _field.Text);
            Assert.AreEqual(2, _field.CaretIndex);
            Assert.AreEqual(0, _field.SelectionLength);
        }

        [TestMethod]
        public void BackspaceRemovesTheSelectionRatherThanOneCharacter()
        {
            _surface.Focus(_field);
            _field.Text = "hello";
            _field.Select(0, 3);
            Layout();

            Press(Key.Backspace);

            Assert.AreEqual("lo", _field.Text);
            Assert.AreEqual(0, _field.CaretIndex);
        }

        [TestMethod]
        public void ControlAndASelectsEverything()
        {
            _surface.Focus(_field);
            _field.Text = "hello";
            Layout();

            Press(Key.A, KeyModifiers.Control);

            Assert.AreEqual("hello", _field.SelectedText);
        }

        [TestMethod]
        public void APlainAIsNotASelectAll()
        {
            _surface.Focus(_field);
            _field.Text = "hello";
            _field.CaretIndex = 0;
            Layout();

            Press(Key.A);

            Assert.AreEqual(0, _field.SelectionLength, "the letter itself arrives through TextInput");
        }

        [TestMethod]
        public void AnUnfocusedArrowDoesNotMoveTheCaret()
        {
            _field.Text = "hello";
            _field.CaretIndex = 2;
            Layout();

            Press(Key.Right);

            Assert.AreEqual(2, _field.CaretIndex, "keys go to the root when nothing is focused");
        }

        [TestMethod]
        public void SettingTheTextClampsTheCaret()
        {
            _field.Text = "hello";
            _field.CaretIndex = 5;

            _field.Text = "hi";

            Assert.AreEqual(2, _field.CaretIndex);
        }

        [TestMethod]
        public void TextChangedFiresOnEditsOnly()
        {
            var log = new List<string>();
            _field.TextChanged += (s, e) => log.Add(_field.Text);

            _field.Text = "set from code";
            _surface.Focus(_field);
            Type("!");

            Assert.AreEqual(1, log.Count, "assigning Text is not an edit");
            Assert.AreEqual("!set from code", log[0],
                "assigning Text keeps the caret where it was rather than jumping to the end");
        }

        [TestMethod]
        public void ADoubleClickSelectsAWord()
        {
            _surface.Focus(_field);
            _field.Text = "one two three";
            Layout();

            _field.Select(5, 5);
            _field.RaisePointerDoubleClick(new PointerEventArgs(0, 0, PointerButton.Left, _field));

            Assert.AreEqual("one", _field.SelectedText,
                "the fake measurer gives every glyph a zero width, so the hit lands at index 0");
        }

        [TestMethod]
        public void TheCaretBlinksWhileFocusedAndStopsWhenItLeaves()
        {
            _surface.Focus(_field);

            Assert.AreEqual(1, _scheduler.PendingCount, "one repeating blink");
            Assert.IsTrue(_scheduler.Scheduled[0].Repeat);
            Assert.IsTrue(_field.CaretVisible);

            _scheduler.FireAll();
            Assert.IsFalse(_field.CaretVisible);
            Assert.IsTrue(_surface.IsDirty, "a blink asks for a repaint");

            _surface.Focus(null);

            Assert.AreEqual(0, _scheduler.PendingCount, "the blink is disposed with the focus");
            Assert.IsFalse(_field.IsFocused);
        }

        [TestMethod]
        public void TypingBringsTheCaretBackImmediately()
        {
            _surface.Focus(_field);
            _scheduler.FireAll();

            Assert.IsFalse(_field.CaretVisible);

            Type("a");

            Assert.IsTrue(_field.CaretVisible, "the caret must not be hidden while you type");
        }

        [TestMethod]
        public void ABlinkRepaintsWithoutRelayingOut()
        {
            _surface.Focus(_field);
            Paint();

            Assert.IsFalse(_surface.IsDirty, "rendering clears the visual flag");

            _scheduler.FireAll();

            Assert.IsTrue(_surface.IsDirty);
            Assert.IsFalse(_root.IsLayoutDirty, "blinking is a repaint, not a layout");
        }

        [TestMethod]
        public void AFieldIsMeasuredAsASingleLine()
        {
            _field.Text = "one\ntwo";
            Layout();

            Assert.AreEqual(1, _field.TextLines.Count);
            Assert.AreEqual("one\ntwo", _field.TextLines[0], "a field never wraps and never ellipsises");
        }
    }
}
