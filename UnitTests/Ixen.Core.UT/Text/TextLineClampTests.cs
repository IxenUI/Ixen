using Ixen.Core.UT.Layout.Geometry;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class TextLineClampTests : BaseGeometryTests
    {
        private const string LONG_TEXT =
            "the quick brown fox jumps over the lazy dog and then walks back again slowly";

        private const string ELLIPSIS = "…";

        private static VisualElement Label(TextOverflow overflow, int maxLines, SizeUnit heightUnit = SizeUnit.Content,
            float height = 0, string text = LONG_TEXT)
        {
            VisualElement element = Element("label", LayoutType.Column, SizeUnit.Pixels, 120, heightUnit, height);

            element.Styles.FontSize = new FontSizeStyleDescriptor { Value = 16 };
            element.Styles.LineHeight = new LineHeightStyleDescriptor { Kind = LineHeightKind.Pixels, Value = 20 };
            element.Styles.TextOverflow = new TextOverflowStyleDescriptor { Value = overflow, MaxLines = maxLines };
            element.Text = text;

            return element;
        }

        private static string LastLine(VisualElement element)
            => element.TextLines[element.TextLines.Count - 1];

        [TestMethod]
        public void AContentSizedLabelStopsAtItsLines()
        {
            VisualElement label = Label(TextOverflow.Ellipsis, 2);

            Layout(label);

            Assert.AreEqual(2, label.TextLines.Count);
            Assert.IsTrue(LastLine(label).EndsWith(ELLIPSIS), "the cut is shown where the text stops");
            Assert.AreEqual(40f, label.ActualHeight, "and the label is two lines tall, not as tall as the text");
        }

        [TestMethod]
        public void ClipCutsWithoutAnEllipsis()
        {
            VisualElement label = Label(TextOverflow.Clip, 2);

            Layout(label);

            Assert.AreEqual(2, label.TextLines.Count);
            Assert.IsFalse(LastLine(label).EndsWith(ELLIPSIS));
        }

        [TestMethod]
        public void AShortTextIsUntouched()
        {
            VisualElement label = Label(TextOverflow.Ellipsis, 3, text: "short");

            Layout(label);

            Assert.AreEqual(1, label.TextLines.Count);
            Assert.AreEqual("short", LastLine(label));
        }

        [TestMethod]
        public void TheTighterOfHeightAndLinesWins()
        {
            VisualElement byLines = Label(TextOverflow.Ellipsis, 2, SizeUnit.Pixels, 60);
            VisualElement byHeight = Label(TextOverflow.Ellipsis, 5, SizeUnit.Pixels, 60);

            Layout(byLines);
            Layout(byHeight);

            Assert.AreEqual(2, byLines.TextLines.Count, "two lines asked for, three would fit");
            Assert.AreEqual(3, byHeight.TextLines.Count, "five asked for, three fit");
        }

        [TestMethod]
        public void ChangingTheLimitLaysTheTextOutAgain()
        {
            VisualElement label = Label(TextOverflow.Ellipsis, 2);
            VisualElement root = Layout(label);

            label.Styles.TextOverflow = new TextOverflowStyleDescriptor { Value = TextOverflow.Ellipsis, MaxLines = 3 };
            label.Invalidate();
            Layout(root);

            Assert.AreEqual(3, label.TextLines.Count, "the line count is part of what the layout cache keys on");
        }

        [TestMethod]
        public void TheLineCountIsWrittenAfterTheKeyword()
        {
            var parser = new TextOverflowStyleParser("ellipsis 3");

            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual(TextOverflow.Ellipsis, parser.Descriptor.Value);
            Assert.AreEqual(3, parser.Descriptor.MaxLines);

            Assert.AreEqual(0, new TextOverflowStyleParser("clip").Descriptor.MaxLines);
            Assert.IsFalse(new TextOverflowStyleParser("ellipsis 0").IsValid, "no line at all is not a limit");
            Assert.IsFalse(new TextOverflowStyleParser("ellipsis three").IsValid);
            Assert.IsFalse(new TextOverflowStyleParser("ellipsis 2 3").IsValid);
            Assert.IsFalse(new TextOverflowStyleParser("ellipsis -2").IsValid);
        }
    }
}
