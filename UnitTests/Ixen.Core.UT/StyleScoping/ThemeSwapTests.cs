using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class ThemeSwapTests
    {
        private static StyleRegistry Sheet(string content)
        {
            var source = new XnsSource(content);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            return registry;
        }

        private static (IxenSurface, VisualElement) Tree()
        {
            var root = new VisualElement { Name = "root" };
            var panel = new VisualElement { Name = "panel" };

            root.AddChild(panel);

            return (new IxenSurface(root), panel);
        }

        private static SKColor Fill(IxenSurface surface, int width, int height)
        {
            surface.ComputeLayout(width, height);

            using SKBitmap bitmap = surface.RenderToBitmap();

            return bitmap.GetPixel(width / 2, height / 2);
        }

        [TestMethod]
        public void LoadedDefaultsTakeOnlyTheAssembliesThatDeclareThemselves()
        {
            var registry = new StyleRegistry();

            registry.AddLoadedDefaults();

            Assert.IsNull(
                registry.GetGlobal(StyleClassTarget.ElementName, "generated_root"),
                "no assembly here declares [IxenDefaultStyles], so a registry built this way is "
                + "empty - it must not sweep up an ordinary application sheet");

            Assert.IsNotNull(
                StyleRegistry.CreateFromLoadedAssemblies()
                    .GetGlobal(StyleClassTarget.ElementName, "generated_root"),
                "whereas the full walk does find it, which is what makes the first assertion a "
                + "statement about the filter rather than about the fixture");
        }

        [TestMethod]
        public void SwappingTheRegistryRestylesWithNoInvalidateFromTheCaller()
        {
            (IxenSurface surface, _) = Tree();

            surface.Styles = Sheet("panel { width: 1*  height: 1*  background: #FF0000 }");

            Assert.IsTrue(Fill(surface, 40, 40).Red > 200);

            surface.Styles = Sheet("panel { width: 1*  height: 1*  background: #0000FF }");

            SKColor pixel = Fill(surface, 40, 40);

            Assert.IsTrue(pixel.Blue > 200 && pixel.Red < 60,
                $"the setter has to invalidate, or StyleComputer skips every element whose "
                + $"MustRefreshStyles is already false; got {pixel}");
        }

        [TestMethod]
        public void SwappingToTheSameRegistryCostsNothing()
        {
            (IxenSurface surface, _) = Tree();

            StyleRegistry styles = Sheet("panel { width: 1*  height: 1*  background: #FF0000 }");

            surface.Styles = styles;
            Fill(surface, 40, 40);

            surface.Styles = styles;

            Assert.IsFalse(surface.IsDirty,
                "a host re-reads its theme whenever something says it changed, and it is usually "
                + "the same one");
        }

        [TestMethod]
        public void AResizeAfterASwapStillCrossesTheNewBreakpoints()
        {
            (IxenSurface surface, _) = Tree();

            surface.Styles = Sheet("panel { width: 1*  height: 1*  background: #FF0000 }\r\n"
                + "@media (max-width: 400px) { panel { background: #00FF00 } }");

            Assert.IsTrue(Fill(surface, 300, 40).Green > 200);

            surface.Styles = Sheet("panel { width: 1*  height: 1*  background: #FF0000 }\r\n"
                + "@media (max-width: 600px) { panel { background: #0000FF } }\r\n"
                + "@media (max-width: 200px) { panel { background: #FFFFFF } }");

            Assert.IsTrue(Fill(surface, 300, 40).Blue > 200);

            SKColor pixel = Fill(surface, 700, 40);

            Assert.IsTrue(pixel.Red > 200 && pixel.Blue < 60,
                "the stored signature is a bitmask over the previous registry's own query list, so "
                + $"it means nothing once the registry changed; got {pixel}");
        }
    }
}
