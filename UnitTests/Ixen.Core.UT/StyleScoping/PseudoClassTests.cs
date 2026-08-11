using Ixen.Core.Input;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class PseudoClassTests
    {
        private const int VIEWPORT = 200;

        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private static IxenSurface Surface(string xns, VisualElement root)
        {
            var registry = new StyleRegistry();
            registry.Add(Compile(xns));

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
        public void APseudoClassSelectorIsTokenized()
        {
            ClassesSet set = Compile("action:hover {\r\n    background: #222222\r\n}");

            Assert.AreEqual(1, set.Classes.Count);
            Assert.AreEqual("action:hover", set.Classes[0].Name);
            Assert.AreEqual(StyleClassTarget.ElementName, set.Classes[0].Target);
        }

        [TestMethod]
        public void AStateOverridesTheBareRuleOnTheSameName()
        {
            VisualElement box = Element("box");
            IxenSurface surface = Surface(
                "box {\r\n    background: #111111\r\n}\r\nbox:hover {\r\n    background: #222222\r\n}", box);

            Assert.AreEqual("#111111", BackgroundOf(box));

            box.AddState("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(box),
                "this is the whole point: an element-name rule is now overridable");

            box.RemoveState("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#111111", BackgroundOf(box));
        }

        [TestMethod]
        public void StatesWorkOnClassesAndTypesToo()
        {
            VisualElement box = Element("box", "card");
            box.TypeName = "Widget";

            IxenSurface surface = Surface(
                ".card {\r\n    background: #111111\r\n}\r\n.card:hover {\r\n    background: #222222\r\n}\r\n" +
                "#Widget:pressed {\r\n    color: #FF0000\r\n}", box);

            box.AddState("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(box), "class state");

            box.AddState("pressed");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#FF0000", box.StylesHandlers.Color.Descriptor.Value, "type state");
        }

        [TestMethod]
        public void AStateInAScopeReachesDescendants()
        {
            VisualElement card = Element("card");
            VisualElement label = Element("label");
            card.AddChild(label);

            IxenSurface surface = Surface(
                "card {\r\n    label {\r\n        background: #111111\r\n    }\r\n}\r\n" +
                "card:hover {\r\n    label {\r\n        background: #222222\r\n    }\r\n}", card);

            Assert.AreEqual("#111111", BackgroundOf(label));

            card.AddState("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(label),
                "hovering a container must be able to restyle its children");

            card.RemoveState("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#111111", BackgroundOf(label));
        }

        [TestMethod]
        public void AStateSelectorNestedInsideAnotherBlockWorks()
        {
            VisualElement panel = Element("panel");
            VisualElement action = Element("action");
            panel.AddChild(action);

            ClassesSet set = Compile(
                "panel {\r\n" +
                "    action {\r\n        background: #111111\r\n    }\r\n" +
                "    action:hover {\r\n        background: #222222\r\n    }\r\n" +
                "}");

            Assert.AreEqual(2, set.Classes.Count, string.Join(" | ", set.Classes.Select(c => $"{c.Name}@{c.Scope}")));
            Assert.IsNotNull(set.Classes.SingleOrDefault(c => c.Name == "action:hover"),
                "got: " + string.Join(" | ", set.Classes.Select(c => $"{c.Name}@{c.Scope}")));

            var registry = new StyleRegistry();
            registry.Add(set);

            Assert.IsTrue(registry.HasStateClasses, "the registry must notice the state selector");

            var surface = new IxenSurface(panel) { Styles = registry };
            panel.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#111111", BackgroundOf(action));

            action.AddState("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(action), "this is the shape both demos use");
        }

        [TestMethod]
        public void AStateRuleDoesNotApplyWithoutTheState()
        {
            VisualElement box = Element("box");
            Surface("box:hover {\r\n    background: #222222\r\n}", box);

            Assert.IsNull(BackgroundOf(box));
        }

        [TestMethod]
        public void TheHoverStateIsMaintainedByThePointer()
        {
            VisualElement root = Element("root");
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            root.AddChild(box);

            IxenSurface surface = Surface(
                "box {\r\n    background: #111111\r\n}\r\nbox:hover {\r\n    background: #222222\r\n}", root);

            surface.PointerMove(40, 40);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsTrue(box.HasState("hover"));
            Assert.AreEqual("#222222", BackgroundOf(box), "no C# touched a colour");

            surface.PointerMove(150, 150);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(box.HasState("hover"));
            Assert.AreEqual("#111111", BackgroundOf(box));
        }

        [TestMethod]
        public void ThePressedStateIsMaintainedByThePointer()
        {
            VisualElement root = Element("root");
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            root.AddChild(box);

            IxenSurface surface = Surface("box:pressed {\r\n    background: #333333\r\n}", root);

            surface.PointerDown(40, 40, PointerButton.Left);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsTrue(box.HasState("pressed"));
            Assert.AreEqual("#333333", BackgroundOf(box));

            surface.PointerUp(40, 40, PointerButton.Left);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(box.HasState("pressed"));
        }

        [TestMethod]
        public void HoverAppliesToTheWholeAncestorChain()
        {
            VisualElement root = Element("root");
            VisualElement card = Element("card");
            card.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            card.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            VisualElement label = Element("label");
            label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            card.AddChild(label);
            root.AddChild(card);

            IxenSurface surface = Surface("card:hover {\r\n    background: #222222\r\n}", root);

            surface.PointerMove(20, 20);

            Assert.IsTrue(label.HasState("hover"), "the deepest element");
            Assert.IsTrue(card.HasState("hover"), "and every ancestor crossed");
            Assert.IsTrue(root.HasState("hover"));
        }

        [TestMethod]
        public void PressedIsClearedWhenTheCaptureIsLost()
        {
            VisualElement root = Element("root");
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            root.AddChild(box);

            IxenSurface surface = Surface("box:pressed {\r\n    background: #333333\r\n}", root);

            surface.PointerDown(40, 40, PointerButton.Left);
            Assert.IsTrue(box.HasState("pressed"));

            surface.PointerCaptureLost();

            Assert.IsFalse(box.HasState("pressed"), "a stolen capture must not leave a stuck look");
        }

        [TestMethod]
        public void NoStateRuleMeansNoStateTracking()
        {
            VisualElement root = Element("root");
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            root.AddChild(box);

            IxenSurface surface = Surface("box {\r\n    background: #111111\r\n}", root);

            surface.PointerMove(40, 40);

            Assert.IsFalse(box.HasState("hover"),
                "without a single state selector, hovering must not invalidate anything");
            Assert.IsFalse(surface.IsDirty);
        }

        [TestMethod]
        public void TheStateHelpersAreIdempotent()
        {
            VisualElement box = Element("box");

            box.AddState("a");
            box.AddState("a");

            Assert.AreEqual(1, box.States.Count);
            Assert.IsTrue(box.HasState("a"));

            box.RemoveState("nope");
            box.RemoveState(null);
            box.AddState(null);

            Assert.AreEqual(1, box.States.Count);

            box.ToggleState("a", false);
            Assert.IsFalse(box.HasState("a"));
        }

        [TestMethod]
        public void StatesAndClassesAreSeparateAxes()
        {
            VisualElement box = Element("box", "hover");
            IxenSurface surface = Surface(
                ".hover {\r\n    background: #111111\r\n}\r\nbox:hover {\r\n    background: #222222\r\n}", box);

            Assert.AreEqual("#111111", BackgroundOf(box),
                "the class named hover matched; the state rule did not");

            box.AddState("hover");
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", BackgroundOf(box), "now the state rule applies as well and wins");
        }
    }
}
