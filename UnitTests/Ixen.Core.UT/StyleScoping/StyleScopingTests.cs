using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class StyleScopingTests
    {
        private const string SHEET_SCOPE = "StyleSheet1";
        private const string SCOPE = "container";

        private static StyleRegistry BuildRegistry()
        {
            var registry = new StyleRegistry();

            registry.Add(new StyleClass(StyleClassTarget.ClassName, null, null, "testGlobalClass", null));
            registry.Add(new StyleClass(StyleClassTarget.ClassName, null, SCOPE, "testScopedGlobalClass", null));
            registry.Add(new StyleClass(StyleClassTarget.ClassName, SHEET_SCOPE, null, "testSheetScopedClass", null));
            registry.Add(new StyleClass(StyleClassTarget.ClassName, SHEET_SCOPE, SCOPE, "testSheetScopedScopedClass", null));

            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "testElementGlobalClass", null));
            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, SCOPE, "testElementScopedGlobalClass", null));
            registry.Add(new StyleClass(StyleClassTarget.ElementName, SHEET_SCOPE, null, "testElementSheetScopedClass", null));
            registry.Add(new StyleClass(StyleClassTarget.ElementName, SHEET_SCOPE, SCOPE, "testElementSheetScopedScopedClass", null));

            registry.Add(new StyleClass(StyleClassTarget.ElementType, null, null, "testTypeGlobalClass", null));
            registry.Add(new StyleClass(StyleClassTarget.ElementType, null, SCOPE, "testTypeScopedGlobalClass", null));
            registry.Add(new StyleClass(StyleClassTarget.ElementType, SHEET_SCOPE, null, "testTypeSheetScopedClass", null));
            registry.Add(new StyleClass(StyleClassTarget.ElementType, SHEET_SCOPE, SCOPE, "testTypeSheetScopedScopedClass", null));

            return registry;
        }

        [TestMethod]
        public void GlobalClass_IsFoundWithoutScope()
        {
            StyleClass styleClass = BuildRegistry().GetGlobalClass("testGlobalClass");

            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testGlobalClass", styleClass.Name);
            Assert.IsNull(styleClass.Scope);
        }

        [TestMethod]
        public void ScopedClass_IsNotFoundWithoutItsScope()
        {
            Assert.IsNull(BuildRegistry().GetGlobalClass("testScopedGlobalClass"));
        }

        [TestMethod]
        public void ScopedClass_IsFoundWithItsScope()
        {
            StyleClass styleClass = BuildRegistry().GetGlobalClass("testScopedGlobalClass", SCOPE);

            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testScopedGlobalClass", styleClass.Name);
            Assert.AreEqual(SCOPE, styleClass.Scope);
        }

        [TestMethod]
        public void SheetScopedClass_IsFoundWithItsSheetScope()
        {
            StyleClass styleClass = BuildRegistry().GetClass("testSheetScopedClass", SHEET_SCOPE);

            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testSheetScopedClass", styleClass.Name);
            Assert.AreEqual(SHEET_SCOPE, styleClass.SheetScope);
            Assert.IsNull(styleClass.Scope);
        }

        [TestMethod]
        public void SheetScopedAndScopedClass_IsFoundWithBoth()
        {
            StyleClass styleClass = BuildRegistry().GetClass("testSheetScopedScopedClass", SHEET_SCOPE, SCOPE);

            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testSheetScopedScopedClass", styleClass.Name);
            Assert.AreEqual(SHEET_SCOPE, styleClass.SheetScope);
            Assert.AreEqual(SCOPE, styleClass.Scope);
        }

        [TestMethod]
        public void ElementNameClasses_FollowTheSameScopingRules()
        {
            StyleRegistry registry = BuildRegistry();

            StyleClass global = registry.GetGlobalElementClass("testElementGlobalClass");
            Assert.IsNotNull(global);
            Assert.IsNull(global.Scope);

            StyleClass scoped = registry.GetGlobalElementClass("testElementScopedGlobalClass", SCOPE);
            Assert.IsNotNull(scoped);
            Assert.AreEqual(SCOPE, scoped.Scope);

            StyleClass sheetScoped = registry.GetElementClass("testElementSheetScopedClass", SHEET_SCOPE);
            Assert.IsNotNull(sheetScoped);
            Assert.AreEqual(SHEET_SCOPE, sheetScoped.SheetScope);
            Assert.IsNull(sheetScoped.Scope);

            StyleClass both = registry.GetElementClass("testElementSheetScopedScopedClass", SHEET_SCOPE, SCOPE);
            Assert.IsNotNull(both);
            Assert.AreEqual(SHEET_SCOPE, both.SheetScope);
            Assert.AreEqual(SCOPE, both.Scope);
        }

        [TestMethod]
        public void ElementTypeClasses_FollowTheSameScopingRules()
        {
            StyleRegistry registry = BuildRegistry();

            StyleClass global = registry.GetGlobalTypeClass("testTypeGlobalClass");
            Assert.IsNotNull(global);
            Assert.IsNull(global.Scope);

            StyleClass scoped = registry.GetGlobalTypeClass("testTypeScopedGlobalClass", SCOPE);
            Assert.IsNotNull(scoped);
            Assert.AreEqual(SCOPE, scoped.Scope);

            StyleClass sheetScoped = registry.GetTypeClass("testTypeSheetScopedClass", SHEET_SCOPE);
            Assert.IsNotNull(sheetScoped);
            Assert.AreEqual(SHEET_SCOPE, sheetScoped.SheetScope);
            Assert.IsNull(sheetScoped.Scope);

            StyleClass both = registry.GetTypeClass("testTypeSheetScopedScopedClass", SHEET_SCOPE, SCOPE);
            Assert.IsNotNull(both);
            Assert.AreEqual(SHEET_SCOPE, both.SheetScope);
            Assert.AreEqual(SCOPE, both.Scope);
        }

        [TestMethod]
        public void TargetsAreIndependent_SameNameDoesNotCollideAcrossTargets()
        {
            var registry = new StyleRegistry();
            var asClass = new StyleClass(StyleClassTarget.ClassName, null, null, "shared", null);
            var asElement = new StyleClass(StyleClassTarget.ElementName, null, null, "shared", null);
            var asType = new StyleClass(StyleClassTarget.ElementType, null, null, "shared", null);

            registry.Add(asClass);
            registry.Add(asElement);
            registry.Add(asType);

            Assert.AreEqual(3, registry.Count);
            Assert.AreSame(asClass, registry.GetGlobalClass("shared"));
            Assert.AreSame(asElement, registry.GetGlobalElementClass("shared"));
            Assert.AreSame(asType, registry.GetGlobalTypeClass("shared"));
        }

        [TestMethod]
        public void TwoRegistries_AreFullyIsolated()
        {
            var first = new StyleRegistry();
            var second = new StyleRegistry();

            first.Add(new StyleClass(StyleClassTarget.ClassName, null, null, "onlyInFirst", null));

            Assert.IsNotNull(first.GetGlobalClass("onlyInFirst"));
            Assert.IsNull(second.GetGlobalClass("onlyInFirst"));
            Assert.AreEqual(0, second.Count);
        }

        [TestMethod]
        public void AddingTheSameKeyTwice_KeepsTheLastOne()
        {
            var registry = new StyleRegistry();
            var first = new StyleClass(StyleClassTarget.ClassName, null, null, "dup", null);
            var second = new StyleClass(StyleClassTarget.ClassName, null, null, "dup", null);

            registry.Add(first);
            registry.Add(second);

            Assert.AreEqual(1, registry.Count);
            Assert.AreSame(second, registry.GetGlobalClass("dup"));
        }
    }
}
