using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class GeneratedStyleSheetTests
    {
        private static StyleClass Root()
        {
            StyleClass styleClass = StyleRegistry.Default.GetGlobalElementClass("generated_root");

            Assert.IsNotNull(styleClass, "the generated AllGeneratedStyles sheet should be registered");

            return styleClass;
        }

        private static T Style<T>() where T : StyleDescriptor
            => (T)Root().Styles.Single(s => s.GetType() == typeof(T));

        [TestMethod]
        public void EveryGeneratableStyleSurvivesGeneration()
        {
            Assert.AreEqual(18, Root().Styles.Count, string.Join(", ", Root().Styles.Select(s => s.GetType().Name)));
        }

        [TestMethod]
        public void TheTextStylesSurviveGeneration()
        {
            TextAlignStyleDescriptor align = Style<TextAlignStyleDescriptor>();

            Assert.AreEqual(TextAlign.Right, align.Horizontal, "both axes must survive one declaration");
            Assert.AreEqual(TextVAlign.Bottom, align.Vertical);
            Assert.AreEqual(TextWrap.NoWrap, Style<TextWrapStyleDescriptor>().Value);
            Assert.AreEqual(TextOverflow.Ellipsis, Style<TextOverflowStyleDescriptor>().Value);
        }

        [TestMethod]
        public void TheColorStyleIsNotDropped()
        {
            Assert.AreEqual("#123456", Style<ColorStyleDescriptor>().Value);
        }

        [TestMethod]
        public void TheBorderKeepsItsColourThicknessAndType()
        {
            BorderStyleDescriptor border = Style<BorderStyleDescriptor>();

            Assert.AreEqual("#CCCCCC", border.Color);
            Assert.AreEqual(1.5f, border.Thickness);
            Assert.AreEqual(BorderType.Inner, border.Type);
        }

        [TestMethod]
        public void TheCornerRadiusKeepsItsFourFractionalValues()
        {
            CornerRadiusStyleDescriptor radius = Style<CornerRadiusStyleDescriptor>();

            Assert.AreEqual(8.5f, radius.TopLeft, "TopLeft");
            Assert.AreEqual(4f, radius.TopRight, "TopRight");
            Assert.AreEqual(2f, radius.BottomRight, "BottomRight");
            Assert.AreEqual(1f, radius.BottomLeft, "BottomLeft");
        }

        [TestMethod]
        public void FractionalSpacingSurvivesGeneration()
        {
            Assert.AreEqual(1.5f, Style<MarginStyleDescriptor>().Top.Value, "margin top");
            Assert.AreEqual(4.25f, Style<PaddingStyleDescriptor>().Right.Value, "padding right");
        }

        [TestMethod]
        public void FractionalTemplatesSurviveGeneration()
        {
            RowTemplateStyleDescriptor rows = Style<RowTemplateStyleDescriptor>();

            Assert.AreEqual(2, rows.Value.Count);
            Assert.AreEqual(SizeUnit.Weight, rows.Value[0].Unit);
            Assert.AreEqual(1.5f, rows.Value[0].Value);
            Assert.AreEqual(SizeUnit.Pixels, rows.Value[1].Unit);
            Assert.AreEqual(20.5f, rows.Value[1].Value);
        }

        [TestMethod]
        public void TheFontStylesSurviveGeneration()
        {
            Assert.AreEqual("Segoe UI", Style<FontFamilyStyleDescriptor>().Value);
            Assert.AreEqual(13.5f, Style<FontSizeStyleDescriptor>().Value);
            Assert.AreEqual(FontWeight.Bold, Style<FontWeightStyleDescriptor>().Value);
            Assert.AreEqual(FontStyle.Italic, Style<FontStyleStyleDescriptor>().Value);
        }

        [TestMethod]
        public void APseudoClassSurvivesGeneration()
        {
            StyleClass stated = StyleRegistry.Default.GetScopedClass(
                StyleClassTarget.ElementName, "generated_child:hover", null, "generated_root");

            Assert.IsNotNull(stated, "the generated sheet must keep the state in the selector name");
            Assert.AreEqual(SizeUnit.Pixels, stated.Styles.OfType<WidthStyleDescriptor>().Single().Unit);
            Assert.IsTrue(StyleRegistry.Default.HasStateClasses, "and the registry must notice it");
        }

        [TestMethod]
        public void AFractionalPercentSurvivesGeneration()
        {
            StyleClass child = StyleRegistry.Default.GetScopedClass(
                StyleClassTarget.ElementName, "generated_child", null, "generated_root");

            Assert.IsNotNull(child);

            WidthStyleDescriptor width = child.Styles.OfType<WidthStyleDescriptor>().Single();

            Assert.AreEqual(SizeUnit.Percents, width.Unit);
            Assert.AreEqual(30.5f, width.Value);
        }
    }
}
