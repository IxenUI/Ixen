using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class StyleFormatVersionTests
    {
        private class StaleSheet : StyleSheet
        {
            public override int FormatVersion => StyleFormat.VERSION - 1;

            internal StaleSheet()
            {
                AddClass(new StyleClass(StyleClassTarget.ElementName, null, null, "box",
                    new List<StyleDescriptor>
                    {
                        new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 }
                    }));
            }
        }

        private class FutureSheet : StyleSheet
        {
            public override int FormatVersion => StyleFormat.VERSION + 1;
        }

        private class CurrentSheet : StyleSheet
        {
            internal CurrentSheet()
            {
                AddClass(new StyleClass(StyleClassTarget.ElementName, null, null, "box",
                    new List<StyleDescriptor>
                    {
                        new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 }
                    }));
            }
        }

        [TestMethod]
        public void AnOlderSheetIsRefused()
        {
            var registry = new StyleRegistry();

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => registry.Add(new StaleSheet()),
                "a stylesheet is compiled code, so one built by an older generator describes its "
                + "descriptors in a shape this build may read differently - and reading it wrongly "
                + "is silent, which is what makes the version worth carrying");

            Assert.IsTrue(error.Message.Contains(nameof(StaleSheet)),
                "the message has to name the type, because the fix is to rebuild the assembly "
                + "that carries it and nothing else says which one that is");
        }

        [TestMethod]
        public void ANewerSheetIsRefusedToo()
        {
            var registry = new StyleRegistry();

            Assert.Throws<InvalidOperationException>(() => registry.Add(new FutureSheet()),
                "the mismatch is what matters, not the direction: a newer sheet against an older "
                + "Core is the same trap seen from the other side");
        }

        [TestMethod]
        public void TheDefaultsLayerChecksItAsWell()
        {
            var registry = new StyleRegistry();

            Assert.Throws<InvalidOperationException>(() => registry.AddDefaults(new StaleSheet()),
                "a control library ships its theme as defaults, which is exactly the case where a "
                + "compiled sheet arrives from another assembly");
        }

        [TestMethod]
        public void AMatchingSheetGoesIn()
        {
            var registry = new StyleRegistry();

            registry.Add(new CurrentSheet());

            Assert.IsNotNull(registry.GetGlobal(StyleClassTarget.ElementName, "box"));
        }

        [TestMethod]
        public void ASheetBuiltByHandIsNotStale()
        {
            var registry = new StyleRegistry();
            var sheet = new StyleSheet();

            Assert.AreEqual(StyleFormat.VERSION, sheet.FormatVersion,
                "the default is the current format, so building a sheet in code stays legal - "
                + "only a sheet that declares a different number is refused, and only a generated "
                + "one does that");

            registry.Add(sheet);
        }

        [TestMethod]
        public void AGeneratedSheetDeclaresTheFormat()
        {
            var sheet = new Ixen.StyleSheets.AllGeneratedStyles_StyleSheet();

            Assert.AreEqual(typeof(Ixen.StyleSheets.AllGeneratedStyles_StyleSheet),
                sheet.GetType().GetProperty(nameof(StyleSheet.FormatVersion))
                    .GetGetMethod().DeclaringType,
                "the generated class has to DECLARE the override, not inherit it - the generator "
                + "emits the constant and the consumer's compiler inlines it, so the sheet ends "
                + "up carrying the number that was current when its assembly was built. Comparing "
                + "the value alone proves nothing, because the inherited default answers the same");

            Assert.AreEqual(StyleFormat.VERSION, sheet.FormatVersion);

            new StyleRegistry().Add(sheet);
        }

        [TestMethod]
        public void NothingIsRefusedForBeingNull()
        {
            new StyleRegistry().Add((StyleSheet)null);
        }
    }
}
