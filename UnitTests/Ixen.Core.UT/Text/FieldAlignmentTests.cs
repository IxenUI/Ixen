using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class FieldAlignmentTests
    {
        private const int SIZE = 200;

        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private T Add<T>(string name) where T : TextField, new()
        {
            var field = new T { Name = name, Text = "Ixen" };

            field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 160 };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 90 };
            field.Styles.FontSize = new FontSizeStyleDescriptor { Value = 18 };
            field.Styles.Color = new ColorStyleDescriptor { Value = "#FF000000" };

            _root.AddChild(field);

            return field;
        }

        private int FirstPaintedRow()
        {
            _surface.ComputeLayout(SIZE, SIZE);

            using (var bitmap = new SKBitmap(SIZE, SIZE))
            {
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.White);
                    _surface.Render(canvas);
                }

                for (int y = 0; y < SIZE; y++)
                {
                    for (int x = 0; x < SIZE; x++)
                    {
                        SKColor pixel = bitmap.GetPixel(x, y);

                        if (pixel.Red < 128 && pixel.Alpha > 0)
                        {
                            return y;
                        }
                    }
                }
            }

            return -1;
        }

        [TestMethod]
        public void AFieldCentresItsTextWithNoStylesheetAtAll()
        {
            Add<TextField>("name");

            int first = FirstPaintedRow();

            Assert.IsTrue(first > 25,
                "a one-line field whose text sits against its top edge is wrong in every case, "
                + "and text-align defaults to top - so the field says so itself");
            Assert.IsTrue(first < 55);
        }

        [TestMethod]
        public void AnAreaIgnoresVerticalAlignmentAltogether()
        {
            TextArea area = Add<TextArea>("notes");

            int first = FirstPaintedRow();

            area.Styles.TextAlign = new TextAlignStyleDescriptor
            {
                Horizontal = TextAlign.Left,
                Vertical = TextVAlign.Middle
            };

            area.Invalidate();

            Assert.AreEqual(first, FirstPaintedRow(),
                "an area scrolls, so its text starts at the top whatever is asked of it. That is "
                + "why TextArea needs no alignment of its own - a line setting it back to top "
                + "was dead code, measured rather than assumed.");
            Assert.IsTrue(first < 25);
        }

        [TestMethod]
        public void AStylesheetStillWins()
        {
            TextField field = Add<TextField>("name");

            var xns = new XnsSource("name { text-align: left top }");
            ClassesSet set = xns.Compile();

            Assert.IsFalse(xns.HasErrors);

            var registry = new StyleRegistry();
            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();

            int first = FirstPaintedRow();

            Assert.IsTrue(first < 25,
                "the field states this as an INLINE style, so the cascade beats it - which is "
                + "what keeps it a sensible default rather than a rule nobody can change");
            Assert.AreEqual(TextVAlign.Top, field.StylesHandlers.TextAlign.Descriptor.Vertical);
        }

    }
}
