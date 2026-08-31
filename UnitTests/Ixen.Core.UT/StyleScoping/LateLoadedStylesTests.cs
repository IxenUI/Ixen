using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class LateLoadedStylesTests
    {
        [TestMethod]
        public void ConstructingASurfaceDoesNotFreezeTheDefaultRegistry()
        {
            StyleRegistry.ResetDefault();

            var surface = new IxenSurface();

            Assert.IsFalse(StyleRegistry.DefaultIsCreated,
                "the registry is built by scanning the assemblies that are loaded at the time, so "
                + "building it when the surface is constructed misses any control library the "
                + "application has not touched yet. Android hit exactly that: IxenView creates the "
                + "surface in Init and the activity assigns RootComponent afterwards, so the whole "
                + "default theme was silently absent while Win32 was fine.");

            surface.Root = new VisualElement { Name = "root" };
            surface.ComputeLayout(100, 100);

            Assert.IsTrue(StyleRegistry.DefaultIsCreated,
                "the first layout is what needs it, and by then the tree exists");
        }

        [TestMethod]
        public void ASheetRegisteredAfterTheSurfaceIsStillSeen()
        {
            StyleRegistry.ResetDefault();

            var surface = new IxenSurface();

            var root = new VisualElement { Name = "root" };
            var box = new VisualElement { Name = "late_box" };

            root.AddChild(box);
            surface.Root = root;

            StyleRegistry.Default.Add(new StyleClass(StyleClassTarget.ElementName, null, null,
                "late_box", new System.Collections.Generic.List<StyleDescriptor>
                {
                    new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 37 }
                }));

            surface.ComputeLayout(200, 200);

            Assert.AreEqual(37f, box.ActualWidth,
                "which is the shape of the Android bug: the styles arrive between the surface being "
                + "built and the first frame");
        }

        [TestMethod]
        public void AnExplicitRegistryStillWins()
        {
            var registry = new StyleRegistry();

            var surface = new IxenSurface { Styles = registry };

            Assert.AreSame(registry, surface.Styles,
                "the property is still settable, and a host or a test that sets one must not be "
                + "handed the default instead");
        }
    }
}
