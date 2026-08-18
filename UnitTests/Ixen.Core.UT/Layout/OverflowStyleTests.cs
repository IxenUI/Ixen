using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class OverflowStyleTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private VisualElement _list;
        private IxenSurface _surface;
        private StyleRegistry _registry;

        [TestInitialize]
        public void Setup()
        {
            _registry = new StyleRegistry();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _list = new VisualElement { Name = "list" };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            for (int index = 0; index < 6; index++)
            {
                var row = new VisualElement();
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
                _list.AddChild(row);
            }

            _root.AddChild(_list);

            _surface = new IxenSurface(_root) { Styles = _registry };
        }

        private void Overflow(OverflowKind kind)
        {
            _list.Styles.Overflow = new OverflowStyleDescriptor { Value = kind };
            _list.Invalidate();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void ScrollMakesTheElementScrollable()
        {
            Overflow(OverflowKind.Scroll);
            Layout();

            Assert.IsTrue(_list.Scrollable, "the style writes the behaviour");
            Assert.IsTrue(_list.MaxScrollY > 0, "and the content really can move");
        }

        [TestMethod]
        public void HiddenTurnsItOff()
        {
            _list.Scrollable = true;
            Layout();

            Assert.IsTrue(_list.Scrollable);

            Overflow(OverflowKind.Hidden);
            Layout();

            Assert.IsFalse(_list.Scrollable, "a stylesheet can take it away again");
            Assert.AreEqual(0f, _list.MaxScrollY);
        }

        [TestMethod]
        public void UnsetLeavesWhateverXnlOrCodeAsked()
        {
            _list.Scrollable = true;
            Layout();

            Assert.IsTrue(_list.Scrollable);

            Overflow(OverflowKind.Unset);
            Layout();

            Assert.IsTrue(_list.Scrollable,
                "an absent overflow must not silently undo scrollable: \"true\" from XNL");
        }

        [TestMethod]
        public void TheScrollbarsAreStyledInTheSamePassThatCreatesThem()
        {
            _registry.Add(new StyleClass(StyleClassTarget.ElementType, null, null, "Scrollbar",
                new System.Collections.Generic.List<StyleDescriptor>
                {
                    new BackgroundStyleDescriptor { Color = "#FF0000" }
                }));

            Overflow(OverflowKind.Scroll);
            Layout();

            VisualElement bar = _list.Chrome.First(c => c is Scrollbar);

            Assert.AreEqual("#FF0000", bar.StylesHandlers.Background.Descriptor.Color,
                "the pass resolves an element before walking its chrome, so a bar born during "
                + "the cascade is styled before it is ever measured");
        }

        [TestMethod]
        public void ItSettlesAfterOneExtraPass()
        {
            Overflow(OverflowKind.Scroll);
            Layout();

            Layout();

            Assert.IsFalse(_root.IsLayoutDirty,
                "writing Scrollable invalidates the layout, but only on the pass that changes it");
        }

        [TestMethod]
        public void ItComesFromXnsEndToEnd()
        {
            var source = new XnsSource(@"list {
    height: 40px
    overflow: scroll
}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();
            Layout();

            Assert.IsTrue(_list.Scrollable, "declared in a stylesheet, resolved through the cascade");
        }

        [TestMethod]
        public void AStylesheetOverridesTheXnlValue()
        {
            _list.Scrollable = true;

            var source = new XnsSource("list { height: 40px  overflow: hidden }");
            var registry = new StyleRegistry();
            registry.Add(source.Compile());

            _surface.Styles = registry;
            _root.Invalidate();
            Layout();

            Assert.IsFalse(_list.Scrollable,
                "the cascade beats an inline value, which is the rule everywhere else too");
        }

        [TestMethod]
        public void TheKeywordsParse()
        {
            Assert.AreEqual(OverflowKind.Scroll, Parsed("scroll"));
            Assert.AreEqual(OverflowKind.Scroll, Parsed("auto"));
            Assert.AreEqual(OverflowKind.Hidden, Parsed("hidden"));

            Assert.IsFalse(new OverflowStyleParser("visible").IsValid,
                "there is no visible: an element always clips its children");
            Assert.IsFalse(new OverflowStyleParser("").IsValid);
        }

        private static OverflowKind Parsed(string value)
        {
            var parser = new OverflowStyleParser(value);

            Assert.IsTrue(parser.IsValid, $"'{value}' should parse");

            return parser.Descriptor.Value;
        }
    }
}
