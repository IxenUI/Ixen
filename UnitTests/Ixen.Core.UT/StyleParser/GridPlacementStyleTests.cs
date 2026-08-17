using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class GridPlacementStyleTests
    {
        private static StyleDescriptor Compile(string style)
        {
            var source = new XnsSource("el { " + style + " }");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(e => e.Message)));

            return set.Classes.Single().Styles.Single();
        }

        private static bool Rejects(string style)
        {
            var source = new XnsSource("el { " + style + " }");
            source.Compile();

            return source.HasErrors;
        }

        [TestMethod]
        public void AnIndexIsParsedFromXns()
        {
            Assert.AreEqual(2, ((ColumnIndexStyleDescriptor)Compile("column-index: 2")).Value);
            Assert.AreEqual(0, ((RowIndexStyleDescriptor)Compile("row-index: 0")).Value);
        }

        [TestMethod]
        public void AutoIsTheDefaultAndCanBeSaidOutLoud()
        {
            var descriptor = (ColumnIndexStyleDescriptor)Compile("column-index: auto");

            Assert.IsTrue(descriptor.IsAuto);
            Assert.IsTrue(new ColumnIndexStyleDescriptor().IsAuto, "and it is what an unset one means");
        }

        [TestMethod]
        public void ASpanIsParsedFromXns()
        {
            Assert.AreEqual(3, ((ColumnSpanStyleDescriptor)Compile("column-span: 3")).Value);
            Assert.AreEqual(2, ((RowSpanStyleDescriptor)Compile("row-span: 2")).Value);
            Assert.AreEqual(1, new RowSpanStyleDescriptor().Value, "one by default");
        }

        [TestMethod]
        public void ANegativeIndexIsRejected()
        {
            Assert.IsTrue(Rejects("column-index: -1"));
            Assert.IsTrue(Rejects("row-index: nope"));
        }

        [TestMethod]
        public void ASpanBelowOneIsRejected()
        {
            Assert.IsTrue(Rejects("column-span: 0"));
            Assert.IsTrue(Rejects("row-span: -2"));
            Assert.IsTrue(Rejects("column-span: auto"), "a span is a count, not a placement");
        }

        [TestMethod]
        public void TheGeneratedSourceRoundTrips()
        {
            var column = new ColumnIndexStyleDescriptor { Value = 4 };
            var span = new RowSpanStyleDescriptor { Value = 2 };

            Assert.IsTrue(StyleDescriptorSource(column).Contains("4"));
            Assert.IsTrue(StyleDescriptorSource(span).Contains("2"));
        }

        private static string StyleDescriptorSource(StyleDescriptor descriptor)
        {
            System.Reflection.MethodInfo method = typeof(StyleDescriptor)
                .GetMethod("ToSource", System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Public);

            return (string)method.Invoke(descriptor, null);
        }
    }
}
