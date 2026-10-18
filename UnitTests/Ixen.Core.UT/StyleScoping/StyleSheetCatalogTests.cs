using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class StyleSheetCatalogTests
    {
        private sealed class HandWritten : StyleSheet
        {
        }

        private sealed class AlsoHandWritten : StyleSheet
        {
        }

        [TestMethod]
        public void EveryGeneratedSheetOfThisAssemblyRegisteredItselfWithNoReflection()
        {
            var registered = StyleSheetCatalog.Snapshot().Select(entry => entry.Type).ToList();

            Assert.IsTrue(registered.Contains(typeof(Ixen.StyleSheets.AllGeneratedStyles_StyleSheet)),
                "the module initializer the generator emits is what puts a sheet here");
            Assert.IsTrue(registered.Count > 10, "every .xns of the test assembly is one entry");
        }

        [TestMethod]
        public void TheDefaultRegistryIsBuiltFromTheCatalog()
        {
            StyleRegistry registry = StyleRegistry.CreateFromLoadedAssemblies();

            Assert.IsNotNull(registry.GetGlobal(StyleClassTarget.ElementName, "generated_root"),
                "a rule of AllGeneratedStyles.xns reaches the registry through its registration");
        }

        [TestMethod]
        public void RegisteringTwiceKeepsOneEntry()
        {
            StyleSheetCatalog.Register(typeof(HandWritten), () => new HandWritten(), false);
            StyleSheetCatalog.Register(typeof(HandWritten), () => new HandWritten(), false);

            Assert.AreEqual(1, StyleSheetCatalog.Snapshot().Count(entry => entry.Type == typeof(HandWritten)));
        }

        [TestMethod]
        public void ADefaultsEntryGoesToTheDefaultsLayerOnly()
        {
            StyleSheetCatalog.Register(typeof(AlsoHandWritten), () => new AlsoHandWritten(), true);

            StyleSheetCatalog.Entry entry = StyleSheetCatalog.Snapshot().Single(e => e.Type == typeof(AlsoHandWritten));

            Assert.IsTrue(entry.Defaults);
        }

        private sealed class LateSheet : StyleSheet
        {
            public LateSheet()
            {
                AddClass(new StyleClass(StyleClassTarget.ElementName, null, null, "late_registered",
                    new System.Collections.Generic.List<Ixen.Core.Visual.Styles.Descriptors.StyleDescriptor>()));
            }
        }

        [TestMethod]
        public void ASheetRegisteredAfterTheDefaultRegistryExistsJoinsIt()
        {
            StyleRegistry registry = StyleRegistry.Default;

            StyleSheetCatalog.Register(typeof(LateSheet), () => new LateSheet(), false);

            Assert.IsNotNull(registry.GetGlobal(StyleClassTarget.ElementName, "late_registered"),
                "an assembly whose module initializer runs after the registry was built still gets its styles");
        }

        [TestMethod]
        public void NothingIsAccepted()
        {
            Assert.Throws<ArgumentNullException>(() => StyleSheetCatalog.Register(null, () => null, false));
            Assert.Throws<ArgumentNullException>(() => StyleSheetCatalog.Register(typeof(HandWritten), null, false));
        }
    }
}
