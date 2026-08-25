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
            Assert.AreEqual(25, Root().Styles.Count, string.Join(", ", Root().Styles.Select(s => s.GetType().Name)));
        }

        [TestMethod]
        public void TheOverflowSurvivesGeneration()
        {
            Assert.AreEqual(OverflowKind.Scroll, Style<OverflowStyleDescriptor>().Value);
        }

        [TestMethod]
        public void TheBackgroundImageSurvivesGeneration()
        {
            BackgroundStyleDescriptor background = Style<BackgroundStyleDescriptor>();

            Assert.AreEqual("#FF0000", background.Color);
            Assert.AreEqual("Assets/Images/logo.png", background.ImageUrl,
                "a path with slashes must survive both the tokenizer and the generated source");
            Assert.IsTrue(background.RepeatX);
            Assert.IsFalse(background.RepeatY);

            Assert.AreEqual(BackgroundStyleDescriptor.UNSET_POSITION, background.PositionX,
                "only the axis the keyword names is set");
            Assert.AreEqual(1f, background.PositionY);
        }

        [TestMethod]
        public void TheObjectFitSurvivesGeneration()
        {
            Assert.AreEqual(ObjectFit.ScaleDown, Style<ObjectFitStyleDescriptor>().Value,
                "a hyphenated enum value must survive the round trip");
        }

        [TestMethod]
        public void TheAnimationSurvivesGeneration()
        {
            AnimationStyleDescriptor animation = Style<AnimationStyleDescriptor>();

            Assert.AreEqual("generated_pulse", animation.Name);
            Assert.AreEqual(480, animation.Duration);
            Assert.AreEqual(60, animation.Delay);
            Assert.AreEqual(EasingKind.EaseOut, animation.Easing);
            Assert.AreEqual(AnimationStyleDescriptor.INFINITE, animation.Iterations);
            Assert.IsTrue(animation.Alternate);
        }

        [TestMethod]
        public void TheKeyframesSurviveGeneration()
        {
            KeyframesSet keyframes = StyleRegistry.Default.GetKeyframes("generated_pulse");

            Assert.IsNotNull(keyframes, "a keyframes block must reach the registry through the generated sheet");
            Assert.AreEqual(3, keyframes.Frames.Count);

            CollectionAssert.AreEquivalent(
                new[] { StyleIdentifier.BACKGROUND, StyleIdentifier.WIDTH },
                keyframes.Properties.ToList(),
                "both a colour and a size track must survive");

            Assert.AreEqual(0f, keyframes.Frames[0].Offset);
            Assert.AreEqual(0.5f, keyframes.Frames[1].Offset);
            Assert.AreEqual(1f, keyframes.Frames[2].Offset);

            var last = (SizeStyleDescriptor)keyframes.Frames[2].Styles
                .Single(s => s is WidthStyleDescriptor);

            Assert.AreEqual(30.5f, last.Value, "a fractional stop value survives the round trip");
        }

        [TestMethod]
        public void TheCursorSurvivesGeneration()
        {
            Assert.AreEqual(CursorKind.Hand, Style<CursorStyleDescriptor>().Value);
        }

        [TestMethod]
        public void TheTransitionSurvivesGeneration()
        {
            TransitionStyleDescriptor transition = Style<TransitionStyleDescriptor>();

            Assert.AreEqual(150, transition.DurationOf(StyleIdentifier.BACKGROUND));
            Assert.AreEqual(200, transition.DurationOf(StyleIdentifier.COLOR), "0.2s is 200ms");
            Assert.AreEqual(0, transition.DurationOf(StyleIdentifier.BORDER), "what is not declared does not animate");
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

        [TestMethod]
        public void TheShadowsSurviveGeneration()
        {
            BoxShadowStyleDescriptor box = Style<BoxShadowStyleDescriptor>();

            Assert.AreEqual(-1f, box.OffsetX, "a negative offset makes it through the tokenizer and the parser");
            Assert.AreEqual(2.5f, box.OffsetY);
            Assert.AreEqual(6f, box.Blur);
            Assert.AreEqual(3f, box.Spread);
            Assert.AreEqual("#40112233", box.Color);

            TextShadowStyleDescriptor text = Style<TextShadowStyleDescriptor>();

            Assert.AreEqual(1f, text.OffsetY);
            Assert.AreEqual(2.5f, text.Blur);
            Assert.AreEqual("#80445566", text.Color);
        }

        [TestMethod]
        public void AGradientSurvivesGeneration()
        {
            StyleClass gradient = StyleRegistry.Default.GetScopedClass(
                StyleClassTarget.ElementName, "generated_gradient", null, "generated_root");

            Assert.IsNotNull(gradient, "the nested generated_gradient class should be registered");

            var background = (BackgroundStyleDescriptor)gradient.Styles.Single();

            Assert.IsNotNull(background.Gradient);
            Assert.AreEqual(GradientKind.Linear, background.Gradient.Kind);
            Assert.AreEqual(45f, background.Gradient.Angle);
            Assert.AreEqual(3, background.Gradient.Stops.Count);
            Assert.AreEqual("#112233", background.Gradient.Stops[0].Color);
            Assert.AreEqual(0.2f, background.Gradient.Stops[0].Offset);
            Assert.IsFalse(background.Gradient.Stops[1].HasOffset);
        }
    }
}
