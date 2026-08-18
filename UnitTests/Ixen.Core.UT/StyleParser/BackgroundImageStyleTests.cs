using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class BackgroundImageStyleTests
    {
        private static BackgroundStyleDescriptor Valid(string value)
        {
            var parser = new BackgroundStyleParser(value);

            Assert.IsTrue(parser.IsValid, $"'{value}' should parse");

            return parser.Descriptor;
        }

        private static void Invalid(string value)
            => Assert.IsFalse(new BackgroundStyleParser(value).IsValid, $"'{value}' should not parse");

        [TestMethod]
        public void AColourAloneStillParsesExactlyAsBefore()
        {
            Assert.AreEqual("#F5F5F5", Valid("#F5F5F5").Color);
            Assert.AreEqual("#80FF3B30", Valid("#80FF3B30").Color);
            Assert.IsNull(Valid("#F5F5F5").ImageUrl);
        }

        [TestMethod]
        public void AnImageAloneParses()
        {
            BackgroundStyleDescriptor descriptor = Valid("logo.png");

            Assert.AreEqual("logo.png", descriptor.ImageUrl);
            Assert.IsNull(descriptor.Color);
        }

        [TestMethod]
        public void AColourAndAnImageComeInEitherOrder()
        {
            BackgroundStyleDescriptor first = Valid("#F5F5F5 logo.png");
            BackgroundStyleDescriptor second = Valid("logo.png #F5F5F5");

            Assert.AreEqual("#F5F5F5", first.Color);
            Assert.AreEqual("logo.png", first.ImageUrl);
            Assert.AreEqual(first.Color, second.Color);
            Assert.AreEqual(first.ImageUrl, second.ImageUrl);
        }

        [TestMethod]
        public void TheRepeatKeywordsSetBothAxes()
        {
            BackgroundStyleDescriptor both = Valid("logo.png repeat");
            Assert.IsTrue(both.RepeatX);
            Assert.IsTrue(both.RepeatY);

            BackgroundStyleDescriptor horizontal = Valid("logo.png repeat-x");
            Assert.IsTrue(horizontal.RepeatX);
            Assert.IsFalse(horizontal.RepeatY);

            BackgroundStyleDescriptor vertical = Valid("logo.png repeat-y");
            Assert.IsFalse(vertical.RepeatX);
            Assert.IsTrue(vertical.RepeatY);

            BackgroundStyleDescriptor none = Valid("logo.png no-repeat");
            Assert.IsFalse(none.RepeatX);
            Assert.IsFalse(none.RepeatY);
        }

        [TestMethod]
        public void NotRepeatingIsTheDefault()
        {
            BackgroundStyleDescriptor descriptor = Valid("logo.png");

            Assert.IsFalse(descriptor.RepeatX);
            Assert.IsFalse(descriptor.RepeatY);
        }

        [TestMethod]
        public void ABareWordIsStillAnError()
        {
            Invalid("red");
            Invalid("transparent");

            Assert.IsFalse(new BackgroundStyleParser("red").IsValid,
                "a mistyped colour must stay XN003, not become a reference to a missing image");
        }

        [TestMethod]
        public void ABadColourIsStillAnError()
        {
            Invalid("#GGGGGG");
            Invalid("#FFF");
        }

        [TestMethod]
        public void TheFitKeywordsScaleTheImage()
        {
            Assert.AreEqual(ObjectFit.Cover, Valid("background.jpg cover").Fit);
            Assert.AreEqual(ObjectFit.Contain, Valid("background.jpg contain").Fit);
            Assert.AreEqual(ObjectFit.Fill, Valid("background.jpg fill").Fit);
            Assert.AreEqual(ObjectFit.Fill, Valid("background.jpg stretch").Fit);
            Assert.AreEqual(ObjectFit.None, Valid("background.jpg auto").Fit);
        }

        [TestMethod]
        public void NaturalSizeIsTheDefaultFit()
        {
            BackgroundStyleDescriptor descriptor = Valid("logo.png");

            Assert.AreEqual(ObjectFit.None, descriptor.Fit);
            Assert.IsFalse(descriptor.IsScaled, "so nothing about the existing behaviour changed");
        }

        [TestMethod]
        public void AFitCombinesWithNoRepeatAndWithAColour()
        {
            BackgroundStyleDescriptor descriptor = Valid("#EEF2FF background.jpg cover no-repeat");

            Assert.AreEqual("#EEF2FF", descriptor.Color);
            Assert.AreEqual(ObjectFit.Cover, descriptor.Fit);
            Assert.IsFalse(descriptor.RepeatX);
        }

        [TestMethod]
        public void AFitTogetherWithRepeatingIsAnError()
        {
            Invalid("tile.png repeat cover");
            Invalid("tile.png repeat-x contain");

            Assert.IsTrue(new BackgroundStyleParser("tile.png repeat auto").IsValid,
                "auto is the natural size, so it does not conflict with tiling");
        }

        [TestMethod]
        public void AFitWithNoImageIsAnError()
        {
            Invalid("cover");
            Invalid("#F5F5F5 cover");
        }

        [TestMethod]
        public void ThePositionKeywordsMapToFractions()
        {
            Assert.AreEqual(0f, Valid("hero.jpg left").PositionX);
            Assert.AreEqual(0.5f, Valid("hero.jpg center").PositionX);
            Assert.AreEqual(1f, Valid("hero.jpg right").PositionX);
            Assert.AreEqual(0f, Valid("hero.jpg top").PositionY);
            Assert.AreEqual(0.5f, Valid("hero.jpg middle").PositionY);
            Assert.AreEqual(1f, Valid("hero.jpg bottom").PositionY);
        }

        [TestMethod]
        public void EachKeywordSetsOneAxisOnly()
        {
            BackgroundStyleDescriptor descriptor = Valid("hero.jpg right");

            Assert.AreEqual(1f, descriptor.PositionX);
            Assert.AreEqual(BackgroundStyleDescriptor.UNSET_POSITION, descriptor.PositionY,
                "center/middle is the same horizontal/vertical split text-align uses");
        }

        [TestMethod]
        public void APositionMixesWithEverythingElseInAnyOrder()
        {
            BackgroundStyleDescriptor one = Valid("#EEF2FF hero.jpg cover right bottom");
            BackgroundStyleDescriptor two = Valid("bottom cover right #EEF2FF hero.jpg");

            Assert.AreEqual(one.Color, two.Color);
            Assert.AreEqual(one.ImageUrl, two.ImageUrl);
            Assert.AreEqual(one.Fit, two.Fit);
            Assert.AreEqual(one.PositionX, two.PositionX);
            Assert.AreEqual(one.PositionY, two.PositionY);
        }

        [TestMethod]
        public void TheAnchorDefaultsDependOnWhetherThePictureIsScaled()
        {
            BackgroundStyleDescriptor natural = Valid("logo.png");

            Assert.IsFalse(natural.HasPosition);
            Assert.AreEqual(0f, natural.AnchorX, "an unscaled picture anchors top-left");
            Assert.AreEqual(0f, natural.AnchorY);

            BackgroundStyleDescriptor scaled = Valid("hero.jpg cover");

            Assert.AreEqual(0.5f, scaled.AnchorX, "a scaled one centres");
            Assert.AreEqual(0.5f, scaled.AnchorY);

            BackgroundStyleDescriptor stated = Valid("hero.jpg cover left top");

            Assert.AreEqual(0f, stated.AnchorX, "and an explicit position overrides either default");
            Assert.AreEqual(0f, stated.AnchorY);
        }

        [TestMethod]
        public void APositionWithNoImageIsAnError()
        {
            Invalid("right");
            Invalid("#F5F5F5 bottom");
        }

        [TestMethod]
        public void ARepeatWithNoImageIsAnError()
        {
            Invalid("#F5F5F5 repeat");
            Invalid("repeat");
        }

        [TestMethod]
        public void AnEmptyValueIsAnError()
        {
            Invalid("");
            Invalid("   ");
        }

        [TestMethod]
        public void APathWithFoldersTokenizes()
        {
            var source = new XnsSource(@"panel {
    background: #F5F5F5 Assets/Images/logo.png no-repeat
}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var descriptor = (BackgroundStyleDescriptor)set.Classes[0].Styles[0];

            Assert.AreEqual("Assets/Images/logo.png", descriptor.ImageUrl,
                "a slash had to be added to the value character set for this to be expressible");
        }

        [TestMethod]
        public void ATrailingLineCommentIsNotSwallowedByAPath()
        {
            var source = new XnsSource(@"panel {
    background: Assets/logo.png  // the logo
    width: 100px
}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var descriptor = (BackgroundStyleDescriptor)set.Classes[0].Styles[0];

            Assert.AreEqual("Assets/logo.png", descriptor.ImageUrl,
                "the value must stop at the comment, not eat it");
            Assert.AreEqual(2, set.Classes[0].Styles.Count, "and the next style must still be seen");
        }

        [TestMethod]
        public void ABlockCommentAfterAPathIsNotSwallowedEither()
        {
            var source = new XnsSource(@"panel {
    background: Assets/logo.png /* the logo */
    width: 100px
}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            Assert.AreEqual("Assets/logo.png",
                ((BackgroundStyleDescriptor)set.Classes[0].Styles[0]).ImageUrl);
        }

        [TestMethod]
        public void ACommentRightAfterAColonStillBehavesAsBefore()
        {
            var source = new XnsSource(@"panel {
    width: 100px  // a width
}");
            source.Compile();

            Assert.IsFalse(source.HasErrors,
                "slash stays out of the first character set, so nothing about comments changed");
        }

        [TestMethod]
        public void ItSurvivesGeneratedSource()
        {
            BackgroundStyleDescriptor descriptor = Valid("#F5F5F5 Assets/logo.png repeat-x");

            Assert.IsTrue(descriptor.CanGenerateSource);

            string source = descriptor.ToSource();

            StringAssert.Contains(source, "\"#F5F5F5\"");
            StringAssert.Contains(source, "\"Assets/logo.png\"");
            StringAssert.Contains(source, "RepeatX = true");
            StringAssert.Contains(source, "RepeatY = false");
        }
    }
}
