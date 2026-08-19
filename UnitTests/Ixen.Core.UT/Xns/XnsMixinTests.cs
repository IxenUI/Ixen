using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsMixinTests
    {
        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private static List<StyleDescriptor> StylesOf(string xns, string name)
            => Compile(xns).Classes.Single(c => c.Name == name).Styles;

        private static T Resolved<T>(List<StyleDescriptor> styles) where T : StyleDescriptor
            => styles.OfType<T>().Last();

        private static void AssertRejected(string xns, string expected)
        {
            var source = new XnsSource(xns);
            source.Compile();

            Assert.IsTrue(source.HasErrors, "should have been rejected");
            StringAssert.Contains(
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)), expected);
        }

        private const string CHIP =
            "@mixin chip {\r\n    corner-radius: 6px\r\n    text-align: center middle\r\n}\r\n";

        [TestMethod]
        public void AnIncludeBringsInTheDeclarations()
        {
            List<StyleDescriptor> styles = StylesOf(
                CHIP + ".action {\r\n    @include chip\r\n    background: #4C6EF5\r\n}", "action");

            Assert.AreEqual(6, Resolved<CornerRadiusStyleDescriptor>(styles).TopLeft);
            Assert.AreEqual(TextAlign.Center, Resolved<TextAlignStyleDescriptor>(styles).Horizontal);
            Assert.AreEqual("#4C6EF5", Resolved<BackgroundStyleDescriptor>(styles).Color);
        }

        [TestMethod]
        public void TheMixinItselfEmitsNoClass()
        {
            ClassesSet set = Compile(CHIP + ".action {\r\n    @include chip\r\n}");

            Assert.AreEqual(1, set.Classes.Count, "only the class that included it");
            Assert.AreEqual("action", set.Classes[0].Name);
        }

        [TestMethod]
        public void TwoSelectorsMayShareOneMixin()
        {
            ClassesSet set = Compile(
                CHIP
                + ".action {\r\n    @include chip\r\n    background: #4C6EF5\r\n}\r\n"
                + ".field {\r\n    @include chip\r\n    background: #2E3138\r\n}");

            Assert.AreEqual(6, Resolved<CornerRadiusStyleDescriptor>(
                set.Classes.Single(c => c.Name == "action").Styles).TopLeft);
            Assert.AreEqual(6, Resolved<CornerRadiusStyleDescriptor>(
                set.Classes.Single(c => c.Name == "field").Styles).TopLeft);
        }

        [TestMethod]
        public void ADeclarationAfterTheIncludeOverridesIt()
        {
            List<StyleDescriptor> styles = StylesOf(
                CHIP + ".action {\r\n    @include chip\r\n    corner-radius: 20px\r\n}", "action");

            Assert.AreEqual(20, Resolved<CornerRadiusStyleDescriptor>(styles).TopLeft,
                "the include is expanded in place, so what follows wins");
        }

        [TestMethod]
        public void ADeclarationBeforeTheIncludeIsOverriddenByIt()
        {
            List<StyleDescriptor> styles = StylesOf(
                CHIP + ".action {\r\n    corner-radius: 20px\r\n    @include chip\r\n}", "action");

            Assert.AreEqual(6, Resolved<CornerRadiusStyleDescriptor>(styles).TopLeft,
                "order is the only rule, in both directions");
        }

        [TestMethod]
        public void SeveralIncludesInOneBlockAllApply()
        {
            List<StyleDescriptor> styles = StylesOf(
                CHIP
                + "@mixin soft {\r\n    color: #E8ECF5\r\n}\r\n"
                + ".action {\r\n    @include chip\r\n    @include soft\r\n}", "action");

            Assert.AreEqual(6, Resolved<CornerRadiusStyleDescriptor>(styles).TopLeft);
            Assert.AreEqual("#E8ECF5", Resolved<ColorStyleDescriptor>(styles).Value);
        }

        [TestMethod]
        public void AMixinMayIncludeAnother()
        {
            List<StyleDescriptor> styles = StylesOf(
                CHIP
                + "@mixin button {\r\n    @include chip\r\n    height: 44px\r\n}\r\n"
                + ".action {\r\n    @include button\r\n}", "action");

            Assert.AreEqual(6, Resolved<CornerRadiusStyleDescriptor>(styles).TopLeft);
            Assert.AreEqual(44, Resolved<HeightStyleDescriptor>(styles).Value);
        }

        [TestMethod]
        public void DeclarationOrderOfTheMixinDoesNotMatter()
        {
            List<StyleDescriptor> styles = StylesOf(
                ".action {\r\n    @include chip\r\n}\r\n" + CHIP, "action");

            Assert.AreEqual(6, Resolved<CornerRadiusStyleDescriptor>(styles).TopLeft,
                "mixins are collected before anything is compiled");
        }

        [TestMethod]
        public void AMixinWorksInsideAScopeAndInsideAMediaBlock()
        {
            ClassesSet set = Compile(
                CHIP
                + "panel {\r\n    action {\r\n        @include chip\r\n    }\r\n}\r\n"
                + "@media (max-width: 600px) {\r\n    tight {\r\n        @include chip\r\n    }\r\n}");

            StyleClass scoped = set.Classes.Single(c => c.Name == "action");
            StyleClass inMedia = set.Classes.Single(c => c.Name == "tight");

            Assert.AreEqual("panel", scoped.Scope);
            Assert.AreEqual(6, Resolved<CornerRadiusStyleDescriptor>(scoped.Styles).TopLeft);

            Assert.IsNotNull(inMedia.Media);
            Assert.AreEqual(6, Resolved<CornerRadiusStyleDescriptor>(inMedia.Styles).TopLeft);
        }

        [TestMethod]
        public void AMixinBodyGoesThroughVariablesAndCalc()
        {
            List<StyleDescriptor> styles = StylesOf(
                "$gap: 8px\r\n"
                + "@mixin spaced {\r\n    height: calc($gap * 3)\r\n}\r\n"
                + ".action {\r\n    @include spaced\r\n}", "action");

            Assert.AreEqual(24, Resolved<HeightStyleDescriptor>(styles).Value);
        }

        [TestMethod]
        public void AnUndeclaredMixinIsReported()
        {
            AssertRejected(".action {\r\n    @include missing\r\n}", "not a declared mixin");
        }

        [TestMethod]
        public void ASelfIncludeIsReportedRatherThanLooping()
        {
            AssertRejected(
                "@mixin loop {\r\n    @include loop\r\n}\r\n.action {\r\n    @include loop\r\n}",
                "includes itself");
        }

        [TestMethod]
        public void AMutualIncludeIsReportedRatherThanLooping()
        {
            AssertRejected(
                "@mixin a {\r\n    @include b\r\n}\r\n@mixin b {\r\n    @include a\r\n}\r\n"
                + ".action {\r\n    @include a\r\n}",
                "includes itself");
        }

        [TestMethod]
        public void AMixinHoldingSelectorsIsReported()
        {
            AssertRejected(
                "@mixin card {\r\n    background: #FF0000\r\n    label {\r\n        color: #FFFFFF\r\n    }\r\n}\r\n"
                + ".action {\r\n    @include card\r\n}",
                "only hold declarations");
        }

        [TestMethod]
        public void ANestedMixinDeclarationIsReported()
        {
            AssertRejected(
                "box {\r\n    @mixin chip {\r\n        corner-radius: 6px\r\n    }\r\n}",
                "top level");
        }

        [TestMethod]
        public void AnIncludeAtTopLevelIsASyntaxError()
        {
            var source = new XnsSource("@include chip");
            source.Compile();

            Assert.IsTrue(source.HasErrors, "there is no block for it to apply to");
            Assert.AreEqual(LanguageErrorCode.SYNTAX, source.Diagnostics[0].Code);
        }

        [TestMethod]
        public void ASheetWithoutMixinsIsUnaffected()
        {
            List<StyleDescriptor> styles = StylesOf("box {\r\n    corner-radius: 4px\r\n}", "box");

            Assert.AreEqual(4, Resolved<CornerRadiusStyleDescriptor>(styles).TopLeft);
        }
    }
}
