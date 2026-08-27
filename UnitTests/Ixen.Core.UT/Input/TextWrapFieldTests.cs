using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class TextWrapFieldTests
    {
        private const int VIEWPORT = 600;
        private const float NARROW = 120;

        private const string SENTENCE =
            "the wild swans at coole are drifting on the still water under an autumn sky";

        private VisualElement _root;
        private TextArea _area;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _area = new TextArea { Name = "area" };
            _area.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = NARROW };
            _area.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _root.AddChild(_area);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                Scheduler = new FakeScheduler()
            };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void SetText(string text)
        {
            _area.Text = text;
            Layout();
        }

        private void Press(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            _surface.Focus(_area);
            _surface.KeyDown(key, modifiers);
            Layout();
        }

        [TestMethod]
        public void ALongLineIsBrokenIntoSeveralVisualLines()
        {
            SetText(SENTENCE);

            Assert.IsFalse(SENTENCE.Contains('\n'), "the value itself holds no break");
            Assert.IsTrue(_area.LineCount > 1, $"expected several lines, got {_area.LineCount}");
            Assert.AreEqual(_area.LineCount, _area.TextLines.Count);
        }

        [TestMethod]
        public void TheLinesJoinBackIntoTheValueExactly()
        {
            SetText(SENTENCE);

            var joined = new StringBuilder();

            foreach (string line in _area.TextLines)
            {
                joined.Append(line);
            }

            Assert.AreEqual(SENTENCE, joined.ToString(),
                "a soft break consumes no character, so the drawn lines must rebuild the value");
        }

        [TestMethod]
        public void EveryLineStartsWhereTheOneBeforeItEnds()
        {
            SetText(SENTENCE);

            int expected = 0;

            for (int line = 0; line < _area.LineCount; line++)
            {
                Assert.AreEqual(expected, _area.LineStart(line), $"start of line {line}");
                expected += _area.TextLines[line].Length;
            }
        }

        [TestMethod]
        public void EveryIndexBelongsToExactlyOneLine()
        {
            SetText(SENTENCE);

            for (int i = 0; i <= SENTENCE.Length; i++)
            {
                int line = _area.LineAt(i);

                Assert.IsTrue(line >= 0 && line < _area.LineCount, $"index {i} maps outside the lines");
                Assert.IsTrue(i >= _area.LineStart(line), $"index {i} is before the start of line {line}");

                if (line + 1 < _area.LineCount)
                {
                    Assert.IsTrue(i < _area.LineStart(line + 1),
                        $"index {i} was claimed by line {line} and by line {line + 1}");
                }
            }
        }

        [TestMethod]
        public void NoLineIsWiderThanTheBox()
        {
            SetText(SENTENCE);

            foreach (string line in _area.TextLines)
            {
                Assert.IsTrue(_area.CaretOffsets != null);
            }

            for (int line = 0; line < _area.LineCount; line++)
            {
                int end = _area.LineEnd(line);

                Assert.IsTrue(_area.OffsetAt(end) <= _area.ContentWidth + 1,
                    $"line {line} reaches {_area.OffsetAt(end)} in a {_area.ContentWidth} box");
            }
        }

        [TestMethod]
        public void EndOnAWrappedLineLandsBeforeTheBreakingSpace()
        {
            SetText(SENTENCE);

            int secondStart = _area.LineStart(1);

            Assert.AreEqual(' ', SENTENCE[secondStart - 1],
                "a soft break happens at a space, and the space stays on the line above");

            Assert.AreEqual(secondStart - 1, _area.LineEnd(0),
                "so the last caret position of the line is before that space");
        }

        [TestMethod]
        public void HomeAndEndWorkOnTheVisualLine()
        {
            SetText(SENTENCE);

            _area.Select(_area.LineStart(1) + 2, _area.LineStart(1) + 2);
            Layout();

            Press(Key.End);
            Assert.AreEqual(_area.LineEnd(1), _area.CaretIndex);

            Press(Key.Home);
            Assert.AreEqual(_area.LineStart(1), _area.CaretIndex);
        }

        [TestMethod]
        public void DownMovesToTheNextVisualLine()
        {
            SetText(SENTENCE);

            _area.Select(2, 2);
            Layout();

            Press(Key.Down);

            Assert.AreEqual(1, _area.LineAt(_area.CaretIndex),
                "the caret crossed a soft break, not a newline");
        }

        [TestMethod]
        public void UpComesBackToTheLineAbove()
        {
            SetText(SENTENCE);

            _area.Select(_area.LineStart(2) + 1, _area.LineStart(2) + 1);
            Layout();

            Press(Key.Up);

            Assert.AreEqual(1, _area.LineAt(_area.CaretIndex));
        }

        [TestMethod]
        public void ClickingTheSecondVisualLineLandsOnIt()
        {
            SetText(SENTENCE);

            float y = _area.Y + _area.LineHeight * 1.5f;
            int index = _area.IndexAt(_area.X + 4, y);

            Assert.AreEqual(1, _area.LineAt(index));
        }

        [TestMethod]
        public void NoWrapKeepsOneLineAndScrollsSideways()
        {
            _area.Styles.TextWrap = new TextWrapStyleDescriptor { Value = TextWrap.NoWrap };
            _area.Invalidate();

            SetText(SENTENCE);

            Assert.AreEqual(1, _area.LineCount);
            Assert.IsTrue(_area.ScrollExtentWidth > _area.ContentWidth,
                "the long line overflows, which is what the horizontal scrollbar is for");
        }

        [TestMethod]
        public void ASingleLineFieldNeverWraps()
        {
            var field = new TextField { Name = "field" };
            field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = NARROW };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _root.AddChild(field);
            field.Text = SENTENCE;
            Layout();

            Assert.AreEqual(1, field.LineCount,
                "wrapping is a multiline concern; a field scrolls sideways like an input");
        }

        [TestMethod]
        public void AWordTooLongForTheBoxOverflowsRatherThanBreaking()
        {
            SetText("short aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa end");

            foreach (string line in _area.TextLines)
            {
                Assert.IsFalse(line.Contains("aaaa") && line.Contains("end"),
                    "the long word is not split, so it keeps a line to itself");
            }

            Assert.IsTrue(_area.TextLines.Any(l => l.Trim() == "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                "there is no character-level breaking, matching non-editable text");
        }

        [TestMethod]
        public void HardBreaksAndSoftBreaksCoexist()
        {
            SetText("one\n" + SENTENCE + "\ntwo");

            Assert.IsTrue(_area.LineCount > 3, "the middle paragraph wrapped on top of the two newlines");
            Assert.AreEqual("one", _area.TextLines[0]);
            Assert.AreEqual("two", _area.TextLines[_area.LineCount - 1]);
        }

        [TestMethod]
        public void TypingReflowsTheLines()
        {
            SetText(SENTENCE);

            int before = _area.LineCount;

            _surface.Focus(_area);
            _area.Select(0, 0);

            for (int i = 0; i < 20; i++)
            {
                _surface.TextInput("xx ");
            }

            Layout();

            Assert.IsTrue(_area.LineCount > before,
                "adding text pushes words down without any explicit relayout call");
        }

        [TestMethod]
        public void NarrowingTheBoxReflows()
        {
            SetText(SENTENCE);

            int wide = _area.LineCount;

            _area.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = NARROW / 2 };
            _area.Invalidate();
            Layout();

            Assert.IsTrue(_area.LineCount > wide,
                "the wrap is recomputed from the content width every measure pass");
        }

        [TestMethod]
        public void TheMeasuredHeightFollowsTheLineCount()
        {
            SetText(SENTENCE);

            var tall = new TextArea { Name = "tall" };
            tall.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = NARROW };
            tall.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            _root.AddChild(tall);

            tall.Text = SENTENCE;
            Layout();

            Assert.IsTrue(tall.LineCount > 1);
            Assert.IsTrue(tall.ActualHeight >= tall.LineHeight * tall.LineCount,
                "a content-sized area grows to hold every wrapped line");
        }

        [TestMethod]
        public void EveryLineStartsAtTheLeftEdge()
        {
            SetText(SENTENCE);

            for (int line = 0; line < _area.LineCount; line++)
            {
                Assert.AreEqual(0f, _area.OffsetAt(_area.LineStart(line)),
                    $"the caret at the start of line {line} must sit at x 0");
            }
        }

        [TestMethod]
        public void ATrailingSpaceDoesNotPushTheWordBeforeItDown()
        {
            _surface.TextMeasurer = new FixedTextMeasurer(10, 20);
            _area.Scrollable = false;
            _area.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 75 };
            _area.Invalidate();

            SetText("aaa bbb ccc");

            CollectionAssert.AreEqual(
                new[] { "aaa bbb ", "ccc" },
                _area.TextLines.ToArray(),
                "'aaa bbb' is 7 chars = 70 and fits in 75; the space after it reaches 80 and must hang "
                + "rather than move 'bbb' to the next line");
        }

        [TestMethod]
        public void TheBreakGoesAfterTheSpaceNotBeforeIt()
        {
            _surface.TextMeasurer = new FixedTextMeasurer(10, 20);
            _area.Scrollable = false;
            _area.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            _area.Invalidate();

            SetText("aaa bbb");

            CollectionAssert.AreEqual(new[] { "aaa ", "bbb" }, _area.TextLines.ToArray());
            Assert.AreEqual(4, _area.LineStart(1), "the space belongs to the line above");
            Assert.AreEqual(3, _area.LineEnd(0), "and there is no caret position after it");
            Assert.AreEqual(0f, _area.OffsetAt(4));
            Assert.AreEqual(30f, _area.OffsetAt(3));

            Assert.AreEqual(10f, _area.OffsetAt(5),
                "the wrapped word is re-measured from its own line start, not from the old one");
            Assert.AreEqual(20f, _area.OffsetAt(6));
            Assert.AreEqual(30f, _area.OffsetAt(7));
        }
    }

    internal sealed class FixedTextMeasurer : ITextMeasurer
    {
        private readonly float _charWidth;
        private readonly float _lineHeight;

        internal FixedTextMeasurer(float charWidth, float lineHeight)
        {
            _charWidth = charWidth;
            _lineHeight = lineHeight;
        }

        public void MeasureText(string text, FontSpec font, out float width, out float height)
        {
            width = (text == null ? 0 : text.Length) * _charWidth;
            height = _lineHeight;
        }

        public void MeasureCharacters(string text, FontSpec font, float[] advances)
        {
            for (int index = 0; index < (text == null ? 0 : text.Length); index++)
            {
                advances[index] = _charWidth;
            }
        }

        public float GetLineHeight(FontSpec font) => _lineHeight;
    }
}
