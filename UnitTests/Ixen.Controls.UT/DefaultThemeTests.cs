using Ixen.Core;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.StyleSheets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class DefaultThemeTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private Button _button;
        private StyleRegistry _registry;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _button = new Button { Name = "save", Text = "Save" };

            _root.AddChild(_button);

            _registry = new StyleRegistry();
            _registry.AddDefaults(new DefaultTheme_StyleSheet());

            _surface = new IxenSurface(_root) { Styles = _registry };
        }

        private void Layout()
        {
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private string Background() => _button.StylesHandlers.Background.Descriptor.Color;

        private void App(string source)
        {
            var xns = new XnsSource(source);
            ClassesSet set = xns.Compile();

            Assert.IsFalse(xns.HasErrors, source);

            _registry.Add(set);
        }

        [TestMethod]
        public void AButtonLooksLikeSomethingWithNoStylesheetAtAll()
        {
            Layout();

            Assert.IsNotNull(Background(),
                "an element with no rule is invisible in Ixen - a control library that shipped "
                + "no theme would hand people an empty rectangle");
            Assert.IsTrue(_button.ActualHeight > 0, "and it sizes itself from its padding and text");
        }

        [TestMethod]
        public void TheTypeNameIsWhatMakesTheThemeMatch()
        {
            Assert.AreEqual("Button", _button.TypeName,
                "TypeName is set by XNL or by a component, so a control built from C# has to set "
                + "its own or #Button matches nothing at all");
        }

        [TestMethod]
        public void AnApplicationRuleOfTheSameKindWins()
        {
            App("#Button { background: #FF0000 }");

            Layout();

            Assert.AreEqual("#FF0000", Background(),
                "the default is applied just before the application's rule of the same kind, so "
                + "the application wins whatever order the assemblies happened to load in");
        }

        [TestMethod]
        public void TheThemeLosesEvenWhenItIsRegisteredLast()
        {
            var registry = new StyleRegistry();
            var xns = new XnsSource("#Button { background: #FF0000 }");

            registry.Add(xns.Compile());
            registry.AddDefaults(new DefaultTheme_StyleSheet());

            _surface.Styles = registry;

            Layout();

            Assert.AreEqual("#FF0000", Background(),
                "THIS is the case the layer exists for. CreateFromLoadedAssemblies walks the "
                + "loaded assemblies in whatever order they happen to be in, so a theme can "
                + "perfectly well be registered after the application's own rule - and "
                + "last-wins would then hand the theme the win at random.");
        }
        [TestMethod]
        public void AndSoDoesAClassRule()
        {
            _button.AddClass("primary");

            App(".primary { background: #00FF00 }");

            Layout();

            Assert.AreEqual("#00FF00", Background(),
                "a class already beats a type selector, so this needs no layer of its own");
        }

        [TestMethod]
        public void ChangingOnlyTheBaseKeepsTheThemesHover()
        {
            App("#Button { background: #FF0000 }");

            _button.AddState("hover");
            Layout();

            Assert.AreEqual("#E8EAF0", Background(),
                "the interleaving is per state, not per whole layer: an application that sets a "
                + "colour and says nothing about hover keeps the theme's hover rather than "
                + "silently losing it");
        }

        [TestMethod]
        public void AnApplicationHoverStillWins()
        {
            App("#Button:hover { background: #0000FF }");

            _button.AddState("hover");
            Layout();

            Assert.AreEqual("#0000FF", Background());
        }

        [TestMethod]
        public void TheThemeCoversTheFourStates()
        {
            Layout();
            string idle = Background();

            foreach (string state in new[] { "hover", "pressed", "disabled" })
            {
                _button.AddState(state);
                Layout();

                Assert.AreNotEqual(idle, Background(), $"the theme says something about :{state}");

                _button.RemoveState(state);
            }

            _button.AddState("focus");
            Layout();

            Assert.AreEqual("#4C6EF5", _button.StylesHandlers.Border.Descriptor.Color,
                "focus moves the border rather than the fill, so it reads on any background");
        }

        [TestMethod]
        public void AnEmptyRegistryStillHasNoDefaults()
        {
            var bare = new StyleRegistry();

            Assert.IsFalse(bare.HasDefaultClasses,
                "the whole path is gated, so an application that never loads a control library "
                + "pays one bool test");
        }
    }
}
