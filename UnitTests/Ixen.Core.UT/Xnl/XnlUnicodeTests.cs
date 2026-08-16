using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.StyleSheets;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlUnicodeTests
    {
        private const string EXPECTED = "café ● 日本";

        [TestMethod]
        public void AUtf8XnlKeepsItsNonAsciiValue()
        {
            VisualElement view = new Component<UnicodeView>().View;
            VisualElement label = view.FindByName("label");

            Assert.IsNotNull(label);
            Assert.AreEqual(EXPECTED, label.Text);
        }

        [TestMethod]
        public void AUtf8XnsKeepsItsNonAsciiValue()
        {
            var registry = new StyleRegistry();
            registry.Add(new UnicodeStyles_StyleSheet());

            VisualElement view = new Component<UnicodeView>().View;

            var surface = new IxenSurface(view)
            {
                Styles = registry
            };

            surface.ComputeLayout(400, 400);

            VisualElement label = view.FindByName("label");

            Assert.AreEqual("Ségoe", label.StylesHandlers.FontFamily.Descriptor.Value);
            Assert.AreEqual(120, label.Width, "the rest of the block still compiled");
        }

        [TestMethod]
        public void AnXnsValueTakesUnicodeLettersButNotSymbols()
        {
            Assert.IsFalse(HasErrors("el { font-family: Ségoe 日本 }"),
                "letters are letters whatever the script");

            Assert.IsTrue(HasErrors("el { font-family: ● }"),
                "a style value accepts letters, digits and a few sigils - a symbol is not one");
        }

        private static bool HasErrors(string content)
        {
            var source = new Core.Language.Xns.XnsSource(content);
            source.Compile();

            return source.HasErrors;
        }
    }
}
