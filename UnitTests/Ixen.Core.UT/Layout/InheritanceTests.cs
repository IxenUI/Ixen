using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class InheritanceTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private VisualElement _card;
        private VisualElement _label;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _card = new VisualElement { Name = "card" };
            _label = new VisualElement { Name = "label", Text = "hello" };

            _card.AddChild(_label);
            _root.AddChild(_card);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private FontSpec Spec(VisualElement element) => FontSpec.From(element.StylesHandlers);

        [TestMethod]
        public void ColourAndFontDescendFromAnAncestor()
        {
            _card.Styles.Color = new ColorStyleDescriptor { Value = "#FF00FF00" };
            _card.Styles.FontFamily = new FontFamilyStyleDescriptor { Value = "Verdana" };
            _card.Styles.FontSize = new FontSizeStyleDescriptor { Value = 22 };
            _card.Styles.FontWeight = new FontWeightStyleDescriptor { Value = FontWeight.Bold };
            _card.Styles.FontStyle = new FontStyleStyleDescriptor { Value = FontStyle.Italic };
            _card.Invalidate();

            Layout();

            Assert.AreEqual(new Color("#FF00FF00"), _label.StylesHandlers.Color.Brush.Color);
            Assert.AreEqual("Verdana", Spec(_label).Family);
            Assert.AreEqual(22, Spec(_label).Size);
            Assert.IsTrue(Spec(_label).Bold);
            Assert.IsTrue(Spec(_label).Italic);
        }

        [TestMethod]
        public void ItCrossesSeveralLevels()
        {
            var deep = new VisualElement { Name = "deep", Text = "x" };
            _label.AddChild(deep);

            _card.Styles.FontSize = new FontSizeStyleDescriptor { Value = 30 };
            _card.Invalidate();

            Layout();

            Assert.AreEqual(30, Spec(deep).Size, "it is not one level, it is the chain");
        }

        [TestMethod]
        public void AnOwnValueWins()
        {
            _card.Styles.FontSize = new FontSizeStyleDescriptor { Value = 30 };
            _label.Styles.FontSize = new FontSizeStyleDescriptor { Value = 11 };
            _root.Invalidate();

            Layout();

            Assert.AreEqual(11, Spec(_label).Size);
            Assert.AreEqual(30, Spec(_card).Size, "and the ancestor keeps its own");
        }

        [TestMethod]
        public void AClassCanBeWhatIsInherited()
        {
            var registry = new StyleRegistry();
            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "card", new()
            {
                new ColorStyleDescriptor { Value = "#FF123456" }
            }));

            _surface.Styles = registry;
            _root.Invalidate();

            Layout();

            Assert.AreEqual(new Color("#FF123456"), _label.StylesHandlers.Color.Brush.Color,
                "what is inherited is the resolved value, not the inline one");
        }

        [TestMethod]
        public void AClassOnTheChildStillWins()
        {
            var registry = new StyleRegistry();
            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "card", new()
            {
                new ColorStyleDescriptor { Value = "#FF123456" }
            }));

            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "label", new()
            {
                new ColorStyleDescriptor { Value = "#FFAABBCC" }
            }));

            _surface.Styles = registry;
            _root.Invalidate();

            Layout();

            Assert.AreEqual(new Color("#FFAABBCC"), _label.StylesHandlers.Color.Brush.Color);
        }

        [TestMethod]
        public void ChangingTheAncestorUpdatesTheDescendant()
        {
            _card.Styles.FontSize = new FontSizeStyleDescriptor { Value = 20 };
            _card.Invalidate();
            Layout();

            Assert.AreEqual(20, Spec(_label).Size);

            _card.Styles.FontSize = new FontSizeStyleDescriptor { Value = 40 };
            _card.Invalidate();
            Layout();

            Assert.AreEqual(40, Spec(_label).Size, "a stale inherited handler would have kept 20");
        }

        [TestMethod]
        public void WhatIsNotInheritableStaysPut()
        {
            _card.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF00FF00" };
            _card.Styles.Padding = new PaddingStyleDescriptor();
            _card.Invalidate();

            Layout();

            Assert.AreNotSame(_card.StylesHandlers.Background, _label.StylesHandlers.Background,
                "a background belongs to the element that declares it");
        }

        [TestMethod]
        public void AnUnstyledTreeStillGetsTheDefaults()
        {
            Assert.AreEqual(FontSizeStyleDescriptor.DEFAULT_SIZE, Spec(_label).Size);
            Assert.IsFalse(Spec(_label).Bold);
            Assert.AreEqual(new Color("#000000"), _label.StylesHandlers.Color.Brush.Color);
        }

        [TestMethod]
        public void AnInheritingElementDoesNotAllocateAHandlerPerPass()
        {
            _card.Styles.Color = new ColorStyleDescriptor { Value = "#FF00FF00" };
            _card.Invalidate();
            Layout();

            object first = _label.StylesHandlers.Color;

            _root.Invalidate();
            Layout();

            Assert.AreSame(first, _label.StylesHandlers.Color,
                "it points at the ancestor's handler rather than building its own");
        }
    }
}
