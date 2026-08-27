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
    public class LineHeightTests
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

            _label = new VisualElement
            {
                Name = "label",
                Text = "one\ntwo\nthree"
            };

            _label.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
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
            internal const float NATURAL = 24f;
            internal const float PER_CHAR = 8f;

            public void MeasureText(string text, FontSpec font, out float width, out float height)
            {
                width = string.IsNullOrEmpty(text) ? 0 : text.Length * PER_CHAR;
                height = GetLineHeight(font);
            }

            public void MeasureCharacters(string text, FontSpec font, float[] advances)
            {
                for (int index = 0; index < (text == null ? 0 : text.Length); index++)
                {
                    advances[index] = PER_CHAR;
                }
            }

            public float GetLineHeight(FontSpec font)
                => font.LineHeight > 0 ? font.LineHeight : NATURAL;
        }

        private void Declare(VisualElement element, string value)
        {
            var source = new XnsSource($"probe {{ line-height: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            element.Styles.LineHeight = (LineHeightStyleDescriptor)set.Classes.Single().Styles.Single();
            element.Invalidate();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private static LineHeightStyleDescriptor Parse(string value)
        {
            var source = new XnsSource($"probe {{ line-height: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return (LineHeightStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var source = new XnsSource($"probe {{ line-height: {value} }}");
            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'line-height: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, source.Diagnostics[0].Code);
        }

        [TestMethod]
        public void WithNothingDeclaredTheFontDecides()
        {
            Layout();

            Assert.AreEqual(FixedMeasurer.NATURAL * 3, _label.Height,
                "three lines at the font's own spacing");
        }

        [TestMethod]
        public void AMultiplierScalesTheFontSize()
        {
            Declare(_label, "2");
            Layout();

            Assert.AreEqual(SIZE * 2 * 3, _label.Height,
                "a bare number is a multiple of the font size, not a pixel count");
        }

        [TestMethod]
        public void ALengthIsAbsolute()
        {
            Declare(_label, "30px");
            Layout();

            Assert.AreEqual(90f, _label.Height);
        }

        [TestMethod]
        public void APercentageIsOfTheFontSize()
        {
            Declare(_label, "150%");
            Layout();

            Assert.AreEqual(SIZE * 1.5f * 3, _label.Height);
        }

        [TestMethod]
        public void NormalGoesBackToTheFontMetrics()
        {
            Declare(_label, "normal");
            Layout();

            Assert.AreEqual(FixedMeasurer.NATURAL * 3, _label.Height);
        }

        [TestMethod]
        public void ATighterLineHeightShrinksTheBox()
        {
            Declare(_label, "12px");
            Layout();

            Assert.AreEqual(36f, _label.Height,
                "below the font's own spacing the lines close up, as in CSS");
        }

        [TestMethod]
        public void ItIsInherited()
        {
            var inner = new VisualElement { Name = "inner", Text = "a\nb" };
            inner.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _label.Text = null;
            _label.AddChild(inner);

            Declare(_label, "40px");
            _root.Invalidate();
            Layout();

            Assert.AreEqual(80f, inner.Height, "declaring it on a container sets the lines inside");
        }

        [TestMethod]
        public void NormalStopsAnInheritedLineHeight()
        {
            var inner = new VisualElement { Name = "inner", Text = "a\nb" };
            inner.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _label.Text = null;
            _label.AddChild(inner);

            Declare(_label, "40px");
            Declare(inner, "normal");

            _root.Invalidate();
            Layout();

            Assert.AreEqual(FixedMeasurer.NATURAL * 2, inner.Height,
                "which is the whole reason Unset and Normal are different members");
        }

        [TestMethod]
        public void AnInheritedMultiplierFollowsTheChildsOwnFontSize()
        {
            var inner = new VisualElement { Name = "inner", Text = "a\nb" };
            inner.Styles.FontSize = new FontSizeStyleDescriptor { Value = 10 };
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _label.Text = null;
            _label.AddChild(inner);

            Declare(_label, "3");
            _root.Invalidate();
            Layout();

            Assert.AreEqual(60f, inner.Height,
                "a multiplier is resolved against the element that uses it, which is why "
                + "a number is the form worth reaching for");
        }

        [TestMethod]
        public void ItDoesNotChangeTheMeasuredWidth()
        {
            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            Layout();

            float width = _label.Width;

            Declare(_label, "40px");
            Layout();

            Assert.AreEqual(width, _label.Width, "line-height is a vertical property only");
        }

        [TestMethod]
        public void ItReachesTheWrapBoundToo()
        {
            _label.Text = "one two three four five six seven eight";
            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };

            Layout();

            int lines = _label.TextLines.Count;

            Declare(_label, "2");
            Layout();

            Assert.AreEqual(lines, _label.TextLines.Count, "the wrap is unchanged");

            Assert.AreEqual(SIZE * 2 * lines, _label.Height,
                "but every wrapped line takes the declared height");
        }

        [TestMethod]
        public void AFieldsCaretAndLinesFollowIt()
        {
            var field = new TextField
            {
                Name = "field",
                Text = "a\nb\nc",
                Multiline = true
            };

            field.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _label.Text = null;
            _root.AddChild(field);

            Declare(field, "36px");
            _root.Invalidate();
            Layout();

            Assert.AreEqual(36f, field.LineHeight,
                "the field's line model is a measure output and reads the same resolved value");
        }

        [TestMethod]
        public void EveryFormParses()
        {
            Assert.AreEqual(LineHeightKind.Normal, Parse("normal").Kind);

            Assert.AreEqual(LineHeightKind.Multiplier, Parse("1.5").Kind);
            Assert.AreEqual(1.5f, Parse("1.5").Value);

            Assert.AreEqual(LineHeightKind.Pixels, Parse("24px").Kind);
            Assert.AreEqual(24f, Parse("24px").Value);

            Assert.AreEqual(LineHeightKind.Percents, Parse("150%").Kind);
            Assert.AreEqual(150f, Parse("150%").Value);
        }

        [TestMethod]
        public void AnUndeclaredLineHeightIsNotDeclared()
        {
            Assert.IsFalse(new LineHeightStyleDescriptor().IsDeclared);
            Assert.IsTrue(Parse("normal").IsDeclared, "so that it can stop an inherited value");
            Assert.AreEqual(0f, new LineHeightStyleDescriptor().Resolve(20));
        }

        [TestMethod]
        public void NonsenseIsRejected()
        {
            AssertRejected("0");
            AssertRejected("0px");
            AssertRejected("-2");
            AssertRejected("1*");
            AssertRejected("?");
            AssertRejected("tall");
        }

        [TestMethod]
        public void ItRoundTripsThroughGeneratedSource()
        {
            string source = Parse("1.5").ToSource();

            StringAssert.Contains(source, "LineHeightKind.Multiplier");
            StringAssert.Contains(source, "Value = 1.5f");
        }
    }
}
