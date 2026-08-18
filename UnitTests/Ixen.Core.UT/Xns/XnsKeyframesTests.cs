using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsKeyframesTests
    {
        private static ClassesSet Compile(string source, out XnsSource xnsSource)
        {
            xnsSource = new XnsSource(source);
            return xnsSource.Compile();
        }

        private static ClassesSet Valid(string source)
        {
            ClassesSet set = Compile(source, out XnsSource xnsSource);

            Assert.IsFalse(xnsSource.HasErrors,
                string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return set;
        }

        [TestMethod]
        public void AKeyframesBlockIsCompiledApartFromTheClasses()
        {
            ClassesSet set = Valid(@"@keyframes pulse {
    0%   { background: #4C6EF5 }
    100% { background: #FF922B }
}

puck {
    animation: pulse 600ms
}");

            Assert.AreEqual(1, set.Keyframes.Count, "the set is not a style class");
            Assert.AreEqual("pulse", set.Keyframes[0].Name, "the marker is not part of the name");
            Assert.AreEqual(2, set.Keyframes[0].Frames.Count);

            Assert.AreEqual(1, set.Classes.Count, "and the element rule is still a class");
            Assert.AreEqual("puck", set.Classes[0].Name);
        }

        [TestMethod]
        public void OffsetsBecomeFractions()
        {
            ClassesSet set = Valid(@"@keyframes slide {
    0%   { left: 0px }
    25%  { left: 10px }
    100% { left: 40px }
}");

            List<Keyframe> frames = set.Keyframes[0].Frames;

            Assert.AreEqual(0f, frames[0].Offset);
            Assert.AreEqual(0.25f, frames[1].Offset);
            Assert.AreEqual(1f, frames[2].Offset);
        }

        [TestMethod]
        public void FromAndToAreTheEnds()
        {
            ClassesSet set = Valid(@"@keyframes fade {
    from { background: #000000 }
    to   { background: #FFFFFF }
}");

            List<Keyframe> frames = set.Keyframes[0].Frames;

            Assert.AreEqual(0f, frames[0].Offset);
            Assert.AreEqual(1f, frames[1].Offset);
        }

        [TestMethod]
        public void FramesAreSortedByOffsetWhateverTheDeclarationOrder()
        {
            ClassesSet set = Valid(@"@keyframes pulse {
    100% { background: #000000 }
    0%   { background: #FFFFFF }
    50%  { background: #FF0000 }
}");

            KeyframesSet keyframes = set.Keyframes[0];

            Assert.AreEqual(1, keyframes.Properties.Count,
                "reading the tracks is what sorts the frames");

            Assert.AreEqual(0f, keyframes.Frames[0].Offset);
            Assert.AreEqual(0.5f, keyframes.Frames[1].Offset);
            Assert.AreEqual(1f, keyframes.Frames[2].Offset);
        }

        [TestMethod]
        public void AStopStyleIsParsedLikeAnyOther()
        {
            ClassesSet set = Valid(@"@keyframes grow {
    from { width: 10px }
    to   { width: 50% }
}");

            var from = (SizeStyleDescriptor)set.Keyframes[0].Frames[0].Styles[0];
            var to = (SizeStyleDescriptor)set.Keyframes[0].Frames[1].Styles[0];

            Assert.AreEqual(SizeUnit.Pixels, from.Unit);
            Assert.AreEqual(10f, from.Value);
            Assert.AreEqual(SizeUnit.Percents, to.Unit);
            Assert.AreEqual(50f, to.Value);
        }

        [TestMethod]
        public void APropertyDeclaredOnceIsNotATrack()
        {
            ClassesSet set = Valid(@"@keyframes half {
    0%   { background: #000000  width: 10px }
    100% { background: #FFFFFF }
}");

            IReadOnlyList<string> properties = set.Keyframes[0].Properties;

            Assert.IsTrue(properties.Contains(StyleIdentifier.BACKGROUND),
                "two stops make a track");

            Assert.IsFalse(properties.Contains(StyleIdentifier.WIDTH),
                "one stop has nothing to interpolate towards");
        }

        [TestMethod]
        public void OnlyAnimatablePropertiesBecomeTracks()
        {
            ClassesSet set = Valid(@"@keyframes odd {
    0%   { padding: 2px  background: #000000 }
    100% { padding: 8px  background: #FFFFFF }
}");

            IReadOnlyList<string> properties = set.Keyframes[0].Properties;

            CollectionAssert.AreEquivalent(new[] { StyleIdentifier.BACKGROUND }, properties.ToList(),
                "padding is not one of the nine, so it is silently not animated");
        }

        [TestMethod]
        public void ABadOffsetIsReported()
        {
            Compile(@"@keyframes pulse {
    middle { background: #000000 }
}", out XnsSource source);

            Assert.IsTrue(source.HasErrors);
            Assert.IsTrue(source.Diagnostics[0].Message.Contains("middle"),
                source.Diagnostics[0].Message);
        }

        [TestMethod]
        public void AnOffsetPastAHundredIsReported()
        {
            Compile(@"@keyframes pulse {
    150% { background: #000000 }
}", out XnsSource source);

            Assert.IsTrue(source.HasErrors);
        }

        [TestMethod]
        public void AStyleDirectlyInTheBlockIsReported()
        {
            Compile(@"@keyframes pulse {
    background: #000000
}", out XnsSource source);

            Assert.IsTrue(source.HasErrors);
            Assert.IsTrue(source.Diagnostics[0].Message.Contains("offsets"),
                source.Diagnostics[0].Message);
        }

        [TestMethod]
        public void ANestedKeyframesBlockIsReported()
        {
            Compile(@"container {
    @keyframes pulse {
        0% { background: #000000 }
    }
}", out XnsSource source);

            Assert.IsTrue(source.HasErrors);
            Assert.IsTrue(source.Diagnostics[0].Message.Contains("top level"),
                source.Diagnostics[0].Message);
        }

        [TestMethod]
        public void AnUnknownAtRuleIsASyntaxError()
        {
            Compile(@"@media screen {
    container { layout: row }
}", out XnsSource source);

            Assert.IsTrue(source.HasErrors, "keyframes is the only at-rule XNS knows");
        }

        [TestMethod]
        public void TheMarkerIsNotPartOfTheColouredSpan()
        {
            var tokenizer = new XnsSource(@"@keyframes pulse {
    0% { background: #000000 }
}");
            tokenizer.Tokenize();

            var name = tokenizer.GetTokens().First(t => t.Type == XnsTokenType.ClassName);

            Assert.AreEqual("@pulse", name.Content, "the content is the semantic name");
            Assert.AreEqual("@keyframes pulse".Length, name.Length,
                "the length is the source span, so colouring covers the whole header");
        }

        [TestMethod]
        public void AStopIsAnOrdinarySelectorToken()
        {
            var source = new XnsSource(@"@keyframes pulse {
    50% { background: #000000 }
}");
            source.Tokenize();

            List<XnsToken> names = source.GetTokens()
                .Where(t => t.Type == XnsTokenType.ClassName)
                .ToList();

            Assert.AreEqual(2, names.Count);
            Assert.AreEqual("50%", names[1].Content);
        }

        [TestMethod]
        public void AnUnderscoreIsLegalInAStyleValue()
        {
            ClassesSet set = Valid(@"@keyframes my_pulse {
    from { background: #000000 }
    to   { background: #FFFFFF }
}

puck {
    animation: my_pulse 400ms
}");

            Assert.AreEqual("my_pulse", set.Keyframes[0].Name);

            var animation = (AnimationStyleDescriptor)set.Classes[0].Styles[0];

            Assert.AreEqual("my_pulse", animation.Name,
                "an identifier is legal in a selector, so it must be legal in a value that names one");
        }

        [TestMethod]
        public void AValueMayStartWithAnUnderscore()
        {
            ClassesSet set = Valid(@"puck { animation: _pulse 400ms }");

            Assert.AreEqual("_pulse", ((AnimationStyleDescriptor)set.Classes[0].Styles[0]).Name,
                "the first character and the continuation are two different sets; both must accept it");
        }

        [TestMethod]
        public void AStyleWhoseValueEndsInAPercentStillTokenizes()
        {
            ClassesSet set = Valid(@"container {
    width: 50%
    height: 100%
}");

            Assert.AreEqual(1, set.Classes.Count,
                "adding % to the selector set must not disturb a percentage value");
        }
    }
}
