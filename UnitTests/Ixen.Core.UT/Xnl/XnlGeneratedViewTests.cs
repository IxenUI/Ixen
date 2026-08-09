using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlGeneratedViewTests
    {
        private static VisualElement Root()
            => new Component<TypedView>().View;

        [TestMethod]
        public void TheGeneratedViewCarriesTheDeclaredTree()
        {
            VisualElement view = Root();

            Assert.AreEqual(1, view.Children.Count, "the view wraps the declared root element");
            Assert.AreEqual("root", view.Children[0].Name);
            Assert.AreEqual(2, view.Children[0].Children.Count);
        }

        [TestMethod]
        public void AnElementTypeBecomesTheTypeName()
        {
            VisualElement title = Root().Children[0].Children[0];

            Assert.AreEqual("title", title.Name);
            Assert.AreEqual("VisualElement", title.TypeName, "the XNL element type should reach TypeName for #type styling");
        }

        [TestMethod]
        public void AnUntypedElementHasNoTypeName()
        {
            VisualElement plain = Root().Children[0].Children[1];

            Assert.AreEqual("plain", plain.Name);
            Assert.IsNull(plain.TypeName);
        }

        [TestMethod]
        public void APropertyIsAssignedToTheMatchingClrProperty()
        {
            VisualElement title = Root().Children[0].Children[0];

            Assert.AreEqual("Coucou", title.Text);
            Assert.AreEqual("the-title", title.Id);
        }

        [TestMethod]
        public void TheClassPropertyStillGoesToTheClassList()
        {
            VisualElement title = Root().Children[0].Children[0];

            Assert.AreEqual(1, title.Classes.Count);
            Assert.AreEqual("big", title.Classes[0]);
        }
    }
}
