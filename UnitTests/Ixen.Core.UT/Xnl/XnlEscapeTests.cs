using Ixen.Core.UT.Xnl.Fixtures;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlEscapeTests
    {
        private static string TextOf(VisualElement view, string name)
        {
            VisualElement element = view.FindByName(name);

            Assert.IsNotNull(element, name);
            return element.Text;
        }

        private static VisualElement Bound()
        {
            var component = new EscapesComponent();
            var surface = new IxenSurface(component) { Styles = new StyleRegistry() };

            surface.ComputeLayout(400, 400);
            return component.View;
        }

        [TestMethod]
        public void ALiteralValueReachesTheElementDecoded()
        {
            VisualElement view = new EscapesView();

            Assert.AreEqual("Line one\nLine two", TextOf(view, "escapes_lines"));
            Assert.AreEqual("a\tb", TextOf(view, "escapes_tab"));
            Assert.AreEqual("Say \"hi\"", TextOf(view, "escapes_quote"));
            Assert.AreEqual("a\\b", TextOf(view, "escapes_backslash"));
            Assert.AreEqual("C:\\Users", TextOf(view, "escapes_unknown"), "a backslash before anything else is a backslash");
        }

        [TestMethod]
        public void AnEscapedQuoteConvertsToACharacter()
        {
            var widget = (XnlTestWidget)new EscapesView().FindByName("escapes_char");

            Assert.AreEqual('"', widget.Initial);
        }

        [TestMethod]
        public void TheLiteralPartsOfABindingAreDecodedToo()
        {
            Assert.AreEqual("Total:\n3 \"done\"", TextOf(Bound(), "escapes_mixed"));
        }

        [TestMethod]
        public void ABindingCanPassAString()
        {
            Assert.AreEqual("[x]", TextOf(Bound(), "escapes_expression"));
        }

        [TestMethod]
        public void ADecodedLineBreakIsAHardBreak()
        {
            VisualElement view = Bound();
            VisualElement label = view.FindByName("escapes_lines");

            Assert.AreEqual(2, label.TextLines.Count);
            Assert.AreEqual("Line two", label.TextLines[1]);
        }
    }
}
