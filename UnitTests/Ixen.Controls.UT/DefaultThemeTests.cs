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

        private T Add<T>(string name) where T : VisualElement, new()
        {
            var control = new T { Name = name };

            _root.AddChild(control);

            return control;
        }

        [TestMethod]
        public void ASwitchMovesItsKnobWhenItIsChecked()
        {
            Switch toggle = Add<Switch>("notify");

            Layout();
            float off = toggle.Mark.X;

            toggle.Checked = true;
            Layout();

            Assert.IsTrue(toggle.Mark.X > off,
                "the knob slides because #Switch:checked flips content-align. A SECOND "
                + "#Switch:checked block for the background replaced that one outright - the "
                + "registry is keyed on the selector and last-wins takes the whole list, so "
                + "two rules for one selector do NOT merge.");

            Assert.AreEqual("#4C6EF5", toggle.StylesHandlers.Background.Descriptor.Color,
                "and the block that replaced it still does its own job");
        }

        [TestMethod]
        public void AToggleShowsAndHidesItsMark()
        {
            CheckBox box = Add<CheckBox>("agree");

            Layout();

            Assert.IsTrue(box.Mark.StylesHandlers.Visibility.Descriptor.IsDeclared,
                "hidden until checked - and this rule is written UNSCOPED, because the defaults "
                + "layer drops a rule nested inside another and says nothing about it");

            box.Checked = true;
            Layout();

            Assert.IsFalse(box.Mark.StylesHandlers.Visibility.Descriptor.IsDeclared,
                "#CheckBoxMark:checked makes it visible, which is why the control marks its own "
                + "part rather than relying on its parent's state");
        }

        [TestMethod]
        public void AMarkIsSizedByTheThemeRatherThanFillingItsBox()
        {
            RadioButton radio = Add<RadioButton>("small");
            radio.Checked = true;

            Layout();

            Assert.AreEqual(8f, radio.Mark.ActualWidth,
                "a part with no rule of its own is Unset, which means FILL - the radio dot "
                + "swelled to the whole button when its nested rule was being dropped");
            Assert.AreEqual(8f, radio.Mark.ActualHeight);
        }

        [TestMethod]
        public void ASliderKeepsItsThumbInsideAtTheMaximum()
        {
            Slider slider = Add<Slider>("volume");
            slider.Value = slider.Maximum;

            Layout();

            VisualElement thumb = null;

            foreach (VisualElement part in slider.ChildElements)
            {
                if (part.TypeName == Slider.THUMB)
                {
                    thumb = part;
                }
            }

            Assert.IsNotNull(thumb);
            Assert.IsTrue(thumb.X + thumb.ActualWidth <= slider.X + slider.ActualWidth,
                "the thumb is placed at left: 100% of the CONTENT box, so the theme has to "
                + "reserve a gutter of one thumb width or the thumb hangs over the right edge");
        }

        [TestMethod]
        public void EveryMenuItemSpansThePanelWhileThePanelHugsItsWidest()
        {
            var menu = new Menu { Name = "menu" };
            var wide = new MenuItem { Name = "wide", Text = "Comfortable" };
            var narrow = new MenuItem { Name = "narrow", Text = "Cosy" };

            menu.AddChildren(wide, narrow);
            _root.AddChild(menu);

            menu.Open = true;
            Layout();

            Assert.AreEqual(wide.ActualWidth, narrow.ActualWidth,
                "a hover or a selection paints the ITEM, so every row has to span the panel or "
                + "a short entry gets a short bar");
            Assert.AreEqual(menu.Panel.ContentWidth, narrow.ActualWidth);

            Assert.IsTrue(menu.Panel.ContentWidth < 200,
                "and the panel still hugs its widest item rather than swelling to the whole "
                + "viewport, which is what filling children used to do to a ? container");
        }

        [TestMethod]
        public void ABarItemStillHugsItsOwnLabel()
        {
            var bar = new MenuBar { Name = "bar" };
            var file = new MenuItem { Name = "file", Text = "File" };
            var help = new MenuItem { Name = "help", Text = "A much longer label" };

            bar.AddChildren(file, help);
            _root.AddChild(bar);

            Layout();

            Assert.IsTrue(help.ActualWidth > file.ActualWidth + 20,
                "in a BAR the width is the main axis, so an item with no width takes a share of "
                + "the row and every menu comes out the same size - the fill that a panel wants "
                + "is exactly wrong here");
        }
    }
}
