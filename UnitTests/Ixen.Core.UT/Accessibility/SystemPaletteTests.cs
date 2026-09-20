using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class SystemPaletteTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private static SystemPalette Palette()
            => new SystemPalette
            {
                Surface = "#000000",
                Text = "#FFFFFF",
                Control = "#101010",
                ControlText = "#FFFFFF",
                Accent = "#FFFF00",
                AccentText = "#000000",
                Disabled = "#808080"
            };

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void NothingIsAskedForByDefault()
        {
            var surface = new IxenSurface();

            Assert.IsFalse(surface.HighContrast);
            Assert.IsNull(surface.SystemColors);
        }

        [TestMethod]
        public void AskingForHighContrastRestylesTheTree()
        {
            Layout();

            _surface.HighContrast = true;

            Layout();

            Assert.IsTrue(_surface.LastLayoutRan, "turning high contrast on asked for no pass");
        }

        [TestMethod]
        public void TheSameAnswerTwiceDoesNotRestyleTheTree()
        {
            _surface.HighContrast = true;

            Layout();
            Layout();

            Assert.IsFalse(_surface.LastLayoutRan);

            _surface.HighContrast = true;

            Layout();

            Assert.IsFalse(_surface.LastLayoutRan, "an unchanged answer asked for a pass");
        }

        [TestMethod]
        public void APaletteRestylesTheTree()
        {
            Layout();

            _surface.SystemColors = Palette();

            Layout();

            Assert.IsTrue(_surface.LastLayoutRan, "a new palette asked for no pass");
        }

        [TestMethod]
        public void ThePaletteBecomesTokens()
        {
            _surface.SystemColors = Palette();

            StyleTokens tokens = _surface.Styles.Tokens;

            Assert.AreEqual("#000000", tokens.ValueOf(SystemPalette.SURFACE));
            Assert.AreEqual("#FFFFFF", tokens.ValueOf(SystemPalette.TEXT));
            Assert.AreEqual("#101010", tokens.ValueOf(SystemPalette.CONTROL));
            Assert.AreEqual("#FFFFFF", tokens.ValueOf(SystemPalette.CONTROL_TEXT));
            Assert.AreEqual("#FFFF00", tokens.ValueOf(SystemPalette.ACCENT));
            Assert.AreEqual("#000000", tokens.ValueOf(SystemPalette.ACCENT_TEXT));
            Assert.AreEqual("#808080", tokens.ValueOf(SystemPalette.DISABLED));
        }

        [TestMethod]
        public void ASheetDeclarationBeatsTheSystemWhateverTheOrder()
        {
            StyleTokens tokens = _surface.Styles.Tokens;

            tokens.Declare(SystemPalette.ACCENT, "#4C6EF5", false);

            _surface.SystemColors = Palette();

            Assert.AreEqual("#4C6EF5", tokens.ValueOf(SystemPalette.ACCENT),
                "a sheet registered before the host read the system palette still wins");

            var second = new IxenSurface(new VisualElement()) { Styles = new StyleRegistry() };

            second.SystemColors = Palette();
            second.Styles.Tokens.Declare(SystemPalette.ACCENT, "#4C6EF5", false);

            Assert.AreEqual("#4C6EF5", second.Styles.Tokens.ValueOf(SystemPalette.ACCENT),
                "and so does one registered after it");
        }

        [TestMethod]
        public void AnApplicationOverrideBeatsTheSystem()
        {
            _surface.SystemColors = Palette();

            StyleTokens tokens = _surface.Styles.Tokens;

            tokens.Set(SystemPalette.ACCENT, "#E8590C");

            Assert.AreEqual("#E8590C", tokens.ValueOf(SystemPalette.ACCENT));

            tokens.Reset(SystemPalette.ACCENT);

            Assert.AreEqual("#FFFF00", tokens.ValueOf(SystemPalette.ACCENT),
                "resetting an override should give the system colour back");
        }

        [TestMethod]
        public void ALaterPaletteReplacesTheEarlierOne()
        {
            _surface.SystemColors = Palette();

            SystemPalette second = Palette();
            second.Accent = "#123456";

            _surface.SystemColors = second;

            Assert.AreEqual("#123456", _surface.Styles.Tokens.ValueOf(SystemPalette.ACCENT));
        }

        [TestMethod]
        public void AnEntryThatIsNotAColourIsNotDeclared()
        {
            SystemPalette palette = Palette();
            palette.Accent = "not a colour";

            _surface.SystemColors = palette;

            Assert.AreEqual(StyleTokens.MARKER + SystemPalette.ACCENT,
                _surface.Styles.Tokens.ValueOf(SystemPalette.ACCENT),
                "a refused entry should leave the token unresolved");
        }

        [TestMethod]
        public void ANullPaletteIsAccepted()
        {
            _surface.SystemColors = Palette();
            _surface.SystemColors = null;

            Assert.IsNull(_surface.SystemColors);
            Assert.AreEqual("#000000", _surface.Styles.Tokens.ValueOf(SystemPalette.SURFACE),
                "clearing the reference should not unpublish what was already read");
        }

        [TestMethod]
        public void AFullPaletteIsComplete()
        {
            Assert.IsTrue(Palette().IsComplete);
        }

        [TestMethod]
        public void APaletteMissingOneColourIsNot()
        {
            SystemPalette palette = Palette();
            palette.Disabled = null;

            Assert.IsFalse(palette.IsComplete);
        }

        [TestMethod]
        public void APaletteHoldingRubbishIsNot()
        {
            SystemPalette palette = Palette();
            palette.Text = "white";

            Assert.IsFalse(palette.IsComplete);
        }

        [TestMethod]
        public void ARuleNamingASystemTokenPaintsTheSystemColour()
        {
            var box = new VisualElement { Name = "box" };
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            box.Styles.Background = new BackgroundStyleDescriptor
            {
                Color = StyleTokens.MARKER + SystemPalette.ACCENT
            };

            _root.AddChild(box);

            _surface.SystemColors = Palette();

            Layout();

            using (SKBitmap bitmap = _surface.RenderToBitmap())
            {
                SKColor pixel = bitmap.GetPixel(50, 50);

                Assert.AreEqual(255, pixel.Red);
                Assert.AreEqual(255, pixel.Green);
                Assert.AreEqual(0, pixel.Blue);
            }
        }
    }
}
