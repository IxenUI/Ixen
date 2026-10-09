using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.StyleSheets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class CheckMarkTests
    {
        private const int VIEWPORT = 60;

        private VisualElement _root;
        private CheckBox _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _box = new CheckBox { Name = "agree" };
            _root.AddChild(_box);

            var registry = new StyleRegistry();
            registry.AddDefaults(new DefaultTheme_StyleSheet());

            _surface = new IxenSurface(_root) { Styles = registry };
        }

        private void Layout()
        {
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static bool IsAccent(SKColor pixel)
            => pixel.Blue > 180 && pixel.Red < 120 && pixel.Green > 80;

        private static bool IsField(SKColor pixel)
            => pixel.Red > 230 && pixel.Green > 230 && pixel.Blue > 230;

        [TestMethod]
        public void ACheckedBoxDrawsATickRatherThanAFilledSquare()
        {
            _box.Checked = true;
            Layout();

            VisualElement mark = _box.Mark;
            float scale = mark.ActualWidth / 24f;

            using (SKBitmap frame = _surface.RenderToBitmap())
            {
                Assert.IsTrue(
                    IsAccent(frame.GetPixel((int)(mark.X + 9 * scale), (int)(mark.Y + 17 * scale))),
                    "the elbow of the tick is painted in the theme's accent");

                Assert.IsTrue(
                    IsField(frame.GetPixel((int)(mark.X + 1), (int)(mark.Y + 1))),
                    "and the corner of its box still shows the field behind it - which is the "
                    + "whole difference between a shape and the rounded square this used to be");
            }
        }

        [TestMethod]
        public void AnUncheckedBoxDrawsNoTick()
        {
            Layout();

            VisualElement mark = _box.Mark;
            float scale = mark.ActualWidth / 24f;

            using (SKBitmap frame = _surface.RenderToBitmap())
            {
                Assert.IsFalse(
                    IsAccent(frame.GetPixel((int)(mark.X + 9 * scale), (int)(mark.Y + 17 * scale))),
                    "visibility: hidden covers the shape like anything else");
            }
        }

        [TestMethod]
        public void ARadioKeepsItsDotRatherThanTakingTheTick()
        {
            var radio = new RadioButton { Name = "small", Checked = true };
            _root.AddChild(radio);

            Layout();

            Assert.IsInstanceOfType<Path>(radio.Mark);
            Assert.IsNull(((Path)radio.Mark).Data,
                "a radio's mark is its theme's round background, so it declares no shape");
        }

        [TestMethod]
        public void TheTickIsAStrokeSoItSurvivesAnyMarkSize()
        {
            Assert.IsTrue(((Path)_box.Mark).IsStroked,
                "a stroked outline scales with its box, which is what makes one declaration "
                + "serve a mark of any size");
        }
    }
}
