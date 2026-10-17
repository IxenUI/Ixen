using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class TextFlowRenderTests
    {
        private const int WIDTH = 300;
        private const int HEIGHT = 100;

        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFFFF" };

            _box = new VisualElement { Name = "box", Text = "ab" };
            _box.Styles.Color = new ColorStyleDescriptor { Value = "#FF000000" };
            _box.Styles.FontSize = new FontSizeStyleDescriptor { Value = 20 };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Item(float width, float height)
        {
            var item = new VisualElement { Name = "item" };
            item.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            item.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            _box.AddChild(item);
        }

        private SKBitmap Frame()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);

            var bitmap = new SKBitmap(WIDTH, HEIGHT);

            using (var canvas = new SKCanvas(bitmap))
            {
                _surface.Render(canvas);
            }

            return bitmap;
        }

        private static bool IsInk(SKColor color)
            => color.Red < 200 || color.Green < 200 || color.Blue < 200;

        private static int LastInkedColumn(SKBitmap bitmap)
        {
            for (int x = bitmap.Width - 1; x >= 0; x--)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (IsInk(bitmap.GetPixel(x, y)))
                    {
                        return x;
                    }
                }
            }

            return -1;
        }

        private static int LastInkedRow(SKBitmap bitmap)
        {
            for (int y = bitmap.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (IsInk(bitmap.GetPixel(x, y)))
                    {
                        return y;
                    }
                }
            }

            return -1;
        }

        [TestMethod]
        public void TextIsRightAlignedInsideItsOwnSlotRatherThanInTheWholeRow()
        {
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _box.Styles.TextAlign = new TextAlignStyleDescriptor { Horizontal = TextAlign.Right };
            Item(150, 20);

            using (SKBitmap bitmap = Frame())
            {
                int last = LastInkedColumn(bitmap);

                Assert.IsTrue(last >= 0, "nothing was drawn");
                Assert.IsTrue(last < 200, $"the text ended at column {last}");
            }
        }

        [TestMethod]
        public void TextIsBottomAlignedInsideItsOwnSlotRatherThanInTheWholeColumn()
        {
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 90 };
            _box.Styles.TextAlign = new TextAlignStyleDescriptor { Vertical = TextVAlign.Bottom };
            Item(50, 50);

            using (SKBitmap bitmap = Frame())
            {
                int last = LastInkedRow(bitmap);

                Assert.IsTrue(last >= 0, "nothing was drawn");
                Assert.IsTrue(last < 60, $"the text ended at row {last}");
            }
        }

        [TestMethod]
        public void WithNoChildrenTheTextStillUsesTheWholeContentBox()
        {
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _box.Styles.TextAlign = new TextAlignStyleDescriptor { Horizontal = TextAlign.Right };

            using (SKBitmap bitmap = Frame())
            {
                int last = LastInkedColumn(bitmap);

                Assert.IsTrue(last > 250, $"the text ended at column {last}");
            }
        }

        [TestMethod]
        public void TheTextAndTheChildNoLongerShareOneBox()
        {
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _box.Styles.TextAlign = new TextAlignStyleDescriptor { Horizontal = TextAlign.Right };

            var item = new VisualElement { Name = "item" };
            item.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 150 };
            item.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            item.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFFFF" };
            _box.AddChild(item);

            using (SKBitmap bitmap = Frame())
            {
                int last = LastInkedColumn(bitmap);

                Assert.IsTrue(last >= 0, "the child painted over the whole of the text");
                Assert.IsTrue(last < 150, $"the text ended at column {last}");
            }
        }
    }
}
