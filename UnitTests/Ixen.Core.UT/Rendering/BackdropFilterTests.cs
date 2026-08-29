using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class BackdropFilterTests
    {
        private const int WIDTH = 200;
        private const int HEIGHT = 200;

        private VisualElement _root;
        private VisualElement _stripes;
        private VisualElement _panel;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        [TestInitialize]
        public void Setup()
        {
            _root = Box("root", 0, 0, WIDTH, HEIGHT, "#FFFFFF");
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            _stripes = Box("stripes", 0, 0, WIDTH, HEIGHT, null);
            _stripes.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            for (int index = 0; index < 10; index++)
            {
                _stripes.AddChild(Box("stripe" + index, 0, index * 20, WIDTH, 10, "#000000"));
            }

            _panel = Box("panel", 40, 40, 120, 120, null);

            _root.AddChildren(_stripes, _panel);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _bitmap = new SKBitmap(WIDTH, HEIGHT);
            _canvas = new SKCanvas(_bitmap);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
        }

        private static VisualElement Box(string name, float x, float y, float width, float height,
            string colour)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = x };
            element.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = y };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            if (colour != null)
            {
                element.Styles.Background = new BackgroundStyleDescriptor { Color = colour };
            }

            return element;
        }

        private void Blur(VisualElement element, float radius)
        {
            element.Styles.BackdropFilter = new BackdropFilterStyleDescriptor
            {
                Operations = new List<FilterOperation>
                {
                    new FilterOperation { Kind = FilterKind.Blur, Value = radius }
                }
            };

            element.Invalidate();
        }

        private void Frame()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);
            _surface.Render(_canvas);
        }

        private int Grey(int x, int y)
        {
            SKColor colour = _bitmap.GetPixel(x, y);

            return (colour.Red + colour.Green + colour.Blue) / 3;
        }

        private int Contrast(int x)
        {
            int light = 0;
            int dark = 255;

            for (int y = 42; y < 158; y++)
            {
                int grey = Grey(x, y);

                if (grey > light) { light = grey; }
                if (grey < dark) { dark = grey; }
            }

            return light - dark;
        }

        [TestMethod]
        public void WithoutItTheStripesBehindStaySharp()
        {
            Frame();

            Assert.IsTrue(Contrast(100) > 200,
                "black on white through an element that paints nothing: the stripes are simply "
                + "there, at full contrast");
        }

        [TestMethod]
        public void ItBlursWhatIsBehindTheElement()
        {
            Blur(_panel, 4);
            Frame();

            Assert.IsTrue(Contrast(100) < 150,
                "the backdrop is what gets filtered, so the stripes read through the panel as a "
                + "smear rather than as stripes - this is the frosted panel everybody asks for "
                + "first, and filter cannot do it because filter blurs the element itself");
        }

        [TestMethod]
        public void AndOnlyInsideItsOwnBox()
        {
            Blur(_panel, 4);
            Frame();

            Assert.IsTrue(Contrast(20) > 200,
                "outside the panel the same stripes are untouched, so the layer really is "
                + "clipped to the element rather than applied to the frame");
        }

        [TestMethod]
        public void TheElementStillPaintsItselfOnTop()
        {
            Blur(_panel, 4);
            _panel.Styles.Background = new BackgroundStyleDescriptor { Color = "#40FF0000" };
            _panel.Invalidate();

            Frame();

            SKColor middle = _bitmap.GetPixel(100, 100);

            Assert.IsTrue(middle.Red > middle.Blue + 20,
                "a backdrop filter does not blur the element's own painting - the translucent "
                + "red is drawn normally over the blurred backdrop, which is the whole point");
        }

        [TestMethod]
        public void ARadiusClipsTheFrostedArea()
        {
            Blur(_panel, 4);

            _panel.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 60,
                TopRight = 60,
                BottomRight = 60,
                BottomLeft = 60
            };

            _panel.Invalidate();

            Frame();

            Assert.IsTrue(Contrast(45) > 200,
                "the corner of the panel's box is outside its rounded shape, so the stripes "
                + "there are as sharp as anywhere else");
        }

        [TestMethod]
        public void ItComesFromXnsEndToEnd()
        {
            var source = new XnsSource("panel { backdrop-filter: blur(4px) }");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors);

            var registry = new StyleRegistry();

            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();

            Frame();

            Assert.IsTrue(Contrast(100) < 150,
                "the ApplyStyle arm is only ever reached by a rule from a stylesheet");
        }

        [TestMethod]
        public void NoneIsAcceptedAndDoesNothing()
        {
            var source = new XnsSource("panel { backdrop-filter: none }");

            source.Compile();

            Assert.IsFalse(source.HasErrors);

            var refused = new XnsSource("panel { backdrop-filter: frost(4px) }");

            refused.Compile();

            Assert.IsTrue(refused.HasErrors, "an unknown function is a diagnostic, not a no-op");
        }
    }
}
