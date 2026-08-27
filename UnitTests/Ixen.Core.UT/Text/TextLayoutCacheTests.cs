using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class TextLayoutCacheTests
    {
        private const int VIEWPORT = 600;
        private const string CONTENT = "measure arrange clip render, and then do it all again";

        private sealed class CountingMeasurer : ITextMeasurer
        {
            private readonly ITextMeasurer _inner = SkiaTextMeasurer.Default;

            internal int Runs { get; set; }

            public void MeasureText(string text, FontSpec font, out float width, out float height)
                => _inner.MeasureText(text, font, out width, out height);

            public void MeasureCharacters(string text, FontSpec font, float[] advances)
            {
                Runs++;
                _inner.MeasureCharacters(text, font, advances);
            }

            public float GetLineHeight(FontSpec font) => _inner.GetLineHeight(font);
        }

        private VisualElement _label;
        private IxenSurface _surface;
        private CountingMeasurer _measurer;

        [TestInitialize]
        public void Setup()
        {
            _label = new VisualElement { Name = "label", Text = CONTENT };
            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.AddChild(_label);

            _measurer = new CountingMeasurer();
            _surface = new IxenSurface(root) { Styles = new StyleRegistry(), TextMeasurer = _measurer };

            Layout();

            _measurer.Runs = 0;
        }

        private void Layout()
        {
            _label.InvalidateLayout();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private string Lines() => string.Join("|", _label.TextLines);

        [TestMethod]
        public void RelayingOutWithNothingChangedMeasuresNothing()
        {
            string before = Lines();

            for (int pass = 0; pass < 5; pass++)
            {
                Layout();
            }

            Assert.AreEqual(0, _measurer.Runs,
                "the text, the font, the wrap flags and the offered width are all in the cache key, "
                + "so five relayouts that change none of them measure nothing at all - a scroll of a "
                + "long list used to re-measure every row and produce the same answer");

            Assert.AreEqual(before, Lines(), "and the lines are still the ones that were built");
        }

        [TestMethod]
        public void AScrollDoesNotRemeasureTheContent()
        {
            _label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _label.Scrollable = true;

            Layout();
            _measurer.Runs = 0;

            _label.ScrollY = 4;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0, _measurer.Runs, "scrolling moves offsets, it does not change the text");
        }

        [TestMethod]
        public void ChangingTheTextRebuildsTheLines()
        {
            _label.Text = "something else entirely, long enough that it wraps differently";

            Layout();

            Assert.AreEqual(1, _measurer.Runs);
            Assert.IsTrue(Lines().Contains("something"), "the new value really was laid out");
        }

        [TestMethod]
        public void ChangingTheOfferedWidthRebuildsTheLines()
        {
            string narrow = Lines();

            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 560 };
            _label.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, _measurer.Runs);
            Assert.AreNotEqual(narrow, Lines(), "a wider box wraps into fewer lines");
        }

        [TestMethod]
        public void EveryFontPropertyIsPartOfTheKey()
        {
            Check(() => _label.Styles.FontSize = new FontSizeStyleDescriptor { Value = 27 }, "font-size");
            Check(() => _label.Styles.FontWeight = new FontWeightStyleDescriptor { Value = FontWeight.Bold }, "font-weight");
            Check(() => _label.Styles.FontStyle = new FontStyleStyleDescriptor { Value = FontStyle.Italic }, "font-style");
            Check(() => _label.Styles.FontFamily = new FontFamilyStyleDescriptor { Value = "Courier New" }, "font-family");
            Check(() => _label.Styles.LineHeight = new LineHeightStyleDescriptor
            {
                Kind = LineHeightKind.Multiplier,
                Value = 2.5f
            }, "line-height");
            Check(() => _label.Styles.LetterSpacing = new LetterSpacingStyleDescriptor
            {
                Kind = LetterSpacingKind.Pixels,
                Value = 3
            }, "letter-spacing");
            Check(() => _label.Styles.TextWrap = new TextWrapStyleDescriptor { Value = TextWrap.NoWrap }, "text-wrap");
            Check(() => _label.Styles.TextOverflow = new TextOverflowStyleDescriptor
            {
                Value = TextOverflow.Ellipsis
            }, "text-overflow");
        }

        private void Check(System.Action change, string what)
        {
            _measurer.Runs = 0;

            change();

            _label.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, _measurer.Runs,
                $"{what} changes what the text measures to, so it has to be part of the cache key - "
                + "a cache that is keyed on every one of its inputs is the whole reason this one "
                + "needs no invalidation protocol");
        }

        [TestMethod]
        public void SwappingTheMeasurerRebuildsTheLines()
        {
            var second = new CountingMeasurer();

            _surface.TextMeasurer = second;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, second.Runs,
                "the measurer is an input too, and Root.Invalidate() cannot reach a cache that "
                + "lives on the element - so it is compared by reference in the key");
        }

        [TestMethod]
        public void EmptyingTheTextAndPuttingItBackDoesNotResurrectClearedLines()
        {
            _label.Text = string.Empty;

            Layout();

            Assert.AreEqual(0, _label.TextLines.Count, "the lines were cleared");

            _label.Text = CONTENT;

            Layout();

            Assert.IsTrue(_label.TextLines.Count > 0,
                "the same value, the same font and the same width would match a cache left valid, "
                + "and the lines it points at have been cleared - so the empty-text path resets it");

            Assert.IsTrue(_label.ActualHeight > 0, "and the element is sized from them again");
        }
    }
}
