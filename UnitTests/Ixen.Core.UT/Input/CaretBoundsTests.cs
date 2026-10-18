using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class CaretBoundsTests
    {
        private const int VIEWPORT = 240;
        private const string INK = "#FF0000";

        private TextField _field;
        private IxenSurface _surface;

        private void Build(TextField field, float height, TextVAlign? vertical)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.Padding = Pixels(12);

            _field = field;
            _field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 180 };
            _field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            _field.Styles.Padding = Pixels(7);
            _field.Styles.Color = new ColorStyleDescriptor { Value = INK };

            if (vertical.HasValue)
            {
                _field.Styles.TextAlign = new TextAlignStyleDescriptor { Vertical = vertical.Value };
            }

            root.AddChild(_field);

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Focus(_field);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static PaddingStyleDescriptor Pixels(float value)
            => new PaddingStyleDescriptor
            {
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value },
                Bottom = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value },
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value }
            };

        private void AssertBoundsAreTheInk(int index)
        {
            _field.CaretBounds(index, out float x, out float y, out float height);

            using (SKBitmap bitmap = _surface.RenderToBitmap())
            {
                int column = (int)System.Math.Floor(x);
                int first = -1;
                int last = -1;

                for (int candidate = column; candidate <= column + 1 && first < 0; candidate++)
                {
                    for (int row = 0; row < bitmap.Height; row++)
                    {
                        SKColor pixel = bitmap.GetPixel(candidate, row);

                        if (pixel.Alpha > 128 && pixel.Red > 128 && pixel.Green < 100)
                        {
                            first = first < 0 ? row : first;
                            last = row;
                        }
                    }
                }

                Assert.IsTrue(first >= 0, $"no caret ink in column {column}");
                Assert.AreEqual(y, first, 1.01, "the top of the caret");
                Assert.AreEqual(y + height, last + 1, 1.01, "the bottom of the caret");
            }
        }

        [TestMethod]
        public void AMiddleAlignedFieldPutsTheBoundsWhereTheCaretIsDrawn()
        {
            Build(new TextField(), 60, null);

            AssertBoundsAreTheInk(0);
        }

        [TestMethod]
        public void ATopAlignedFieldToo()
        {
            Build(new TextField(), 60, TextVAlign.Top);

            AssertBoundsAreTheInk(0);
        }

        [TestMethod]
        public void ABottomAlignedFieldToo()
        {
            Build(new TextField(), 60, TextVAlign.Bottom);

            AssertBoundsAreTheInk(0);
        }

        [TestMethod]
        public void TheBoundsFollowTheCaretAlongTheText()
        {
            Build(new TextField(), 30, null);

            _field.Text = "    ";
            _field.CaretIndex = 4;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _field.CaretBounds(0, out float start, out _, out _);
            _field.CaretBounds(4, out float end, out _, out _);

            Assert.IsTrue(end > start + 4, "four spaces moved the caret");

            AssertBoundsAreTheInk(4);
        }

        [TestMethod]
        public void AnAreaPutsThemOnTheCaretsOwnLine()
        {
            Build(new TextArea(), 120, null);

            _field.Text = "\n\n";
            _field.CaretIndex = 2;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _field.CaretBounds(0, out _, out float top, out float height);
            _field.CaretBounds(2, out _, out float third, out _);

            Assert.AreEqual(top + 2 * height, third, 0.01);

            AssertBoundsAreTheInk(2);
        }
    }
}
