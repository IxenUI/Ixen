using Ixen.Core.Language.Xns;
using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class StyleTokensTests
    {
        private const int VIEWPORT = 200;

        private const string ACCENT = "#4C6EF5";
        private const string ORANGE = "#E8590C";

        private IxenSurface _surface;
        private VisualElement _box;
        private FakeScheduler _scheduler;

        private void Sheet(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            root.AddChild(_box);

            _scheduler = new FakeScheduler();

            _surface = new IxenSurface(root)
            {
                Styles = registry,
                Scheduler = _scheduler
            };

            root.Invalidate();
        }

        private SKColor Fill()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            using SKBitmap bitmap = _surface.RenderToBitmap();

            return bitmap.GetPixel(VIEWPORT / 2, VIEWPORT / 2);
        }

        private static string Hex(SKColor color)
            => new Color(color.Red, color.Green, color.Blue, color.Alpha).ToRGBHexColor();

        [TestMethod]
        public void ASheetLooksExactlyAsItDidUntilSomethingIsOverridden()
        {
            Sheet("$accent: " + ACCENT + "\r\nbox {\r\n    background: $accent\r\n}");

            Assert.AreEqual(ACCENT, Hex(Fill()),
                "the declared value is the token's fallback, so nothing moves on its own");
        }

        [TestMethod]
        public void SettingATokenRepaintsWhatUsesIt()
        {
            Sheet("$accent: " + ACCENT + "\r\nbox {\r\n    background: $accent\r\n}");

            Assert.AreEqual(ACCENT, Hex(Fill()));

            _surface.SetToken("accent", ORANGE);

            Assert.AreEqual(ORANGE, Hex(Fill()),
                "the handler carries the palette version, so a change rebuilds its brush");
        }

        [TestMethod]
        public void ResettingGoesBackToTheSheetsOwnColour()
        {
            Sheet("$accent: " + ACCENT + "\r\nbox {\r\n    background: $accent\r\n}");

            _surface.SetToken("accent", ORANGE);
            Assert.AreEqual(ORANGE, Hex(Fill()));

            _surface.ResetToken("accent");

            Assert.AreEqual(ACCENT, Hex(Fill()));
        }

        [TestMethod]
        public void ResetTokensGivesEveryOverrideBack()
        {
            Sheet("$accent: " + ACCENT + "\r\n$surface: #2E3138\r\n"
                + "box {\r\n    background: $accent\r\n}");

            _surface.SetToken("accent", ORANGE);
            _surface.SetToken("surface", "#101010");

            Assert.AreEqual(ORANGE, Hex(Fill()));

            _surface.ResetTokens();

            Assert.AreEqual(ACCENT, Hex(Fill()));
            Assert.AreEqual("#2E3138", _surface.Tokens.ValueOf("surface"));
        }

        [TestMethod]
        public void EveryColourVariableOfTheSheetIsAToken()
        {
            Sheet("$accent: " + ACCENT + "\r\n$surface: #2E3138\r\n$gutter: 14px\r\n"
                + "box {\r\n    background: $accent\r\n    margin: $gutter\r\n}");

            Assert.AreEqual(2, _surface.Tokens.Count,
                "a colour becomes a token and a length does not");
        }

        [TestMethod]
        public void ATokenThatIsNotAColourIsRefused()
        {
            Sheet("$accent: " + ACCENT + "\r\nbox {\r\n    background: $accent\r\n}");

            Assert.Throws<ArgumentException>(() => _surface.SetToken("accent", "14px"));
            Assert.Throws<ArgumentException>(() => _surface.SetToken("accent", "blue"));
            Assert.Throws<ArgumentException>(() => _surface.SetToken("accent", "$other"));
        }

        [TestMethod]
        public void AnUnknownTokenIsLeftAsItIsRatherThanThrowing()
        {
            Sheet("$accent: " + ACCENT + "\r\nbox {\r\n    background: $accent\r\n}");

            Assert.AreEqual("$nobody", _surface.Tokens.ValueOf("nobody"));
        }

        [TestMethod]
        public void SettingATokenNobodyDeclaredStillAnswers()
        {
            Sheet("$accent: " + ACCENT + "\r\nbox {\r\n    background: $accent\r\n}");

            _surface.SetToken("brand", ORANGE);

            Assert.AreEqual(ORANGE, _surface.Tokens.ValueOf("brand"),
                "an application may name a token its stylesheet never did");
        }

        [TestMethod]
        public void ABorderFollowsThePalette()
        {
            Sheet("$line: " + ACCENT + "\r\nbox {\r\n    border: $line 2px inner\r\n}");

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(ACCENT, _box.StylesHandlers.Border.Color.ToRGBHexColor());

            _surface.SetToken("line", ORANGE);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(ORANGE, _box.StylesHandlers.Border.Color.ToRGBHexColor());
        }

        [TestMethod]
        public void ATextColourFollowsThePalette()
        {
            Sheet("$ink: " + ACCENT + "\r\nbox {\r\n    color: $ink\r\n}");

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(ACCENT, _box.StylesHandlers.Color.Brush.Color.ToRGBHexColor());

            _surface.SetToken("ink", ORANGE);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(ORANGE, _box.StylesHandlers.Color.Brush.Color.ToRGBHexColor());
        }

        [TestMethod]
        public void AGradientStopFollowsThePalette()
        {
            Sheet("$accent: " + ACCENT + "\r\n"
                + "box {\r\n    background: linear-gradient(to bottom $accent $accent)\r\n}");

            Assert.AreEqual(ACCENT, Hex(Fill()),
                "a flat gradient of one token is that token everywhere");

            _surface.SetToken("accent", ORANGE);

            Assert.AreEqual(ORANGE, Hex(Fill()));
        }

        [TestMethod]
        public void AnAnimatedColourStopFollowsThePalette()
        {
            Sheet("$accent: " + ACCENT + "\r\n"
                + "box {\r\n    background: #FF0000\r\n    animation: token_fade 64ms infinite\r\n}\r\n"
                + "@keyframes token_fade {\r\n"
                + "    0%   { background: $accent }\r\n"
                + "    100% { background: $accent }\r\n"
                + "}");

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(ACCENT, Current().ToRGBHexColor(),
                "a keyframe stop naming a token resolves like any other colour");

            _surface.SetToken("accent", ORANGE);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(ORANGE, Current().ToRGBHexColor(),
                "and a running animation is re-applied when the palette moves");
        }

        private Color Current()
            => _box.Animations.For(StyleIdentifier.BACKGROUND).Current;

        [TestMethod]
        public void AnApplicationTokenBeatsAThemesOwn()
        {
            var registry = new StyleRegistry();

            registry.AddDefaults(Set("$surface: #F4F5F8\r\nbox {\r\n    background: $surface\r\n}"));
            registry.Add(Set("$surface: #2E3138\r\npanel {\r\n    background: $surface\r\n}"));

            Assert.AreEqual("#2E3138", registry.Tokens.ValueOf("surface"),
                "a palette is one namespace, so the two layers settle it the way rules do");
        }

        [TestMethod]
        public void AndTheOrderTheyAreRegisteredInDoesNotDecideIt()
        {
            var registry = new StyleRegistry();

            registry.Add(Set("$surface: #2E3138\r\npanel {\r\n    background: $surface\r\n}"));
            registry.AddDefaults(Set("$surface: #F4F5F8\r\nbox {\r\n    background: $surface\r\n}"));

            Assert.AreEqual("#2E3138", registry.Tokens.ValueOf("surface"),
                "nobody controls assembly load order, so last-wins would be a coin flip");
        }

        [TestMethod]
        public void AThemeTokenNobodyElseDeclaresStillResolves()
        {
            var registry = new StyleRegistry();

            registry.AddDefaults(Set("$control_focus: #4C6EF5\r\nbox {\r\n    background: $control_focus\r\n}"));

            Assert.AreEqual("#4C6EF5", registry.Tokens.ValueOf("control_focus"));
        }

        [TestMethod]
        public void AnOverrideBeatsBothLayers()
        {
            var registry = new StyleRegistry();

            registry.AddDefaults(Set("$surface: #F4F5F8\r\nbox {\r\n    background: $surface\r\n}"));
            registry.Add(Set("$surface: #2E3138\r\npanel {\r\n    background: $surface\r\n}"));

            registry.Tokens.Set("surface", "#E8590C");

            Assert.AreEqual("#E8590C", registry.Tokens.ValueOf("surface"));
        }

        private static ClassesSet Set(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }
    }
}
