using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class LetterSpacingTests
    {
        private const int VIEWPORT = 400;
        private const float SIZE = 20f;

        private VisualElement _root;
        private VisualElement _label;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _label = new VisualElement { Name = "label", Text = "abcde" };
            _label.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            _label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _root.AddChild(_label);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                TextMeasurer = new FixedMeasurer()
            };
        }

        private sealed class FixedMeasurer : ITextMeasurer
        {
            internal const float PER_CHAR = 10f;
            internal const float LINE = 24f;

            public void MeasureText(string text, FontSpec font, out float width, out float height)
            {
                if (string.IsNullOrEmpty(text))
                {
                    width = 0;
                    height = GetLineHeight(font);
                    return;
                }

                width = text.Length * PER_CHAR + font.Advance(text);
                height = GetLineHeight(font);
            }

            public float GetLineHeight(FontSpec font)
                => font.LineHeight > 0 ? font.LineHeight : LINE;
        }

        private void Declare(VisualElement element, string value)
        {
            var source = new XnsSource($"probe {{ letter-spacing: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            element.Styles.LetterSpacing = (LetterSpacingStyleDescriptor)set.Classes.Single().Styles.Single();
            element.Invalidate();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private static LetterSpacingStyleDescriptor Parse(string value)
        {
            var source = new XnsSource($"probe {{ letter-spacing: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return (LetterSpacingStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var source = new XnsSource($"probe {{ letter-spacing: {value} }}");
            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'letter-spacing: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, source.Diagnostics[0].Code);
        }

        [TestMethod]
        public void WithNothingDeclaredTheWidthIsJustTheGlyphs()
        {
            Layout();

            Assert.AreEqual(50f, _label.Width, "five characters at ten each");
        }

        [TestMethod]
        public void EachCharacterCarriesItsOwnGap()
        {
            Declare(_label, "4px");
            Layout();

            Assert.AreEqual(70f, _label.Width,
                "the gap follows every character including the last, which is CSS's rule and is "
                + "what makes a prefix measure land exactly where the caret goes");
        }

        [TestMethod]
        public void ANegativeSpacingTightensIt()
        {
            Declare(_label, "-2px");
            Layout();

            Assert.AreEqual(40f, _label.Width);
        }

        [TestMethod]
        public void AWidthThatWouldGoNegativeIsClampedByTheBox()
        {
            Declare(_label, "-40px");
            Layout();

            Assert.AreEqual(0f, _label.Width,
                "the measurer hands back the negative advance and DimensionalElement refuses it, "
                + "which is why neither the measurer nor the renderer carries a clamp of its own");
        }

        [TestMethod]
        public void NormalGoesBackToNoSpacing()
        {
            Declare(_label, "normal");
            Layout();

            Assert.AreEqual(50f, _label.Width);
        }

        [TestMethod]
        public void ItIsInherited()
        {
            var inner = new VisualElement { Name = "inner", Text = "ab" };
            inner.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            inner.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _label.Text = null;
            _label.AddChild(inner);

            Declare(_label, "5px");
            _root.Invalidate();
            Layout();

            Assert.AreEqual(30f, inner.Width, "two characters, two gaps");
        }

        [TestMethod]
        public void NormalStopsAnInheritedSpacing()
        {
            var inner = new VisualElement { Name = "inner", Text = "ab" };
            inner.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            inner.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _label.Text = null;
            _label.AddChild(inner);

            Declare(_label, "5px");
            Declare(inner, "normal");

            _root.Invalidate();
            Layout();

            Assert.AreEqual(20f, inner.Width,
                "which is why Unset and Normal are separate members");
        }

        [TestMethod]
        public void ItDoesNotChangeTheLineHeight()
        {
            Layout();
            float height = _label.Height;

            Declare(_label, "6px");
            Layout();

            Assert.AreEqual(height, _label.Height, "letter-spacing is a horizontal property only");
        }

        [TestMethod]
        public void ItMakesTextWrapEarlier()
        {
            _label.Text = "aaa bbb ccc ddd";
            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 160 };

            Layout();
            int tight = _label.TextLines.Count;

            Declare(_label, "6px");
            Layout();

            Assert.IsTrue(_label.TextLines.Count > tight,
                $"the wrap measures through the same call, so it follows; {tight} lines became "
                + $"{_label.TextLines.Count}");
        }

        [TestMethod]
        public void AFieldsCaretOffsetsFollowIt()
        {
            var field = new TextField { Name = "field", Text = "abc" };

            field.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _label.Text = null;
            _root.AddChild(field);

            Declare(field, "4px");
            _root.Invalidate();
            Layout();

            Assert.AreEqual(0f, field.OffsetAt(0));
            Assert.AreEqual(14f, field.OffsetAt(1), "one character plus its own gap");
            Assert.AreEqual(28f, field.OffsetAt(2));
            Assert.AreEqual(42f, field.OffsetAt(3),
                "so the caret at the end sits exactly at the measured advance");
        }

        [TestMethod]
        public void EveryFormParses()
        {
            Assert.AreEqual(LetterSpacingKind.Normal, Parse("normal").Kind);

            Assert.AreEqual(LetterSpacingKind.Pixels, Parse("2px").Kind);
            Assert.AreEqual(2f, Parse("2px").Value);

            Assert.AreEqual(1.5f, Parse("1.5px").Value);
            Assert.AreEqual(-0.5f, Parse("-0.5px").Value, "tightening is legal");
            Assert.AreEqual(3f, Parse("3").Value, "a bare number is pixels, as for font-size");
            Assert.AreEqual(0f, Parse("0px").Value, "zero is a legal no-op, unlike a line height");
        }

        [TestMethod]
        public void AnUndeclaredSpacingIsNotDeclared()
        {
            Assert.IsFalse(new LetterSpacingStyleDescriptor().IsDeclared);
            Assert.AreEqual(0f, new LetterSpacingStyleDescriptor().Resolve());
            Assert.IsTrue(Parse("normal").IsDeclared);
            Assert.AreEqual(0f, Parse("normal").Resolve());
        }

        [TestMethod]
        public void NonsenseIsRejected()
        {
            AssertRejected("wide");
            AssertRejected("1*");
            AssertRejected("?");
            AssertRejected("2em");
            AssertRejected("50%");
        }

        [TestMethod]
        public void ItRoundTripsThroughGeneratedSource()
        {
            string source = Parse("-0.5px").ToSource();

            StringAssert.Contains(source, "LetterSpacingKind.Pixels");
            StringAssert.Contains(source, "Value = -0.5f");
        }
    }
}
