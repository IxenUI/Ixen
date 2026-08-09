using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleScoping
{
    public class StyleScopingTests : StyleSheet
    {
        public StyleScopingTests()
        {
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
            Assert.AreEqual("testGlobalClass", styleClass.Name);
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetGlobalClass("testScopedGlobalClass");
            Assert.IsNull(styleClass);

            styleClass = StyleSheet.GetGlobalClass("testScopedGlobalClass", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testScopedGlobalClass", styleClass.Name);
            Assert.AreEqual("container", styleClass.Scope);

            styleClass = StyleSheet.GetClass("testSheetScopedClass", "StyleSheet1");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testSheetScopedClass", styleClass.Name);
            Assert.AreEqual("StyleSheet1", styleClass.SheetScope);
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetClass("testSheetScopedScopedClass", "StyleSheet1", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testSheetScopedScopedClass", styleClass.Name);
            Assert.AreEqual("StyleSheet1", styleClass.SheetScope);
            Assert.AreEqual("container", styleClass.Scope);

            styleClass = StyleSheet.GetGlobalElementClass("testElementGlobalClass");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testElementGlobalClass", styleClass.Name);
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetGlobalElementClass("testElementScopedGlobalClass", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testElementScopedGlobalClass", styleClass.Name);
            Assert.AreEqual("container", styleClass.Scope);

            styleClass = StyleSheet.GetElementClass("testElementSheetScopedClass", "StyleSheet1");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testElementSheetScopedClass", styleClass.Name);
            Assert.AreEqual("StyleSheet1", styleClass.SheetScope);
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetElementClass("testElementSheetScopedScopedClass", "StyleSheet1", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testElementSheetScopedScopedClass", styleClass.Name);
            Assert.AreEqual("StyleSheet1", styleClass.SheetScope);
            Assert.AreEqual("container", styleClass.Scope);

            styleClass = StyleSheet.GetGlobalTypeClass("testTypeGlobalClass");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testTypeGlobalClass", styleClass.Name);
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetGlobalTypeClass("testTypeScopedGlobalClass", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testTypeScopedGlobalClass", styleClass.Name);
            Assert.AreEqual("container", styleClass.Scope);

            styleClass = StyleSheet.GetTypeClass("testTypeSheetScopedClass", "StyleSheet1");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testTypeSheetScopedClass", styleClass.Name);
            Assert.AreEqual("StyleSheet1", styleClass.SheetScope);
            Assert.IsNull(styleClass.Scope);

            styleClass = StyleSheet.GetTypeClass("testTypeSheetScopedScopedClass", "StyleSheet1", "container");
            Assert.IsNotNull(styleClass);
            Assert.AreEqual("testTypeSheetScopedScopedClass", styleClass.Name);
            Assert.AreEqual("StyleSheet1", styleClass.SheetScope);
            Assert.AreEqual("container", styleClass.Scope);
        }
    }
}
