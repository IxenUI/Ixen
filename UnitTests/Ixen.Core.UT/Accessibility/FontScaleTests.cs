using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class FontScaleTests
    {
        private const int VIEWPORT = 400;
        private const float SIZE = 20f;

        private VisualElement _root;
        private VisualElement _panel;
        private VisualElement _label;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _panel = new VisualElement { Name = "panel" };
            _panel.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _panel.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _label = new VisualElement { Name = "label", Text = "Hxy" };
            _label.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            _label.Styles.Color = new ColorStyleDescriptor { Value = "#000000" };
            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            _label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _panel.AddChild(_label);
            _root.AddChild(_panel);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private static StyleDescriptor Compile(string property, string value)
        {
            var source = new XnsSource($"probe {{ {property}: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set.Classes.Single().Styles.Single();
        }

        private void DeclareLineHeight(string value)
        {
            _label.Styles.LineHeight = (LineHeightStyleDescriptor)Compile("line-height", value);
            _label.Invalidate();
        }

        private void DeclareLetterSpacing(string value)
        {
            _label.Styles.LetterSpacing = (LetterSpacingStyleDescriptor)Compile("letter-spacing", value);
            _label.Invalidate();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private float LabelHeight()
        {
            Layout();
            return _label.ActualHeight;
        }

        private float LabelWidth()
        {
            Layout();
            return _label.ActualWidth;
        }

        private static bool HasInk(SKBitmap bitmap, int y)
        {
            for (int x = 0; x < 200; x++)
            {
                if (bitmap.GetPixel(x, y).Red < 128)
                {
                    return true;
                }
            }

            return false;
        }

        private int InkRows()
        {
            Layout();

            using (SKBitmap bitmap = _surface.RenderToBitmap())
            {
                int rows = 0;

                for (int y = 0; y < VIEWPORT; y++)
                {
                    if (HasInk(bitmap, y))
                    {
                        rows++;
                    }
                }

                return rows;
            }
        }

        [TestMethod]
        public void TheDefaultScaleIsOne()
        {
            Assert.AreEqual(1f, new IxenSurface().FontScale);
        }

        [TestMethod]
        public void AnInvalidScaleFallsBackToOne()
        {
            var surface = new IxenSurface { FontScale = 0 };
            Assert.AreEqual(1f, surface.FontScale);

            surface.FontScale = -2;
            Assert.AreEqual(1f, surface.FontScale);
        }

        [TestMethod]
        public void AScaledFontMeasuresTaller()
        {
            float plain = LabelHeight();

            _surface.FontScale = 2;

            float scaled = LabelHeight();

            Assert.IsTrue(scaled > plain * 1.5f,
                $"a doubled font scale measured {scaled} against {plain}");
        }

        [TestMethod]
        public void AScaledFontMeasuresWider()
        {
            float plain = LabelWidth();

            _surface.FontScale = 2;

            float scaled = LabelWidth();

            Assert.IsTrue(scaled > plain * 1.5f,
                $"a doubled font scale measured {scaled} against {plain}");
        }

        [TestMethod]
        public void AScaledFontDrawsBigger()
        {
            int plain = InkRows();

            _surface.FontScale = 2;

            int scaled = InkRows();

            Assert.IsTrue(scaled > plain * 1.5f,
                $"a doubled font scale inked {scaled} rows against {plain}");
        }

        [TestMethod]
        public void MeasuringAndDrawingAgreeOnTheScale()
        {
            _surface.FontScale = 2;

            Layout();

            float measured = _label.ActualHeight;

            using (SKBitmap bitmap = _surface.RenderToBitmap())
            {
                for (int y = (int)measured + 2; y < VIEWPORT; y++)
                {
                    Assert.IsFalse(HasInk(bitmap, y),
                        $"the glyphs reach row {y} while the measure stopped at {measured}");
                }
            }
        }

        [TestMethod]
        public void ANarrowerBoxWrapsSoonerWhenTheFontIsScaled()
        {
            _label.Text = "one two three four five";
            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            float plain = LabelHeight();

            _surface.FontScale = 2;

            Assert.IsTrue(LabelHeight() > plain,
                "a scaled font in a fixed box must wrap onto more lines");
        }

        [TestMethod]
        public void AMultiplierLineHeightScalesWithTheFont()
        {
            DeclareLineHeight("2");

            float plain = LabelHeight();

            _surface.FontScale = 2;

            float scaled = LabelHeight();

            Assert.IsTrue(scaled > plain * 1.5f,
                $"a multiplier line height measured {scaled} against {plain}");
        }

        [TestMethod]
        public void AnAbsoluteLineHeightDoesNotScale()
        {
            DeclareLineHeight("40px");

            float plain = LabelHeight();

            _surface.FontScale = 2;

            Assert.AreEqual(plain, LabelHeight(), 0.01f);
        }

        [TestMethod]
        public void LetterSpacingDoesNotScale()
        {
            _label.Text = "iiii";

            DeclareLetterSpacing("10px");

            float plain = LabelWidth();

            _surface.FontScale = 2;

            float scaled = LabelWidth();

            Assert.IsTrue(scaled < plain * 2f,
                $"the spacing scaled with the font: {scaled} against {plain}");
        }

        [TestMethod]
        public void TheWholeTreeIsMeasuredAgain()
        {
            Layout();
            Layout();

            Assert.IsFalse(_surface.LastLayoutRan, "the second pass should have nothing to do");

            _surface.FontScale = 2;

            Layout();

            Assert.IsTrue(_surface.LastMeasuredElements >= 3,
                $"only {_surface.LastMeasuredElements} elements were measured again");
        }

        [TestMethod]
        public void TheSameScaleTwiceDoesNotRelayout()
        {
            Layout();

            _surface.FontScale = 2;

            Layout();
            Layout();

            Assert.IsFalse(_surface.LastLayoutRan);

            _surface.FontScale = 2;

            Layout();

            Assert.IsFalse(_surface.LastLayoutRan, "an unchanged scale asked for a layout pass");
        }
    }
}
