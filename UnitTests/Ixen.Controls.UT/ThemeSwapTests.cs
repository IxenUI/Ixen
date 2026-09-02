using Ixen.Core;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class ThemeSwapTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private Button _button;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _button = new Button { Name = "save", Text = "Save" };

            _root.AddChild(_button);

            _surface = new IxenSurface(_root);
        }

        private static StyleRegistry Theme(string source)
        {
            var registry = new StyleRegistry();

            registry.AddLoadedDefaults();

            if (source != null)
            {
                var sheet = new XnsSource(source);
                ClassesSet set = sheet.Compile();

                Assert.IsFalse(sheet.HasErrors,
                    string.Join(" | ", sheet.Diagnostics.Select(d => d.Message)));

                registry.Add(set);
            }

            return registry;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private string Background() => _button.StylesHandlers.Background.Descriptor.Color;

        private float Padding() => _button.PaddingLeft;

        [TestMethod]
        public void AddLoadedDefaultsFindsTheControlLibraryTheme()
        {
            _surface.Styles = Theme(null);

            Layout();

            Assert.IsNotNull(Background(),
                "an application swapping the registry must not lose the control library's theme, "
                + "which is what makes a theme switch usable with Ixen.Controls at all");

            Assert.IsTrue(Padding() > 0);
        }

        [TestMethod]
        public void SwappingTheThemeRestylesAndKeepsTheDefaults()
        {
            _surface.Styles = Theme("#Button { background: #112233 }");

            Layout();

            Assert.AreEqual("#112233", Background());

            _surface.Styles = Theme("#Button { background: #445566 }");

            Layout();

            Assert.AreEqual("#445566", Background(),
                "no Invalidate from the caller - the setter owns that");

            Assert.IsTrue(Padding() > 0,
                "and the theme's padding is still there, since only the application layer changed");
        }

        [TestMethod]
        public void SwappingBackToTheThemeAloneGivesTheThemeLookBack()
        {
            _surface.Styles = Theme("#Button { background: #112233 }");

            Layout();

            string themed = Background();

            _surface.Styles = Theme(null);

            Layout();

            Assert.AreNotEqual(themed, Background());
            Assert.IsNotNull(Background());
        }
    }
}
