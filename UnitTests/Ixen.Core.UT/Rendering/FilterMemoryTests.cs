using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class FilterMemoryTests
    {
        private const int WIDTH = 1282;
        private const int HEIGHT = 753;

        private const int WARMUP = 100;
        private const int MEASURED = 600;

        private const long BUDGET = 30;

        private static IxenSurface Build(bool filtered)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            var card = new VisualElement { Name = "card" };
            card.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 128 };
            card.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 128 };
            card.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            card.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            card.Styles.Background = new BackgroundStyleDescriptor { Color = "#4C6EF5" };

            if (filtered)
            {
                var source = new XnsSource("probe { filter: blur(4px) }");
                ClassesSet set = source.Compile();

                Assert.IsFalse(source.HasErrors);

                card.Styles.Filter = (FilterStyleDescriptor)set.Classes.Single().Styles.Single();
            }

            root.AddChild(card);

            return new IxenSurface(root) { Styles = new StyleRegistry() };
        }

        private static long PrivateMegabytes()
        {
            using (var process = System.Diagnostics.Process.GetCurrentProcess())
            {
                process.Refresh();
                return process.PrivateMemorySize64 / (1024 * 1024);
            }
        }

        private static long GrowthOver(bool filtered)
        {
            IxenSurface surface = Build(filtered);

            var info = new SKImageInfo(WIDTH, HEIGHT, SKColorType.Bgra8888, SKAlphaType.Premul);

            using (SKBitmap target = new SKBitmap(info))
            using (SKCanvas canvas = new SKCanvas(target))
            {
                for (int frame = 0; frame < WARMUP; frame++)
                {
                    surface.ComputeLayout(WIDTH, HEIGHT);
                    surface.Render(canvas);
                }

                long before = PrivateMegabytes();

                for (int frame = 0; frame < MEASURED; frame++)
                {
                    surface.ComputeLayout(WIDTH, HEIGHT);
                    surface.Render(canvas);
                }

                return PrivateMegabytes() - before;
            }
        }

        [TestMethod]
        public void RepaintingAFilteredElementDoesNotGrowTheProcess()
        {
            long growth = GrowthOver(true);

            Assert.IsTrue(growth < BUDGET,
                $"{MEASURED} repaints of one blurred element grew the process by {growth} MB. "
                + "Skia caches an image filter's result per invocation, and the layer is a fresh "
                + "source every frame, so the cache never hits and fills to its own budget - about "
                + "0.14 MB a frame, plateauing near 128 MB. RendererContext.EndFrame purges the "
                + "resource cache after any frame that pushed a filter, which is what keeps this "
                + "flat; SetResourceCacheTotalByteLimit does not govern that cache and does nothing.");
        }

        [TestMethod]
        public void AndNeitherDoesAnUnfilteredOne()
        {
            long growth = GrowthOver(false);

            Assert.IsTrue(growth < BUDGET,
                $"the same scene without a filter grew by {growth} MB, so the guard above is "
                + "measuring the filter rather than the harness");
        }
    }
}
