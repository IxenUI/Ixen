using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlClassesTests
    {
        private static VisualElement Element(string name)
            => new Component<ClassesView>().View.FindByName(name);

        [TestMethod]
        public void ASingleClassStillLandsAlone()
        {
            VisualElement element = Element("one");

            Assert.AreEqual(1, element.Classes.Count);
            Assert.AreEqual("alpha", element.Classes[0]);
        }

        [TestMethod]
        public void AWhitespaceSeparatedValueBecomesSeveralClasses()
        {
            VisualElement element = Element("two");

            Assert.AreEqual(2, element.Classes.Count,
                "the whole value used to land as one class literally named 'alpha beta'");
            Assert.AreEqual("alpha", element.Classes[0]);
            Assert.AreEqual("beta", element.Classes[1]);
        }

        [TestMethod]
        public void TheDeclaredOrderIsKept()
        {
            Assert.AreEqual("beta", Element("two").Classes[1],
                "the last class in the list wins the cascade, so the order is load-bearing");
        }

        [TestMethod]
        public void RepeatedAndSurroundingWhitespaceProducesNoEmptyClass()
        {
            VisualElement element = Element("spaced");

            Assert.AreEqual(3, element.Classes.Count);
            CollectionAssert.AreEqual(new[] { "alpha", "beta", "gamma" }, element.Classes);
        }

        [TestMethod]
        public void AnEmptyValueAddsNothing()
        {
            Assert.AreEqual(0, Element("empty").Classes.Count);
        }

        [TestMethod]
        public void NoClassPropertyAddsNothing()
        {
            Assert.AreEqual(0, Element("none").Classes.Count);
        }
    }
}
