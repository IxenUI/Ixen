using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class TextAreaTests
    {
        private const int VIEWPORT = 400;

        private TextArea _area;
        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _area = new TextArea { Name = "area" };
            _area.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _area.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            _root.AddChild(_area);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                Scheduler = new FakeScheduler()
            };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Press(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            _surface.KeyDown(key, modifiers);
            Layout();
        }

        [TestMethod]
        public void AnAreaIsMultilineAndScrollableOutOfTheBox()
        {
            Assert.IsTrue(_area.Multiline);
            Assert.IsTrue(_area.Scrollable);
            Assert.IsTrue(_area.Focusable);
        }

        [TestMethod]
        public void NewlinesBecomeLines()
        {
            _area.Text = "one\ntwo\nthree";
            Layout();

            Assert.AreEqual(3, _area.LineCount);
            Assert.AreEqual(3, _area.TextLines.Count);
            Assert.AreEqual("one", _area.TextLines[0]);
            Assert.AreEqual("three", _area.TextLines[2]);
        }

        [TestMethod]
        public void TheHeightGrowsWithTheLineCount()
        {
            _area.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            _area.Invalidate();
            _area.Text = "one";
            Layout();

            float single = _area.Height;

            _area.Text = "one\ntwo\nthree";
            Layout();

            Assert.IsTrue(_area.Height > single * 2, "three lines are about three times one");
        }

        [TestMethod]
        public void EnterInsertsALineBreak()
        {
            _surface.Focus(_area);
            _area.Text = "ab";
            _area.CaretIndex = 1;
            Layout();

            Press(Key.Enter);

            Assert.AreEqual("a\nb", _area.Text);
            Assert.AreEqual(2, _area.CaretIndex, "the caret sits after the break");
            Assert.AreEqual(2, _area.LineCount);
        }

        [TestMethod]
        public void ASingleLineFieldIgnoresEnter()
        {
            var field = new TextField { Name = "field" };
            _root.AddChild(field);
            Layout();

            _surface.Focus(field);
            field.Text = "ab";
            field.CaretIndex = 1;
            Layout();

            _surface.KeyDown(Key.Enter, KeyModifiers.None);

            Assert.AreEqual("ab", field.Text, "a break in a single line would have nowhere to go");
        }

        [TestMethod]
        public void UpAndDownMoveByLine()
        {
            _surface.Focus(_area);
            _area.Text = "one\ntwo\nthree";
            _area.CaretIndex = 1;
            Layout();

            Press(Key.Down);

            Assert.AreEqual(1, _area.LineAt(_area.CaretIndex), "second line");

            Press(Key.Down);

            Assert.AreEqual(2, _area.LineAt(_area.CaretIndex));

            Press(Key.Up);
            Press(Key.Up);

            Assert.AreEqual(0, _area.LineAt(_area.CaretIndex));
        }

        [TestMethod]
        public void DownOnTheLastLineGoesToTheEnd()
        {
            _surface.Focus(_area);
            _area.Text = "one\ntwo";
            _area.CaretIndex = 5;
            Layout();

            Press(Key.Down);

            Assert.AreEqual(_area.Text.Length, _area.CaretIndex);
        }

        [TestMethod]
        public void AVerticalMoveKeepsTheDesiredColumn()
        {
            _surface.Focus(_area);
            _area.Text = "long line here\nx\nlong line here";
            _area.CaretIndex = 10;
            Layout();

            Press(Key.Down);

            Assert.AreEqual(16, _area.CaretIndex, "the short line clamps the column");

            Press(Key.Down);

            Assert.AreEqual(27, _area.CaretIndex,
                "and the third line recovers the column it wanted, not the clamped one");
        }

        [TestMethod]
        public void HomeAndEndWorkOnTheLineAndControlOnTheDocument()
        {
            _surface.Focus(_area);
            _area.Text = "one\ntwo\nthree";
            _area.CaretIndex = 5;
            Layout();

            Press(Key.Home);
            Assert.AreEqual(4, _area.CaretIndex, "start of the second line");

            Press(Key.End);
            Assert.AreEqual(7, _area.CaretIndex, "end of the second line");

            Press(Key.Home, KeyModifiers.Control);
            Assert.AreEqual(0, _area.CaretIndex);

            Press(Key.End, KeyModifiers.Control);
            Assert.AreEqual(13, _area.CaretIndex);
        }

        [TestMethod]
        public void ABackspaceAtTheStartOfALineJoinsIt()
        {
            _surface.Focus(_area);
            _area.Text = "one\ntwo";
            _area.CaretIndex = 4;
            Layout();

            Press(Key.Backspace);

            Assert.AreEqual("onetwo", _area.Text);
            Assert.AreEqual(1, _area.LineCount);
        }

        [TestMethod]
        public void ASelectionCanSpanLines()
        {
            _surface.Focus(_area);
            _area.Text = "one\ntwo";
            _area.Select(6, 1);
            Layout();

            Assert.AreEqual("ne\ntw", _area.SelectedText);

            Press(Key.Backspace);

            Assert.AreEqual("oo", _area.Text);
        }

        [TestMethod]
        public void TheCaretIsScrolledIntoView()
        {
            _surface.Focus(_area);
            _area.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
            Layout();

            Assert.AreEqual(0, _area.ScrollY, "the caret is at the top, so nothing scrolled");

            _area.CaretIndex = _area.Text.Length;
            Layout();

            Assert.IsTrue(_area.ScrollY > 0, "reaching the last line brings it into view");

            _area.CaretIndex = 0;
            Layout();

            Assert.AreEqual(0, _area.ScrollY, "and going back to the top scrolls back");
        }

        [TestMethod]
        public void AnOverflowingAreaGetsAScrollbar()
        {
            _area.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
            Layout();

            Scrollbar bar = null;

            foreach (VisualElement chrome in _area.Chrome)
            {
                if (chrome is Scrollbar candidate && candidate.IsVertical)
                {
                    bar = candidate;
                }
            }

            Assert.IsNotNull(bar);
            Assert.IsFalse(bar.IsVoidOrInvalid, "an area reuses the scrolling that already existed");
        }

        [TestMethod]
        public void AClickPicksTheLineUnderThePointer()
        {
            _area.Text = "one\ntwo\nthree";
            Layout();

            _surface.PointerDown(_area.X + 2, _area.Y + _area.LineHeight + 2, PointerButton.Left);

            Assert.AreEqual(1, _area.LineAt(_area.CaretIndex), "the second line was clicked");
        }
    }
}
