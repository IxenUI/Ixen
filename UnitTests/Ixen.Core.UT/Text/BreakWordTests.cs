using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class BreakWordTests
    {
        private const int VIEWPORT = 600;
        private const float PER_CHAR = 10;
        private const float BOX = 100;

        private sealed class FixedMeasurer : ITextMeasurer
        {
            public void MeasureText(string text, FontSpec font, out float width, out float height)
            {
                width = (text == null ? 0 : text.Length) * PER_CHAR;
                height = 20;
            }

            public void MeasureCharacters(string text, FontSpec font, float[] advances)
            {
                for (int index = 0; index < (text == null ? 0 : text.Length); index++)
                {
                    advances[index] = PER_CHAR;
                }
            }

            public float GetLineHeight(FontSpec font) => 20;
        }

        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                TextMeasurer = new FixedMeasurer()
            };
        }

        private VisualElement Label(string text, TextWrap wrap, float width = BOX, float height = 0)
        {
            var label = new VisualElement { Name = "label", Text = text };
            label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            label.Styles.TextWrap = new TextWrapStyleDescriptor { Value = wrap };

            if (height > 0)
            {
                label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            }
            else
            {
                label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            }

            _root.AddChild(label);

            Layout();

            return label;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void AWordTooLongForTheBoxIsCutAtTheEdge()
        {
            VisualElement label = Label("abcdefghijklmnopqrstuvwxyz", TextWrap.BreakWord);

            Assert.AreEqual(3, label.TextLines.Count);
            Assert.AreEqual("abcdefghij", label.TextLines[0]);
            Assert.AreEqual("klmnopqrst", label.TextLines[1]);
            Assert.AreEqual("uvwxyz", label.TextLines[2]);
        }

        [TestMethod]
        public void WithoutItTheWordOverflowsExactlyAsBefore()
        {
            VisualElement label = Label("abcdefghijklmnopqrstuvwxyz", TextWrap.Wrap);

            Assert.AreEqual(1, label.TextLines.Count,
                "a word too long for the box has always been emitted on a line of its own and left to "
                + "overflow, and that is what stays the default");
        }

        [TestMethod]
        public void AWordThatFitsOnALineOfItsOwnIsNeverCut()
        {
            VisualElement label = Label("hello worldwide", TextWrap.BreakWord);

            Assert.AreEqual(2, label.TextLines.Count);
            Assert.AreEqual("hello", label.TextLines[0]);
            Assert.AreEqual("worldwide", label.TextLines[1],
                "nine characters fit in ten, so the space break was enough");
        }

        [TestMethod]
        public void TheSpaceBreakIsTakenFirstAndTheTailIsThenCut()
        {
            VisualElement label = Label("hi abcdefghijklmno", TextWrap.BreakWord);

            Assert.AreEqual(3, label.TextLines.Count);
            Assert.AreEqual("hi", label.TextLines[0]);
            Assert.AreEqual("abcdefghij", label.TextLines[1]);
            Assert.AreEqual("klmno", label.TextLines[2]);
        }

        [TestMethod]
        public void AWordAfterACutCarriesOnWrappingNormally()
        {
            VisualElement label = Label("aaaaaaaaaaaa bbb", TextWrap.BreakWord);

            Assert.AreEqual(2, label.TextLines.Count);
            Assert.AreEqual("aaaaaaaaaa", label.TextLines[0]);
            Assert.AreEqual("aa bbb", label.TextLines[1]);
        }

        [TestMethod]
        public void ACutConsumesNothing()
        {
            const string text = "abcdefghijklmnopqrstuvwxyz";

            VisualElement label = Label(text, TextWrap.BreakWord);

            var joined = new StringBuilder();

            foreach (string line in label.TextLines)
            {
                joined.Append(line);
            }

            Assert.AreEqual(text, joined.ToString(),
                "unlike a space break, which eats the space it broke at");
        }

        [TestMethod]
        [Timeout(5000)]
        public void ACharacterWiderThanTheBoxStillGetsALineOfItsOwn()
        {
            VisualElement label = Label("abcd", TextWrap.BreakWord, 5);

            Assert.AreEqual(4, label.TextLines.Count,
                "one character a line rather than a hung layout pass");
        }

        [TestMethod]
        public void EveryLineFitsTheBox()
        {
            VisualElement label = Label("the quick brownfoxjumpsoverthelazydog again", TextWrap.BreakWord);

            foreach (string line in label.TextLines)
            {
                Assert.IsTrue(line.Length * PER_CHAR <= BOX, $"'{line}' is wider than the box");
            }
        }

        [TestMethod]
        public void ItMakesTheElementTaller()
        {
            VisualElement wrapped = Label("abcdefghijklmnopqrstuvwxyz", TextWrap.Wrap);
            VisualElement broken = Label("abcdefghijklmnopqrstuvwxyz", TextWrap.BreakWord);

            Assert.AreEqual(20, wrapped.Height, 0.01f);
            Assert.AreEqual(60, broken.Height, 0.01f);
        }

        [TestMethod]
        public void A_QuestionMarkWidthShrinksToTheWidestKeptLine()
        {
            var label = new VisualElement { Name = "label", Text = "abcdefghijklmnopqrst" };
            label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            label.Styles.TextWrap = new TextWrapStyleDescriptor { Value = TextWrap.BreakWord };

            var frame = new VisualElement { Name = "frame" };
            frame.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            frame.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            frame.AddChild(label);

            _root.AddChild(frame);

            Layout();

            Assert.AreEqual(2, label.TextLines.Count);
            Assert.AreEqual(BOX, label.Width, 0.01f, "the box it was offered, since both lines are full");
        }

        [TestMethod]
        public void TheCacheKnowsTheWrapModeApart()
        {
            VisualElement label = Label("abcdefghijklmnopqrstuvwxyz", TextWrap.Wrap);

            Assert.AreEqual(1, label.TextLines.Count);

            label.Styles.TextWrap = new TextWrapStyleDescriptor { Value = TextWrap.BreakWord };
            label.Invalidate();

            Layout();

            Assert.AreEqual(3, label.TextLines.Count,
                "the wrap mode is an input of the text layout, so it belongs to the cache key");
        }

        [TestMethod]
        public void ItComposesWithTheVerticalEllipsis()
        {
            VisualElement label = Label("abcdefghijklmnopqrstuvwxyz", TextWrap.BreakWord, BOX, 40);

            label.Styles.TextOverflow = new TextOverflowStyleDescriptor { Value = TextOverflow.Ellipsis };
            label.Invalidate();

            Layout();

            Assert.AreEqual(2, label.TextLines.Count, "two lines of twenty fit in forty");
            Assert.IsTrue(label.TextLines[1].EndsWith("…"), $"'{label.TextLines[1]}' should be marked");
        }

        [TestMethod]
        public void ATextAreaWrapsAtSpacesAndNeverInsideAWord()
        {
            var area = new TextArea { Name = "area", Text = "hi abcdefghijklmno" };
            area.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            area.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            area.Styles.TextWrap = new TextWrapStyleDescriptor { Value = TextWrap.BreakWord };

            _root.AddChild(area);

            Layout();

            Assert.AreEqual(2, area.TextLines.Count);
            Assert.AreEqual("abcdefghijklmno", area.TextLines[1],
                "an editable field's line model reads a consumed break at the end of every line, and a "
                + "cut consumes nothing - so a field wraps at spaces whatever the style says");
        }
    }
}
