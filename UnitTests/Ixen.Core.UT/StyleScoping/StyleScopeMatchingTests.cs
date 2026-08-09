using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class StyleScopeMatchingTests
    {
        private const float TARGET_WIDTH = 123;

        private static StyleRegistry Registry(string xns)
        {
            var xnsSource = new XnsSource(xns);
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            return registry;
        }

        private static VisualElement Element(string name, string typeName = null, string cssClass = null)
        {
            var element = new VisualElement { Name = name, TypeName = typeName };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            if (cssClass != null)
            {
                element.Classes.Add(cssClass);
            }

            return element;
        }

        private static VisualElement Chain(params VisualElement[] fromRoot)
        {
            for (int i = 0; i < fromRoot.Length - 1; i++)
            {
                fromRoot[i].AddChild(fromRoot[i + 1]);
            }

            return fromRoot[fromRoot.Length - 1];
        }

        private static void Layout(StyleRegistry registry, VisualElement root)
            => new IxenSurface(root) { Styles = registry }.ComputeLayout(400, 400);

        private static bool Applied(VisualElement element)
            => element.Width == TARGET_WIDTH;

        [TestMethod]
        public void ADirectParentScopeStillMatches()
        {
            StyleRegistry registry = Registry("panel {\r\n    target { width: 123px }\r\n}");

            var root = Element("root");
            var panel = Element("panel");
            VisualElement target = Chain(root, panel, Element("target"));

            Layout(registry, root);

            Assert.IsTrue(Applied(target));
        }

        [TestMethod]
        public void AnIntermediateElementDoesNotBreakTheMatch()
        {
            StyleRegistry registry = Registry("panel {\r\n    target { width: 123px }\r\n}");

            var root = Element("root");
            VisualElement target = Chain(root, Element("panel"), Element("filler"), Element("target"));

            Layout(registry, root);

            Assert.IsTrue(Applied(target), "descendant matching should skip 'filler'");
        }

        [TestMethod]
        public void SegmentsMustAppearInOrder()
        {
            StyleRegistry registry = Registry("outer {\r\n    inner {\r\n        target { width: 123px }\r\n    }\r\n}");

            var root = Element("root");
            VisualElement reversed = Chain(root, Element("inner"), Element("outer"), Element("target"));

            Layout(registry, root);

            Assert.IsFalse(Applied(reversed), "'inner' before 'outer' must not match");
        }

        [TestMethod]
        public void AMissingAncestorDoesNotMatch()
        {
            StyleRegistry registry = Registry("panel {\r\n    target { width: 123px }\r\n}");

            var root = Element("root");
            VisualElement target = Chain(root, Element("sidebar"), Element("target"));

            Layout(registry, root);

            Assert.IsFalse(Applied(target));
        }

        [TestMethod]
        public void AClassSelectorInTheScopeMatchesAnAncestorClass()
        {
            StyleRegistry registry = Registry("container {\r\n    .card {\r\n        target { width: 123px }\r\n    }\r\n}");

            var root = Element("root");
            VisualElement target = Chain(root, Element("container"), Element("box", cssClass: "card"), Element("target"));

            Layout(registry, root);

            Assert.IsTrue(Applied(target), "a '.card' scope segment should match an ancestor carrying that class");
        }

        [TestMethod]
        public void ATypeSelectorInTheScopeMatchesAnAncestorTypeName()
        {
            StyleRegistry registry = Registry("container {\r\n    #Entries {\r\n        target { width: 123px }\r\n    }\r\n}");

            var root = Element("root");
            VisualElement target = Chain(root, Element("container"), Element("list", typeName: "Entries"), Element("target"));

            Layout(registry, root);

            Assert.IsTrue(Applied(target), "an '#Entries' scope segment should match an ancestor TypeName");
        }

        [TestMethod]
        public void AClassSelectorDoesNotMatchAnElementName()
        {
            StyleRegistry registry = Registry("container {\r\n    .card {\r\n        target { width: 123px }\r\n    }\r\n}");

            var root = Element("root");
            VisualElement target = Chain(root, Element("container"), Element("card"), Element("target"));

            Layout(registry, root);

            Assert.IsFalse(Applied(target), "'.card' must not match an element merely named 'card'");
        }

        [TestMethod]
        public void AScopeDoesNotNeedToStartAtTheRoot()
        {
            StyleRegistry registry = Registry("panel {\r\n    target { width: 123px }\r\n}");

            var root = Element("root");
            VisualElement target = Chain(root, Element("a"), Element("b"), Element("panel"), Element("target"));

            Layout(registry, root);

            Assert.IsTrue(Applied(target));
        }

        [TestMethod]
        public void AMoreSpecificScopeWinsOverALessSpecificOne()
        {
            StyleRegistry registry = Registry(@"outer {
    target {
        width: 10px
    }

    inner {
        target {
            width: 123px
        }
    }
}");

            var root = Element("root");
            VisualElement target = Chain(root, Element("outer"), Element("inner"), Element("target"));

            Layout(registry, root);

            Assert.IsTrue(Applied(target), "'outer/inner' should win over 'outer'");
        }

        [TestMethod]
        public void AnUnscopedClassStillAppliesEverywhere()
        {
            StyleRegistry registry = Registry("target { width: 123px }");

            var root = Element("root");
            VisualElement target = Chain(root, Element("anything"), Element("target"));

            Layout(registry, root);

            Assert.IsTrue(Applied(target));
        }
    }
}
