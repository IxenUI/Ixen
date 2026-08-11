using Ixen.Core.Language.Xns;
using Ixen.Core.UT.Layout.Geometry;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class TextEllipsisTests : BaseGeometryTests
    {
        private const string ELLIPSIS = "…";
        private const string LONG_TEXT = "Contract renewal for the northern region";

        private static VisualElement Row(string text, TextOverflow overflow, TextWrap wrap = TextWrap.NoWrap,
            float width = 150)
        {
            var element = Element("row", LayoutType.Column, SizeUnit.Pixels, width, SizeUnit.Content, 0);
            element.Styles.FontSize = new FontSizeStyleDescriptor { Value = 16 };
            element.Styles.TextWrap = new TextWrapStyleDescriptor { Value = wrap };
            element.Styles.TextOverflow = new TextOverflowStyleDescriptor { Value = overflow };
            element.Text = text;
            return element;
        }

        private static string SingleLine(VisualElement element)
        {
            Layout(element);

            Assert.AreEqual(1, element.TextLines.Count, string.Join(" / ", element.TextLines));

            return element.TextLines[0];
        }

        [TestMethod]
        public void ClipIsTheDefault()
        {
            var element = Element("row", LayoutType.Column, SizeUnit.Pixels, 150, SizeUnit.Content, 0);
            element.Styles.FontSize = new FontSizeStyleDescriptor { Value = 16 };
            element.Styles.TextWrap = new TextWrapStyleDescriptor { Value = TextWrap.NoWrap };
            element.Text = LONG_TEXT;

            Assert.AreEqual(LONG_TEXT, SingleLine(element), "nothing is shortened without text-overflow");
        }

        [TestMethod]
        public void AnOverflowingLineIsShortened()
        {
            string line = SingleLine(Row(LONG_TEXT, TextOverflow.Ellipsis));

            Assert.IsTrue(line.EndsWith(ELLIPSIS), $"'{line}' should end with an ellipsis");
            Assert.IsTrue(line.Length < LONG_TEXT.Length, $"'{line}' should be shorter than the source");
            Assert.IsTrue(LONG_TEXT.StartsWith(line.Substring(0, line.Length - 1)),
                "the kept part must be a prefix of the original");
        }

        [TestMethod]
        public void TheShortenedLineFitsTheContentWidth()
        {
            VisualElement clipped = Row(LONG_TEXT, TextOverflow.Clip);
            VisualElement ellipsised = Row(LONG_TEXT, TextOverflow.Ellipsis);

            Layout(clipped);
            Layout(ellipsised);

            Assert.IsTrue(ellipsised.TextLines[0].Length < clipped.TextLines[0].Length);
        }

        [TestMethod]
        public void ATextThatAlreadyFitsIsUntouched()
        {
            Assert.AreEqual("short", SingleLine(Row("short", TextOverflow.Ellipsis)));
        }

        [TestMethod]
        public void NoTrailingSpaceIsLeftBeforeTheEllipsis()
        {
            string line = SingleLine(Row(LONG_TEXT, TextOverflow.Ellipsis));

            Assert.IsFalse(line.Contains(" " + ELLIPSIS), $"'{line}' kept a space before the ellipsis");
        }

        [TestMethod]
        public void AVeryNarrowElementKeepsOnlyTheEllipsis()
        {
            Assert.AreEqual(ELLIPSIS, SingleLine(Row(LONG_TEXT, TextOverflow.Ellipsis, TextWrap.NoWrap, 6)));
        }

        [TestMethod]
        public void AWiderElementKeepsMoreCharacters()
        {
            string narrow = SingleLine(Row(LONG_TEXT, TextOverflow.Ellipsis, TextWrap.NoWrap, 100));
            string wide = SingleLine(Row(LONG_TEXT, TextOverflow.Ellipsis, TextWrap.NoWrap, 250));

            Assert.IsTrue(wide.Length > narrow.Length, $"narrow='{narrow}' wide='{wide}'");
            Assert.IsTrue(wide.EndsWith(ELLIPSIS) && narrow.EndsWith(ELLIPSIS));
        }

        [TestMethod]
        public void WrappingStillAppliesAndOnlyAnOverlongWordIsShortened()
        {
            VisualElement wrapped = Row("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa short words here",
                TextOverflow.Ellipsis, TextWrap.Wrap, 120);

            Layout(wrapped);

            Assert.IsTrue(wrapped.TextLines.Count > 1, "wrapping is unaffected by the overflow style");
            Assert.IsTrue(wrapped.TextLines[0].EndsWith(ELLIPSIS),
                $"the unbreakable word should be shortened: '{wrapped.TextLines[0]}'");
            Assert.IsFalse(wrapped.TextLines.Last().EndsWith(ELLIPSIS),
                "lines that fit are left alone");
        }

        [TestMethod]
        public void AContentWidthShortensAtTheOfferedBound()
        {
            var host = Element("host", LayoutType.Column, SizeUnit.Pixels, 120, SizeUnit.Pixels, 400);
            var label = Element("label", LayoutType.Column, SizeUnit.Content, 0, SizeUnit.Content, 0);
            label.Styles.FontSize = new FontSizeStyleDescriptor { Value = 16 };
            label.Styles.TextWrap = new TextWrapStyleDescriptor { Value = TextWrap.NoWrap };
            label.Styles.TextOverflow = new TextOverflowStyleDescriptor { Value = TextOverflow.Ellipsis };
            label.Text = LONG_TEXT;
            host.AddChild(label);

            Layout(host);

            Assert.IsTrue(label.TextLines[0].EndsWith(ELLIPSIS));
            Assert.IsTrue(label.Width <= 120, $"the label should stay inside its host, was {label.Width}");
        }

        [TestMethod]
        public void ThePaddingReducesTheRoomForText()
        {
            string bare = SingleLine(Row(LONG_TEXT, TextOverflow.Ellipsis, TextWrap.NoWrap, 200));

            VisualElement padded = WithPadding(
                Row(LONG_TEXT, TextOverflow.Ellipsis, TextWrap.NoWrap, 200), 30);

            Assert.IsTrue(SingleLine(padded).Length < bare.Length, "padding leaves fewer characters");
        }

        [TestMethod]
        public void TheOverflowStyleComesThroughXns()
        {
            var xnsSource = new XnsSource("label {\r\n    text-overflow: ellipsis\r\n}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(TextOverflow.Ellipsis,
                set.Classes[0].Styles.OfType<TextOverflowStyleDescriptor>().Single().Value);
        }

        [TestMethod]
        public void AnUnsupportedOverflowValueIsReported()
        {
            var xnsSource = new XnsSource("label {\r\n    text-overflow: fade\r\n}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, "'fade' is not an overflow mode");
        }
    }
}
