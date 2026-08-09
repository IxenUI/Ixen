using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class StyleScopeTests
    {
        private static StyleClass CompiledClass(string source, string name)
        {
            var xnsSource = new XnsSource(source);
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(e => e.Message)));

            return set.Classes.Single(c => c.Name == name);
        }

        private static VisualElement Named(string name, params VisualElement[] children)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            foreach (VisualElement child in children)
            {
                element.AddChild(child);
            }

            return element;
        }

        private static string RuntimeScope(VisualElement element)
            => StyleScope.Build(element, e => e.Parent, e => e.Name);

        [TestMethod]
        public void TopLevelClass_HasNoScope()
        {
            Assert.IsNull(CompiledClass("container { width: 10px }", "container").Scope);
        }

        [TestMethod]
        public void OneLevelOfNesting_ScopesToTheParentName()
        {
            Assert.AreEqual("container", CompiledClass("container { panel { width: 10px } }", "panel").Scope);
        }

        [TestMethod]
        public void TwoLevelsOfNesting_JoinAncestorsWithASeparator()
        {
            StyleClass inner = CompiledClass("container { panel { entry { width: 10px } } }", "entry");

            Assert.AreEqual("container" + StyleScope.SEPARATOR + "panel", inner.Scope);
        }

        [TestMethod]
        public void DifferentNestingsThatConcatenateIdentically_ProduceDifferentScopes()
        {
            string first = CompiledClass("ab { c { leaf { width: 10px } } }", "leaf").Scope;
            string second = CompiledClass("a { bc { leaf { width: 10px } } }", "leaf").Scope;

            Assert.AreNotEqual(first, second);
            Assert.AreEqual("ab/c", first);
            Assert.AreEqual("a/bc", second);
        }

        [TestMethod]
        public void RuntimeScope_MatchesTheCompiledScope()
        {
            string compiled = CompiledClass("container { panel { entry { width: 10px } } }", "entry").Scope;

            VisualElement entry = Named("entry");
            Named("container", Named("panel", entry));

            Assert.AreEqual(compiled, RuntimeScope(entry));
        }

        [TestMethod]
        public void RuntimeScope_OfARootChild_IsTheRootName()
        {
            VisualElement child = Named("child");
            Named("root", child);

            Assert.AreEqual("root", RuntimeScope(child));
        }

        [TestMethod]
        public void RuntimeScope_OfTheRootItself_IsNull()
        {
            VisualElement root = Named("root", Named("child"));

            Assert.IsNull(RuntimeScope(root));
        }

        [TestMethod]
        public void UnnamedAncestors_AreSkippedRatherThanRepresented()
        {
            VisualElement leaf = Named("leaf");
            var anonymous = new VisualElement();
            anonymous.AddChild(leaf);
            Named("root", anonymous);

            Assert.AreEqual("root", RuntimeScope(leaf));
        }

        [TestMethod]
        public void NestingUnderAClassSelector_IsReportedAsAnUnmatchableScope()
        {
            var xnsSource = new XnsSource("container {\r\n    .card {\r\n        inner { width: 1px }\r\n    }\r\n}");
            xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, "an unmatchable scope is a warning, not an error");

            LanguageError warning = xnsSource.Diagnostics.Single(d => d.Code == LanguageErrorCode.UNSUPPORTED_SCOPE);
            Assert.AreEqual(LanguageErrorSeverity.Warning, warning.Severity);
            Assert.IsTrue(warning.Message.Contains(".card"), warning.Message);
        }

        [TestMethod]
        public void NestingUnderATypeSelector_IsReportedAsAnUnmatchableScope()
        {
            var xnsSource = new XnsSource("container {\r\n    #entries {\r\n        inner { width: 1px }\r\n    }\r\n}");
            xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors);
            Assert.AreEqual(1, xnsSource.Diagnostics.Count(d => d.Code == LanguageErrorCode.UNSUPPORTED_SCOPE));
        }

        [TestMethod]
        public void NestingUnderElementNamesOnly_ReportsNothing()
        {
            var xnsSource = new XnsSource("container {\r\n    panel {\r\n        inner { width: 1px }\r\n    }\r\n}");
            xnsSource.Compile();

            Assert.AreEqual(0, xnsSource.Diagnostics.Count, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
        }

        [TestMethod]
        public void ScopedClass_AppliesOnlyToTheMatchingNesting()
        {
            var registry = new StyleRegistry();
            registry.Add(new XnsSource(@"container {
    panel {
        target {
            width: 123px
        }
    }
}").Compile());

            VisualElement matching = Named("target");
            var matchingRoot = Named("container", Named("panel", matching));

            VisualElement wrongNesting = Named("target");
            var wrongRoot = Named("container", Named("sidebar", wrongNesting));

            new IxenSurface(matchingRoot) { Styles = registry }.ComputeLayout(400, 400);
            new IxenSurface(wrongRoot) { Styles = registry }.ComputeLayout(400, 400);

            Assert.AreEqual(123, matching.Width, "matching nesting should receive the scoped width");
            Assert.AreNotEqual(123, wrongNesting.Width, "a different nesting must not match");
        }
    }
}
