using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleScoping
{
    public class StyleScopingTests : StyleSheet
    {
        public TestStyleSheetScoping() {
            AddClass(new StyleClass(StyleClassTarget.ClassName, null, null, "testGlobalClass", null));
            AddClass(new StyleClass(StyleClassTarget.ClassName, null, "container", "testScopedGlobalClass", null));
            AddClass(new StyleClass(StyleClassTarget.ClassName, "StyleSheet1", null, "testSheetScopedClass", null));
            AddClass(new StyleClass(StyleClassTarget.ClassName, "StyleSheet1", "container", "testSheetScopedScopedClass", null));

            AddClass(new StyleClass(StyleClassTarget.ElementName, null, null, "testElementGlobalClass", null));
            AddClass(new StyleClass(StyleClassTarget.ElementName, null, "container", "testElementScopedGlobalClass", null));
            AddClass(new StyleClass(StyleClassTarget.ElementName, "StyleSheet1", null, "testElementSheetScopedClass", null));
            AddClass(new StyleClass(StyleClassTarget.ElementName, "StyleSheet1", "container", "testElementSheetScopedScopedClass", null));

            AddClass(new StyleClass(StyleClassTarget.ElementType, null, null, "testTypeGlobalClass", null));
            AddClass(new StyleClass(StyleClassTarget.ElementType, null, "container", "testTypeScopedGlobalClass", null));
            AddClass(new StyleClass(StyleClassTarget.ElementType, "StyleSheet1", null, "testTypeSheetScopedClass", null));
            AddClass(new StyleClass(StyleClassTarget.ElementType, "StyleSheet1", "container", "testTypeSheetScopedScopedClass", null));
        }
    }

    [TestClass]
    public class TestStyleSheetScoping
    {
        [TestMethod]
        public void TestScopes()
        {
            StyleClass styleClass;

            styleClass = StyleSheet.GetGlobalClass("testGlobalClass");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testGlobalClass");
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetGlobalClass("testScopedGlobalClass");
            Assert.IsNull(styleClass);

            styleClass = StyleSheet.GetGlobalClass("testScopedGlobalClass", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testScopedGlobalClass");
            Assert.AreEqual(styleClass.Scope, "container");

            styleClass = StyleSheet.GetClass("testSheetScopedClass", "StyleSheet1");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testSheetScopedClass");
            Assert.AreEqual(styleClass.SheetScope, "StyleSheet1");
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetClass("testSheetScopedScopedClass", "StyleSheet1", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testSheetScopedScopedClass");
            Assert.AreEqual(styleClass.SheetScope, "StyleSheet1");
            Assert.AreEqual(styleClass.Scope, "container");

            styleClass = StyleSheet.GetGlobalElementClass("testElementGlobalClass");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testElementGlobalClass");
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetGlobalElementClass("testElementScopedGlobalClass", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testElementScopedGlobalClass");
            Assert.AreEqual(styleClass.Scope, "container");

            styleClass = StyleSheet.GetElementClass("testElementSheetScopedClass", "StyleSheet1");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testElementSheetScopedClass");
            Assert.AreEqual(styleClass.SheetScope, "StyleSheet1");
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetElementClass("testElementSheetScopedScopedClass", "StyleSheet1", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testElementSheetScopedScopedClass");
            Assert.AreEqual(styleClass.SheetScope, "StyleSheet1");
            Assert.AreEqual(styleClass.Scope, "container");

            styleClass = StyleSheet.GetGlobalTypeClass("testTypeGlobalClass");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testTypeGlobalClass");
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetGlobalTypeClass("testTypeScopedGlobalClass", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testTypeScopedGlobalClass");
            Assert.AreEqual(styleClass.Scope, "container");

            styleClass = StyleSheet.GetTypeClass("testTypeSheetScopedClass", "StyleSheet1");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testTypeSheetScopedClass");
            Assert.AreEqual(styleClass.SheetScope, "StyleSheet1");
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetTypeClass("testTypeSheetScopedScopedClass", "StyleSheet1", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual(styleClass.Name, "testTypeSheetScopedScopedClass");
            Assert.AreEqual(styleClass.SheetScope, "StyleSheet1");
            Assert.AreEqual(styleClass.Scope, "container");
        }
    }
}
