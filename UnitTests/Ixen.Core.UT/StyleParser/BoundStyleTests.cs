using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class BoundStyleTests
    {
        private static T Parse<T>(string style, string value)
            where T : BoundStyleDescriptor
        {
            var xnsSource = new XnsSource($"box {{ {style}: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (T)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string style, string value)
        {
            var xnsSource = new XnsSource($"box {{ {style}: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'{style}: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void AllFourReadAPixelValue()
        {
            Assert.AreEqual(120f, Parse<MinWidthStyleDescriptor>("min-width", "120px").Value);
            Assert.AreEqual(120f, Parse<MaxWidthStyleDescriptor>("max-width", "120px").Value);
            Assert.AreEqual(120f, Parse<MinHeightStyleDescriptor>("min-height", "120px").Value);
            Assert.AreEqual(120f, Parse<MaxHeightStyleDescriptor>("max-height", "120px").Value);
        }

        [TestMethod]
        public void ADecimalValueWorks()
        {
            Assert.AreEqual(12.5f, Parse<MaxWidthStyleDescriptor>("max-width", "12.5px").Value);
        }

        [TestMethod]
        public void APercentageIsRejected()
        {
            AssertRejected("max-width", "50%");
            AssertRejected("min-height", "10%");
        }

        [TestMethod]
        public void AWeightOrContentIsRejected()
        {
            AssertRejected("max-width", "1*");
            AssertRejected("min-width", "?");
        }

        [TestMethod]
        public void ANegativeBoundIsRejected()
        {
            AssertRejected("min-width", "-20px");
        }

        [TestMethod]
        public void AnUndeclaredBoundIsNotDeclared()
        {
            Assert.IsFalse(new MinWidthStyleDescriptor().IsDeclared);
            Assert.IsFalse(new MaxHeightStyleDescriptor().IsDeclared);
        }

        [TestMethod]
        public void AllFourRoundTripThroughGeneratedSource()
        {
            StringAssert.Contains(Parse<MinWidthStyleDescriptor>("min-width", "12px").ToSource(),
                "MinWidthStyleDescriptor");

            StringAssert.Contains(Parse<MaxHeightStyleDescriptor>("max-height", "34px").ToSource(),
                "Value = 34f");
        }

        [TestMethod]
        public void AMaxWidthMakesTextWrapEarlier()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var label = new VisualElement
            {
                Name = "label",
                Text = "the wild swans at coole are drifting on the still water"
            };

            label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            root.AddChild(label);

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            surface.ComputeLayout(600, 300);

            int wide = label.TextLines.Count;

            label.Styles.MaxWidth = new MaxWidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            label.Invalidate();
            surface.ComputeLayout(600, 300);

            Assert.IsTrue(label.TextLines.Count > wide,
                "the clamp happens before the text is laid out, so the wrap follows it "
                + "instead of the text overflowing a shrunken box");

            Assert.AreEqual(120f, label.Width);
        }
    }
}
