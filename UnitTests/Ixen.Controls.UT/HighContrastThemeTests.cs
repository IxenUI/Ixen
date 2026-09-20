using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.StyleSheets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class HighContrastThemeTests
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

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private static SystemPalette Palette()
            => new SystemPalette
            {
                Surface = "#000000",
                Text = "#FFFFFF",
                Control = "#1F1F1F",
                ControlText = "#FFFFFF",
                Accent = "#FFFF00",
                AccentText = "#000000",
                Disabled = "#3FF23F"
            };

        private void TurnOn()
        {
            _surface.SystemColors = Palette();
            _surface.HighContrast = true;
        }

        private string Ground(VisualElement element)
        {
            Layout();
            return element.StylesHandlers.Background.Color.ToRGBHexColor();
        }

        private string Ink(VisualElement element)
        {
            Layout();
            return element.StylesHandlers.Color.Brush.Color.ToRGBHexColor();
        }

        [TestMethod]
        public void EveryTokenTheThemeDeclaresTakesASystemColour()
        {
            TurnOn();

            Assert.IsTrue(HighContrastTheme.Sync(_surface));

            SystemPalette palette = Palette();

            var system = new HashSet<string>
            {
                palette.Surface, palette.Text, palette.Control, palette.ControlText,
                palette.Accent, palette.AccentText, palette.Disabled
            };

            int examined = 0;

            foreach (KeyValuePair<string, string> entry in new DefaultTheme_StyleSheet().Tokens)
            {
                string value = _registry.Tokens.ValueOf(entry.Key);

                Assert.IsTrue(system.Contains(value),
                    $"the theme declares '{entry.Key}' and the mapper says nothing about it, "
                    + $"so it kept {value} on a black screen");

                examined++;
            }

            Assert.IsTrue(examined >= 14,
                $"only {examined} tokens were examined, so this guard proves nothing");
        }

        [TestMethod]
        public void TheWholeThemeTakesTheSystemPaletteThroughSyncAlone()
        {
            var box = new CheckBox { Name = "agree" };
            var menu = new Menu { Name = "menu" };

            menu.AddChild(new MenuItem { Name = "one", Text = "One" });

            _root.AddChild(box);
            _root.AddChild(menu);

            menu.Open = true;

            TurnOn();

            Assert.IsTrue(HighContrastTheme.Sync(_surface));

            Assert.AreEqual("#1F1F1F", Ground(_button), "a button takes the system control colour");
            Assert.AreEqual("#000000", Ground(box), "a field takes the system surface");
            Assert.AreEqual("#000000", Ground(menu.Panel), "and so does a floating panel");
            Assert.AreEqual("#FFFFFF", Ink(_button), "the text takes the system control text");
        }

        [TestMethod]
        public void TheBordersBecomeTextColouredSoEverythingIsOutlined()
        {
            TurnOn();
            HighContrastTheme.Sync(_surface);

            Layout();

            Assert.AreEqual("#FFFFFF", _button.StylesHandlers.Border.Color.ToRGBHexColor(),
                "an outline is what distinguishes a control when no shade may be relied on");
        }

        [TestMethod]
        public void WithHighContrastOffNothingIsMapped()
        {
            _surface.SystemColors = Palette();

            Assert.IsFalse(HighContrastTheme.Sync(_surface));
            Assert.AreEqual("#F4F5F8", Ground(_button));
        }

        [TestMethod]
        public void WithNoPaletteNothingIsMapped()
        {
            _surface.HighContrast = true;

            Assert.IsFalse(HighContrastTheme.Sync(_surface));
            Assert.AreEqual("#F4F5F8", Ground(_button));
        }

        [TestMethod]
        public void AnIncompletePaletteIsRefusedRatherThanHalfApplied()
        {
            SystemPalette palette = Palette();
            palette.Disabled = null;

            _surface.SystemColors = palette;
            _surface.HighContrast = true;

            Assert.IsFalse(HighContrastTheme.Sync(_surface));
            Assert.AreEqual("#F4F5F8", Ground(_button),
                "half a palette would leave a light shade beside a black one");
        }

        [TestMethod]
        public void ReleasingGivesTheThemeBack()
        {
            TurnOn();
            HighContrastTheme.Sync(_surface);

            Assert.AreEqual("#1F1F1F", Ground(_button));

            _surface.HighContrast = false;

            Assert.IsFalse(HighContrastTheme.Sync(_surface));
            Assert.AreEqual("#F4F5F8", Ground(_button));
        }

        [TestMethod]
        public void ReleasingForgetsAnOverrideTheApplicationHadSet()
        {
            _surface.SetToken(HighContrastTheme.SURFACE, "#2E3138");

            TurnOn();
            HighContrastTheme.Sync(_surface);

            Assert.AreEqual("#1F1F1F", Ground(_button), "the system wins while the mode is on");

            _surface.HighContrast = false;
            HighContrastTheme.Sync(_surface);

            Assert.AreEqual("#F4F5F8", Ground(_button),
                "releasing goes back to the theme rather than to what the application had set");
        }

        [TestMethod]
        public void SyncAsksForAFrame()
        {
            Layout();
            Layout();

            Assert.IsFalse(_surface.LastLayoutRan);

            TurnOn();
            Layout();

            HighContrastTheme.Sync(_surface);
            Layout();

            Assert.IsTrue(_surface.LastLayoutRan, "mapping the palette asked for no pass");
        }

        [TestMethod]
        public void ANullSurfaceIsRefused()
        {
            Assert.IsFalse(HighContrastTheme.Sync(null));
        }
    }
}
