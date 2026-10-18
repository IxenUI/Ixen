using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.Visual;
using Ixen.Fixtures.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class ReferencedAssemblyTests
    {
        private static VisualElement Host()
        {
            return new LibraryHostComponent().Initialize();
        }

        [TestMethod]
        public void AComponentDeclaredInAReferencedAssemblyIsInjected()
        {
            VisualElement caption = Host().FindByName("card_caption");

            Assert.IsNotNull(caption, "the card's view was never built");
            Assert.AreEqual("from a referenced assembly", caption.Text);
        }

        [TestMethod]
        public void AnElementDeclaredInAReferencedAssemblyIsInstantiated()
        {
            VisualElement badge = Host().FindByName("badge");

            Assert.IsInstanceOfType(badge, typeof(LibraryBadge));
            Assert.AreEqual("LibraryBadge", badge.TypeName);
        }
    }
}
