using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class BackgroundHandlerReuseTests
    {
        private const int VIEWPORT = 1282;
        private const int CELLS = 400;
        private const int PASSES = 50;

        private const long BUDGET = 80 * 1024;

        private static IxenSurface Build(string background, out VisualElement first)
        {
            var registry = new StyleRegistry();

            var sheet = new XnsSource($"cell {{ background: {background} }}");
            ClassesSet set = sheet.Compile();

            Assert.IsFalse(sheet.HasErrors, string.Join(" | ", sheet.Diagnostics.Select(d => d.Message)));
            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            first = null;

            for (int index = 0; index < CELLS; index++)
            {
                var cell = new VisualElement { Name = "cell" };
                cell.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 3 };

                if (first == null)
                {
                    first = cell;
                }

                root.AddChild(cell);
            }

            return new IxenSurface(root) { Styles = registry };
        }

        private static long PerPass(string background)
        {
            IxenSurface surface = Build(background, out _);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            long before = System.GC.GetAllocatedBytesForCurrentThread();

            for (int pass = 0; pass < PASSES; pass++)
            {
                surface.Root.Invalidate();
                surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }

            return (System.GC.GetAllocatedBytesForCurrentThread() - before) / PASSES;
        }

        [TestMethod]
        public void RestylingDoesNotRebuildTheBackgroundHandler()
        {
            long each = PerPass("linear-gradient(to bottom #4C6EF5 #1A2B6D)");

            Assert.IsTrue(each < BUDGET,
                $"one style pass over {CELLS} gradient cells allocated {each / 1024} KB. The handler "
                + "used to be built unconditionally at both assignment sites, so every element got a "
                + "fresh Brush - a native SKPaint - and a fresh GradientShader on every pass: 187 KB "
                + "and +0.8 ms here. BackgroundStyleHandler.For caches one handler per descriptor in "
                + "a ConditionalWeakTable, so the 400 cells sharing one class rule share one handler.");
        }

        [TestMethod]
        public void AFilterHandlerIsReusedAsWell()
        {
            var registry = new StyleRegistry();

            var sheet = new XnsSource("cell { filter: blur(3px) }");
            ClassesSet set = sheet.Compile();

            Assert.IsFalse(sheet.HasErrors, string.Join(" | ", sheet.Diagnostics.Select(d => d.Message)));
            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            for (int index = 0; index < CELLS; index++)
            {
                var cell = new VisualElement { Name = "cell" };
                cell.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 3 };
                root.AddChild(cell);
            }

            var surface = new IxenSurface(root) { Styles = registry };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            long before = System.GC.GetAllocatedBytesForCurrentThread();

            for (int pass = 0; pass < PASSES; pass++)
            {
                root.Invalidate();
                surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }

            long each = (System.GC.GetAllocatedBytesForCurrentThread() - before) / PASSES;

            Assert.IsTrue(each < BUDGET,
                $"one style pass over {CELLS} filtered cells allocated {each / 1024} KB; a filter "
                + "handler carries a FilterChain, so rebuilding it per pass meant a native SKPaint "
                + "and SKImageFilter per element per pass");
        }

        [TestMethod]
        public void AFlatColourIsReusedToo()
        {
            long each = PerPass("#4C6EF5");

            Assert.IsTrue(each < BUDGET,
                $"one style pass over {CELLS} flat-colour cells allocated {each / 1024} KB, "
                + "against 109 KB before the cache");
        }

        [TestMethod]
        public void MutatingTheColourInPlaceIsStillObserved()
        {
            IxenSurface surface = Build("#4C6EF5", out VisualElement first);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(new Color("#4C6EF5"), first.StylesHandlers.Background.Color);

            StyleClass rule = surface.Styles.GetGlobalElementClass("cell");
            var background = (BackgroundStyleDescriptor)rule.Styles.Single();

            background.Color = "#E8590C";

            surface.Root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(new Color("#E8590C"), first.StylesHandlers.Background.Color,
                "the cache keeps a handler only while what it derived from is unchanged, which is "
                + "why it validates the colour string rather than trusting the descriptor reference");
        }

        [TestMethod]
        public void MutatingAGradientStopInPlaceStillRepaints()
        {
            var registry = new StyleRegistry();

            var sheet = new XnsSource("panel { background: linear-gradient(to bottom #0000FF #0000FF) }");
            ClassesSet set = sheet.Compile();

            Assert.IsFalse(sheet.HasErrors, string.Join(" | ", sheet.Diagnostics.Select(d => d.Message)));
            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var panel = new VisualElement { Name = "panel" };
            panel.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            root.AddChild(panel);

            var surface = new IxenSurface(root) { Styles = registry };

            surface.ComputeLayout(200, 200);

            using (SkiaSharp.SKBitmap first = surface.RenderToBitmap())
            {
                SkiaSharp.SKColor pixel = first.GetPixel(100, 30);

                Assert.IsTrue(pixel.Blue > 200 && pixel.Red < 60, $"painted blue, got {pixel}");
            }

            StyleClass rule = registry.GetGlobalElementClass("panel");
            var background = (BackgroundStyleDescriptor)rule.Styles.Single();

            background.Gradient.Stops[0].Color = "#FF0000";
            background.Gradient.Stops[1].Color = "#FF0000";

            root.Invalidate();
            surface.ComputeLayout(200, 200);

            using (SkiaSharp.SKBitmap second = surface.RenderToBitmap())
            {
                SkiaSharp.SKColor pixel = second.GetPixel(100, 30);

                Assert.IsTrue(pixel.Red > 200 && pixel.Blue < 60,
                    "a stop mutated in place must rebuild the shader, which is why the cache "
                    + $"compares the gradient's contents and not just its reference; got {pixel}");
            }
        }
    }
}
