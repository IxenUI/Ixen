using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class CaretOffsetCostTests
    {
        private const int VIEWPORT = 1282;
        private const int LENGTH = 2000;

        private const long BUDGET = 200 * 1024;

        private static TextField Field(string text)
        {
            var field = new TextField
            {
                Name = "field",
                Multiline = true,
                Text = text
            };

            field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 600 };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };

            return field;
        }

        private static IxenSurface Surface(TextField field)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.AddChild(field);

            return new IxenSurface(root) { Styles = new StyleRegistry() };
        }

        [TestMethod]
        public void LayingOutALongFieldDoesNotAllocatePerCharacter()
        {
            TextField field = Field(new string('x', LENGTH));
            IxenSurface surface = Surface(field);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            long before = System.GC.GetAllocatedBytesForCurrentThread();

            for (int pass = 0; pass < 10; pass++)
            {
                field.InvalidateLayout();
                surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }

            long each = (System.GC.GetAllocatedBytesForCurrentThread() - before) / 10;

            Assert.IsTrue(each < BUDGET,
                $"one layout of a {LENGTH} character field allocated {each / 1024} KB. The caret "
                + "offsets used to come from measuring a growing Substring per character, which is "
                + $"quadratic in both time and garbage - about {(long)LENGTH * LENGTH / 2 / 1024} KB "
                + "of strings a layout here, and 23 ms a keystroke at 1600 characters. They now come "
                + "from one pass of per-character advances through ITextMeasurer.MeasureCharacters "
                + "plus a running sum, which allocates nothing.");
        }

        [TestMethod]
        public void TheRunningSumStillAgreesWithMeasuringTheWholeLine()
        {
            TextField field = Field("The wild swans at Coole are drifting on the still water");
            IxenSurface surface = Surface(field);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, field.LineCount, "the sample fits one line");

            FontSpec spec = FontSpec.From(field.StylesHandlers);

            Ixen.Core.Rendering.SkiaTextMeasurer.Default.MeasureText(
                field.Text, spec, out float whole, out _);

            Assert.AreEqual(whole, field.OffsetAt(field.Text.Length), 0.01f,
                "the sum of the per-character advances must equal the whole-string measurement, "
                + "which is only true because Skia applies no kerning without a shaper "
                + "(Rendering/GlyphAdvanceTests)");
        }
    }
}
