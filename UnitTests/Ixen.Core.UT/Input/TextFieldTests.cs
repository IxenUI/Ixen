using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    internal class FakeClipboard : IClipboard
    {
        internal string Text;

        public string GetText() => Text;
        public void SetText(string text) => Text = text;
    }

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
        public void CopyAndPasteGoThroughTheClipboard()
        {
            var clipboard = new FakeClipboard();
            _surface.Clipboard = clipboard;
            _surface.Focus(_field);

            _field.Text = "hello world";
            _field.Select(5, 0);
            Layout();

            Press(Key.C, KeyModifiers.Control);

            Assert.AreEqual("hello", clipboard.Text);
            Assert.AreEqual("hello world", _field.Text, "a copy changes nothing");

            _field.CaretIndex = 11;
            Press(Key.V, KeyModifiers.Control);

            Assert.AreEqual("hello worldhello", _field.Text);
        }

        [TestMethod]
        public void CutCopiesAndRemoves()
        {
            var clipboard = new FakeClipboard();
            _surface.Clipboard = clipboard;
            _surface.Focus(_field);

            _field.Text = "hello world";
            _field.Select(0, 6);
            Layout();

            Press(Key.X, KeyModifiers.Control);

            Assert.AreEqual("hello ", clipboard.Text);
            Assert.AreEqual("world", _field.Text);
            Assert.AreEqual(0, _field.CaretIndex);
        }

        [TestMethod]
        public void PastingReplacesTheSelectionAndDropsControlCharacters()
        {
            var clipboard = new FakeClipboard { Text = "one\r\ntwo" };
            _surface.Clipboard = clipboard;
            _surface.Focus(_field);

            _field.Text = "[xx]";
            _field.Select(1, 3);
            Layout();

            Press(Key.V, KeyModifiers.Control);

            Assert.AreEqual("[onetwo]", _field.Text, "a field is single-line, so newlines are dropped");
        }

        [TestMethod]
        public void WithNoClipboardTheShortcutsDoNothing()
        {
            _surface.Focus(_field);
            _field.Text = "hello";
            _field.SelectAll();
            Layout();

            Press(Key.C, KeyModifiers.Control);
            Press(Key.X, KeyModifiers.Control);
            Press(Key.V, KeyModifiers.Control);

            Assert.AreEqual("hello", _field.Text, "a host with no clipboard keeps every other edit");
        }

        [TestMethod]
        public void CopyingNothingLeavesTheClipboardAlone()
        {
            var clipboard = new FakeClipboard { Text = "kept" };
            _surface.Clipboard = clipboard;
            _surface.Focus(_field);

            _field.Text = "hello";
            _field.CaretIndex = 2;
            Layout();

            Press(Key.C, KeyModifiers.Control);

            Assert.AreEqual("kept", clipboard.Text);
        }

        [TestMethod]
        public void APlaceholderShowsOnlyWhileTheValueIsEmpty()
        {
            _field.Placeholder = "your name";

            Assert.IsTrue(_field.ShowsPlaceholder);

            _surface.Focus(_field);
            Type("a");

            Assert.IsFalse(_field.ShowsPlaceholder, "it is about the value, not the focus");

            Press(Key.Backspace);

            Assert.IsTrue(_field.ShowsPlaceholder);
        }

        [TestMethod]
        public void APasswordFieldMasksItsValueButKeepsIt()
        {
            _field.Text = "secret";
            _field.PasswordChar = '*';
            Layout();

            Assert.AreEqual("secret", _field.Text, "the value itself is untouched");
            Assert.AreEqual("******", _field.DisplayText);
            Assert.AreEqual("******", _field.TextLines[0], "what is drawn is the mask");
        }

        [TestMethod]
        public void ThePasswordSwitchUsesTheBulletAndAnyCharacterStillWorks()
        {
            _field.Text = "secret";
            _field.Password = true;
            Layout();

            Assert.AreEqual(new string(TextField.DEFAULT_PASSWORD_CHAR, 6), _field.DisplayText);
            Assert.AreEqual('\u25CF', _field.PasswordChar, "the default mask is a bullet");

            _field.PasswordChar = '#';
            Layout();

            Assert.AreEqual("######", _field.DisplayText);
            Assert.IsTrue(_field.Password, "the switch reads the character, it does not shadow it");

            _field.Password = false;

            Assert.AreEqual("secret", _field.DisplayText);
            Assert.AreEqual('\0', _field.PasswordChar);
        }

        [TestMethod]
        public void APasswordFieldCannotBeCopied()
        {
            var clipboard = new FakeClipboard { Text = "kept" };
            _surface.Clipboard = clipboard;
            _surface.Focus(_field);

            _field.Text = "secret";
            _field.PasswordChar = '*';
            _field.SelectAll();
            Layout();

            Press(Key.C, KeyModifiers.Control);
            Press(Key.X, KeyModifiers.Control);

            Assert.AreEqual("kept", clipboard.Text, "a masked value never leaves the field");
            Assert.AreEqual("secret", _field.Text, "and cutting it is refused too");
        }

        [TestMethod]
        public void UndoRestoresTheTextAndTheCaret()
        {
            _surface.Focus(_field);
            _field.Text = "hello";
            _field.CaretIndex = 5;
            Layout();

            Press(Key.Backspace);

            Assert.AreEqual("hell", _field.Text);

            Press(Key.Z, KeyModifiers.Control);

            Assert.AreEqual("hello", _field.Text);
            Assert.AreEqual(5, _field.CaretIndex);
        }

        [TestMethod]
        public void TypingARunCollapsesIntoOneUndoStep()
        {
            _surface.Focus(_field);

            Type("a");
            Type("b");
            Type("c");

            Press(Key.Z, KeyModifiers.Control);

            Assert.AreEqual(string.Empty, _field.Text, "a run of characters undoes in one go");
        }

        [TestMethod]
        public void ASpaceAndACaretMoveBreakTheRun()
        {
            _surface.Focus(_field);

            Type("ab");
            Type(" ");
            Type("cd");

            Press(Key.Z, KeyModifiers.Control);

            Assert.AreEqual("ab ", _field.Text);

            Press(Key.Z, KeyModifiers.Control);

            Assert.AreEqual("ab", _field.Text, "the space is its own step");
        }

        [TestMethod]
        public void RedoPutsItBack()
        {
            _surface.Focus(_field);
            Type("hello");

            Press(Key.Z, KeyModifiers.Control);
            Assert.AreEqual(string.Empty, _field.Text);

            Press(Key.Y, KeyModifiers.Control);
            Assert.AreEqual("hello", _field.Text);

            Press(Key.Z, KeyModifiers.Control);
            Press(Key.Z, KeyModifiers.Shift | KeyModifiers.Control);
            Assert.AreEqual("hello", _field.Text, "Ctrl+Shift+Z redoes too");
        }

        [TestMethod]
        public void AnEditAfterAnUndoDropsTheRedo()
        {
            _surface.Focus(_field);
            Type("ab");

            Press(Key.Z, KeyModifiers.Control);
            Type("c");

            Assert.IsFalse(_field.CanRedo);
            Assert.AreEqual("c", _field.Text);
        }

        [TestMethod]
        public void AssigningTextResetsTheHistory()
        {
            _surface.Focus(_field);
            Type("typed");

            Assert.IsTrue(_field.CanUndo);

            _field.Text = "assigned";

            Assert.IsFalse(_field.CanUndo, "an assignment is a new baseline, not an undoable edit");
        }

        [TestMethod]
        public void UndoingWithNoHistoryDoesNothing()
        {
            _surface.Focus(_field);
            _field.Text = "hello";
            Layout();

            Press(Key.Z, KeyModifiers.Control);

            Assert.AreEqual("hello", _field.Text);
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
