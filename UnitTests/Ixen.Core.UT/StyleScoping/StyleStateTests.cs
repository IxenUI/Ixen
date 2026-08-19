using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class StyleStateTests
    {
        private const int VIEWPORT = 200;

        private static IxenSurface Surface(string xns, VisualElement root)
        {
            var xnsSource = new XnsSource(xns);
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            var surface = new IxenSurface(root) { Styles = registry };
            root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static VisualElement Element(string name, params string[] classes)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            foreach (string c in classes)
            {
                element.Classes.Add(c);
            }

            return element;
        }

        private static string BackgroundOf(VisualElement element)
            => element.StylesHandlers.Background.Descriptor?.Color;

        [TestMethod]
        public void AddingAClassChangesTheResolvedStyle()
        {
            VisualElement box = Element("box", "base");
            IxenSurface surface = Surface(
                ".base {\r\n    background: #111111\r\n}\r\n.hover {\r\n    background: #222222\r\n}", box);

            Assert.AreEqual("#111111", BackgroundOf(box));

            box.AddClass("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(box), "AddClass must trigger a restyle on its own");
        }

        [TestMethod]
        public void RemovingAClassRestoresTheBaseStyle()
        {
            VisualElement box = Element("box", "base");
            IxenSurface surface = Surface(
                ".base {\r\n    background: #111111\r\n}\r\n.hover {\r\n    background: #222222\r\n}", box);

            box.AddClass("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            box.RemoveClass("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#111111", BackgroundOf(box),
                "the handler container is reused, so the base pass must reset every property");
        }

        [TestMethod]
        public void TheLastClassInTheListWins()
        {
            VisualElement box = Element("box", "base");
            IxenSurface surface = Surface(
                ".base {\r\n    background: #111111\r\n}\r\n" +
                ".hover {\r\n    background: #222222\r\n}\r\n" +
                ".pressed {\r\n    background: #333333\r\n}", box);

            box.AddClass("hover");
            box.AddClass("pressed");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#333333", BackgroundOf(box), "classes apply in list order");

            box.RemoveClass("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#333333", BackgroundOf(box), "removing an earlier class leaves the later one");
        }

        [TestMethod]
        public void AnElementNameStillBeatsAClass()
        {
            VisualElement box = Element("box", "hover");
            Surface("box {\r\n    background: #111111\r\n}\r\n.hover {\r\n    background: #222222\r\n}", box);

            Assert.AreEqual("#111111", BackgroundOf(box),
                "element name outranks class, so a state class cannot override a name rule");
        }

        [TestMethod]
        public void AClassChangeAlsoReachesScopedDescendants()
        {
            VisualElement card = Element("card", "base");
            VisualElement label = Element("label");
            card.AddChild(label);

            IxenSurface surface = Surface(
                ".base {\r\n    label {\r\n        background: #111111\r\n    }\r\n}\r\n" +
                ".hover {\r\n    label {\r\n        background: #222222\r\n    }\r\n}", card);

            Assert.AreEqual("#111111", BackgroundOf(label));

            card.AddClass("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(label),
                "invalidating a subtree is what makes a scoped state rule work");
        }

        [TestMethod]
        public void AClassCanChangeTheLayout()
        {
            VisualElement host = Element("host");
            VisualElement box = Element("box", "base");
            host.AddChild(box);

            IxenSurface surface = Surface(
                ".base {\r\n    width: 40px\r\n}\r\n.wide {\r\n    width: 120px\r\n}", host);

            Assert.AreEqual(40f, box.Width, "the styled element must be nested: the root is forced to the viewport");

            box.AddClass("wide");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(120f, box.Width, "AddClass invalidates the layout too, not just the styles");
        }

        [TestMethod]
        public void TheHelpersAreIdempotentAndSafe()
        {
            VisualElement box = Element("box");

            Assert.IsFalse(box.HasClass("a"));

            box.AddClass("a");
            box.AddClass("a");

            Assert.AreEqual(1, box.Classes.Count, "adding twice must not duplicate");
            Assert.IsTrue(box.HasClass("a"));

            box.RemoveClass("nope");
            box.RemoveClass(null);
            box.AddClass(null);

            Assert.AreEqual(1, box.Classes.Count);

            box.ToggleClass("a", false);
            Assert.IsFalse(box.HasClass("a"));

            box.ToggleClass("a", true);
            Assert.IsTrue(box.HasClass("a"));
        }

        [TestMethod]
        public void MutatingTheListDirectlyDoesNotRestyle()
        {
            VisualElement box = Element("box", "base");
            IxenSurface surface = Surface(
                ".base {\r\n    background: #111111\r\n}\r\n.hover {\r\n    background: #222222\r\n}", box);

            box.Classes.Add("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#111111", BackgroundOf(box),
                "this is why AddClass exists: a raw list mutation notifies nothing");
        }

        [TestMethod]
        public void ALaterClassBeatsAnEarlierClassesStateVariant()
        {
            VisualElement box = Element("box", "text", "on");
            IxenSurface surface = Surface(
                ".text {\r\n    background: #111111\r\n}\r\n"
                + ".text:hover {\r\n    background: #222222\r\n}\r\n"
                + ".on {\r\n    background: #333333\r\n}", box);

            box.AddState("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#333333", BackgroundOf(box),
                "classes are applied one at a time with their own states, so the second class"
                + " lands after the first one's hover variant");
        }

        [TestMethod]
        public void AnEarlierClassesStateStillWinsOverItsOwnBase()
        {
            VisualElement box = Element("box", "text", "on");
            IxenSurface surface = Surface(
                ".text {\r\n    background: #111111\r\n}\r\n"
                + ".text:hover {\r\n    background: #222222\r\n}\r\n"
                + ".on {\r\n    color: #FFFFFF\r\n}", box);

            box.AddState("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(box),
                "the later class only wins the properties it actually declares");
        }
    }
}
