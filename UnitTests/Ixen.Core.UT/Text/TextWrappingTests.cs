using Ixen.Core.UT.Layout.Geometry;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class TextWrappingTests : BaseGeometryTests
    {
        private const string LONG_TEXT = "the quick brown fox jumps over the lazy dog";

        private static VisualElement Label(string text, SizeUnit widthUnit, float widthValue, float fontSize = 16)
        {
            var element = Element("label", LayoutType.Column, widthUnit, widthValue, SizeUnit.Content, 0);
            element.Styles.FontSize = new FontSizeStyleDescriptor { Value = fontSize };
            element.Text = text;
            return element;
        }

        private static VisualElement NoWrap(VisualElement element)
        {
            element.Styles.TextWrap = new TextWrapStyleDescriptor { Value = TextWrap.NoWrap };
            return element;
        }

        private static int LineCount(VisualElement element)
            => element.TextLines == null ? 0 : element.TextLines.Count;

        [TestMethod]
        public void ALongTextWrapsInsideADefiniteWidth()
        {
            VisualElement label = Label(LONG_TEXT, SizeUnit.Pixels, 120);

            Layout(label);

            Assert.IsTrue(LineCount(label) > 1, $"expected several lines, got {LineCount(label)}");
        }

        [TestMethod]
        public void EachWrappedLineFitsTheWidth()
        {
            VisualElement label = Label(LONG_TEXT, SizeUnit.Pixels, 120);

            Layout(label);

            foreach (string line in label.TextLines)
            {
                Assert.IsFalse(line.Contains("  "), $"'{line}' kept a double space from the break");
                Assert.IsFalse(line.EndsWith(" "), $"'{line}' kept a trailing space");
            }
        }

        [TestMethod]
        public void MoreLinesMakeTheElementTaller()
        {
            VisualElement narrow = Label(LONG_TEXT, SizeUnit.Pixels, 120);
            VisualElement wide = Label(LONG_TEXT, SizeUnit.Pixels, 400);

            Layout(narrow);
            Layout(wide);

            Assert.IsTrue(LineCount(narrow) > LineCount(wide), "the narrow one should wrap more");
            Assert.IsTrue(narrow.Height > wide.Height, $"narrow={narrow.Height} wide={wide.Height}");
        }

        [TestMethod]
        public void TheHeightIsAMultipleOfTheLineHeight()
        {
            VisualElement single = Label("short", SizeUnit.Pixels, 300);
            VisualElement wrapped = Label(LONG_TEXT, SizeUnit.Pixels, 120);

            Layout(single);
            Layout(wrapped);

            Assert.AreEqual(1, LineCount(single));
            Assert.AreEqual(single.Height * LineCount(wrapped), wrapped.Height, 0.01f);
        }

        [TestMethod]
        public void NoWrapKeepsEverythingOnOneLine()
        {
            VisualElement label = NoWrap(Label(LONG_TEXT, SizeUnit.Pixels, 120));

            Layout(label);

            Assert.AreEqual(1, LineCount(label));
            Assert.AreEqual(LONG_TEXT, label.TextLines[0]);
        }

        [TestMethod]
        public void AWordWiderThanTheElementIsNotBroken()
        {
            VisualElement label = Label("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", SizeUnit.Pixels, 40);

            Layout(label);

            Assert.AreEqual(1, LineCount(label), "there is no character-level breaking");
            Assert.AreEqual("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", label.TextLines[0]);
        }

        [TestMethod]
        public void AnOverlongWordKeepsTheFollowingWordsOnTheirOwnLine()
        {
            VisualElement label = Label("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa bb", SizeUnit.Pixels, 40);

            Layout(label);

            Assert.AreEqual(2, LineCount(label));
            Assert.AreEqual("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", label.TextLines[0]);
            Assert.AreEqual("bb", label.TextLines[1]);
        }

        [TestMethod]
        public void ANewlineIsAHardBreak()
        {
            VisualElement label = Label("first\nsecond", SizeUnit.Pixels, 400);

            Layout(label);

            Assert.AreEqual(2, LineCount(label));
            Assert.AreEqual("first", label.TextLines[0]);
            Assert.AreEqual("second", label.TextLines[1]);
        }

        [TestMethod]
        public void ACarriageReturnIsNotKeptInTheLine()
        {
            VisualElement label = Label("first\r\nsecond", SizeUnit.Pixels, 400);

            Layout(label);

            Assert.AreEqual(2, LineCount(label));
            Assert.AreEqual("first", label.TextLines[0]);
        }

        [TestMethod]
        public void ThePaddingReducesTheWrappingWidth()
        {
            VisualElement bare = Label(LONG_TEXT, SizeUnit.Pixels, 200);
            VisualElement padded = WithPadding(Label(LONG_TEXT, SizeUnit.Pixels, 200), 40);

            Layout(bare);
            Layout(padded);

            Assert.IsTrue(LineCount(padded) > LineCount(bare),
                $"bare={LineCount(bare)} padded={LineCount(padded)}");
        }

        [TestMethod]
        public void AContentWidthWrapsAtTheOfferedBound()
        {
            var host = Element("host", LayoutType.Column, SizeUnit.Pixels, 150, SizeUnit.Pixels, 400);
            VisualElement label = Label(LONG_TEXT, SizeUnit.Content, 0);
            host.AddChild(label);

            Layout(host);

            Assert.IsTrue(LineCount(label) > 1, "a ? width still wraps at the bound it was offered");
            Assert.IsTrue(label.Width <= 150, $"the label should not exceed its bound, was {label.Width}");
        }

        [TestMethod]
        public void RemovingTheTextClearsTheLines()
        {
            VisualElement label = Label(LONG_TEXT, SizeUnit.Pixels, 120);
            var host = Element("host");
            host.AddChild(label);

            var surface = new IxenSurface(host);
            surface.ComputeLayout(400, 400);

            Assert.IsTrue(LineCount(label) > 1);

            label.Text = null;
            surface.ComputeLayout(400, 400);

            Assert.AreEqual(0, LineCount(label));
        }
    }
}
