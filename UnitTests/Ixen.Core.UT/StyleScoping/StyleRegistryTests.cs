using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class StyleRegistryTests
    {
        private static StyleRegistry RegistryWithBoxWidth(float pixels)
        {
            var registry = new StyleRegistry();

            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "box",
                new List<StyleDescriptor>
                {
                    new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = pixels }
                }));

            return registry;
        }

        private static VisualElement BuildTree()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            root.AddChild(new VisualElement { Name = "box" });
            return root;
        }

        [TestMethod]
        public void TwoSurfaces_ResolveTheSameElementDifferentlyFromTheirOwnRegistry()
        {
            VisualElement narrowRoot = BuildTree();
            var narrowSurface = new IxenSurface(narrowRoot) { Styles = RegistryWithBoxWidth(100) };
            narrowSurface.ComputeLayout(400, 400);

            VisualElement wideRoot = BuildTree();
            var wideSurface = new IxenSurface(wideRoot) { Styles = RegistryWithBoxWidth(250) };
            wideSurface.ComputeLayout(400, 400);

            Assert.AreEqual(100, narrowRoot.Children[0].Width, "narrow box width");
            Assert.AreEqual(250, wideRoot.Children[0].Width, "wide box width");
        }

        [TestMethod]
        public void SwappingTheRegistry_RestylesTheSameTreeOnTheNextLayout()
        {
            VisualElement root = BuildTree();
            var surface = new IxenSurface(root) { Styles = RegistryWithBoxWidth(100) };

            surface.ComputeLayout(400, 400);
            Assert.AreEqual(100, root.Children[0].Width, "before swap");

            surface.Styles = RegistryWithBoxWidth(250);
            root.Invalidate();
            surface.ComputeLayout(400, 400);

            Assert.AreEqual(250, root.Children[0].Width, "after swap");
        }

        [TestMethod]
        public void AnEmptyRegistry_LeavesElementsOnTheirOwnStyles()
        {
            VisualElement root = BuildTree();
            root.Children[0].Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 42 };

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            surface.ComputeLayout(400, 400);

            Assert.AreEqual(42, root.Children[0].Width);
        }

        [TestMethod]
        public void DefaultRegistry_AutoDiscoversGeneratedStyleSheets()
        {
            StyleClass generated = StyleRegistry.Default.GetGlobalElementClass("container_row_test1");

            Assert.IsNotNull(generated, "the generated RowLayoutTest1Styles sheet should be registered");
            Assert.AreEqual(StyleClassTarget.ElementName, generated.Target);
        }

        [TestMethod]
        public void TwoSurfaces_ShareTheSameDefaultRegistryInstance()
        {
            var first = new IxenSurface(new VisualElement());
            var second = new IxenSurface(new VisualElement());

            Assert.AreSame(first.Styles, second.Styles);
        }

        [TestMethod]
        public void ANewSurface_UsesTheDefaultRegistry()
        {
            var surface = new IxenSurface(new VisualElement());

            Assert.AreSame(StyleRegistry.Default, surface.Styles);
        }

        [TestMethod]
        public void AddingASheet_RegistersAllItsClasses()
        {
            var sheet = new StyleSheet();
            var registry = new StyleRegistry();

            sheet.Classes.Add(new StyleClass(StyleClassTarget.ClassName, null, null, "first", null));
            sheet.Classes.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "second", null));

            registry.Add(sheet);

            Assert.AreEqual(2, registry.Count);
            Assert.IsNotNull(registry.GetGlobalClass("first"));
            Assert.IsNotNull(registry.GetGlobalElementClass("second"));
        }

        [TestMethod]
        public void Clear_EmptiesTheRegistry()
        {
            StyleRegistry registry = RegistryWithBoxWidth(100);
            Assert.AreEqual(1, registry.Count);

            registry.Clear();

            Assert.AreEqual(0, registry.Count);
            Assert.IsNull(registry.GetGlobalElementClass("box"));
        }
    }
}
