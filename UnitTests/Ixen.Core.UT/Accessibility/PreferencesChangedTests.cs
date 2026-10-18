using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class PreferencesChangedTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private IxenSurface _surface;
        private int _raised;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.PreferencesChanged += (sender, e) => _raised++;
        }

        private static SystemPalette Palette(string accent)
            => new SystemPalette
            {
                Surface = "#000000",
                Text = "#FFFFFF",
                Control = "#101010",
                ControlText = "#FFFFFF",
                Accent = accent,
                AccentText = "#000000",
                Disabled = "#808080"
            };

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void NothingChangedRaisesNothing()
        {
            Layout();

            Assert.AreEqual(0, _raised);
        }

        [TestMethod]
        public void ItIsRaisedAtTheNextFrameAndNotAtTheAssignment()
        {
            _surface.HighContrast = true;

            Assert.AreEqual(0, _raised, "raised from inside the setter");

            Layout();

            Assert.AreEqual(1, _raised);
        }

        [TestMethod]
        public void SeveralChangesInOneTurnAreOneEvent()
        {
            _surface.HighContrast = true;
            _surface.ReducedMotion = true;
            _surface.FontScale = 1.5f;
            _surface.SystemColors = Palette("#FFFF00");

            Layout();
            Layout();

            Assert.AreEqual(1, _raised);
        }

        [TestMethod]
        public void EachPreferenceCounts()
        {
            _surface.ReducedMotion = true;
            Layout();
            _surface.FontScale = 2f;
            Layout();
            _surface.SystemColors = Palette("#FFFF00");
            Layout();
            _surface.HighContrast = true;
            Layout();

            Assert.AreEqual(4, _raised);
        }

        [TestMethod]
        public void TheSameAnswerAgainRaisesNothing()
        {
            _surface.HighContrast = false;
            _surface.ReducedMotion = false;
            _surface.FontScale = 1f;

            Layout();

            Assert.AreEqual(0, _raised);
        }

        [TestMethod]
        public void AnEqualPaletteIsNotAChange()
        {
            _surface.SystemColors = Palette("#FFFF00");
            Layout();

            _surface.SystemColors = Palette("#FFFF00");
            Layout();

            Assert.AreEqual(1, _raised);
        }

        [TestMethod]
        public void ADifferentPaletteIsOne()
        {
            _surface.SystemColors = Palette("#FFFF00");
            Layout();

            _surface.SystemColors = Palette("#00FFFF");
            Layout();

            Assert.AreEqual(2, _raised);
            Assert.AreEqual("#00FFFF", _surface.SystemColors.Accent);
        }

        [TestMethod]
        public void AHandlerThatSetsATokenIsSeenOnTheSameFrame()
        {
            ((IElementHost)_surface).PreferencesChanged += (sender, e) => _surface.SetToken("accent", "#FF0000");

            _surface.ReducedMotion = true;
            Layout();

            Assert.IsTrue(_surface.LastLayoutRan, "a handler that restyles was left for the next frame");
            Assert.AreEqual(1, _raised);
        }
    }
}
