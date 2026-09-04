using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class FieldErrorTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private TextField _field;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _field = new TextField { Name = "name", Label = "your name" };
            _field.Styles.Height = new HeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 30
            };

            _root.AddChild(_field);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private AccessibleNode Node()
            => _surface.BuildAccessibilityTree().Children[0];

        [TestMethod]
        public void AMessageMarksTheFieldInvalid()
        {
            Assert.IsFalse(_field.Invalid);

            _field.Error = "we need a name";

            Assert.IsTrue(_field.Invalid,
                "one binding has to do the whole job, or an author can set the message and forget "
                + "the state that styles it");
        }

        [TestMethod]
        public void TheStateIsWhatAStylesheetSees()
        {
            _field.Error = "we need a name";

            Assert.IsTrue(_field.HasState("invalid"),
                "the framework maintains the state, so #TextField:invalid costs no C#");

            _field.Error = null;

            Assert.IsFalse(_field.HasState("invalid"),
                "and clearing the message takes the state away with it");
        }

        [TestMethod]
        public void ClearingTheMessageClearsTheState()
        {
            _field.Error = "we need a name";
            _field.Error = string.Empty;

            Assert.IsFalse(_field.Invalid,
                "an empty message is no message - otherwise a field bound to a computed string "
                + "would stay red once it had been wrong");
        }

        [TestMethod]
        public void TheStateCanBeSetWithNoMessage()
        {
            _field.Invalid = true;

            Assert.IsTrue(_field.HasState("invalid"));
            Assert.IsNull(_field.Error,
                "a control that knows it is wrong but has nothing to say still gets the state");
        }

        [TestMethod]
        public void AScreenReaderIsToldItIsInvalid()
        {
            _field.Error = "we need a name";
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsTrue(Node().HasState(AccessibleStates.Invalid),
                "a red border says nothing to somebody who cannot see it");
        }

        [TestMethod]
        public void TheMessageIsWhatIsAnnounced()
        {
            _field.Description = "as it appears on your passport";
            _field.Error = "we need a name";
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("we need a name", Node().Description,
                "the error is the thing worth hearing while it stands, so it takes the "
                + "description's place rather than queueing behind the hint");
        }

        [TestMethod]
        public void TheHintComesBackWhenTheErrorGoes()
        {
            _field.Description = "as it appears on your passport";
            _field.Error = "we need a name";
            _field.Error = null;

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("as it appears on your passport", Node().Description,
                "the error borrows the description rather than replacing it");
        }

        [TestMethod]
        public void AFieldWithNothingWrongSaysNothing()
        {
            Assert.IsNull(Node().Description);
            Assert.IsFalse(Node().HasState(AccessibleStates.Invalid));
        }

        [TestMethod]
        public void TheSameMessageTwiceDoesNotDirtyTheTree()
        {
            _field.Error = "we need a name";
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _field.Error = "we need a name";
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(_surface.LastLayoutRan,
                "a two-way binding replays the assignment on every keystroke, so an equal value "
                + "must not restyle the tree - AddState is what guarantees it, by refusing a "
                + "state it already holds before it invalidates anything");
        }

        [TestMethod]
        public void AStylesheetCanPaintTheError()
        {
            var xns = new Ixen.Core.Language.Xns.XnsSource(
                ".field { border: #333333 1px inner }"
                + " .field:invalid { border: #FF0000 2px inner }");

            ClassesSet set = xns.Compile();

            Assert.IsFalse(xns.HasErrors);

            var registry = new StyleRegistry();

            registry.Add(set);

            _field.AddClass("field");

            _surface.Styles = registry;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1f, _field.BorderInsideTop,
                "the plain rule is what applies while nothing is wrong");

            _field.Error = "we need a name";
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(2f, _field.BorderInsideTop,
                "and this is the path that matters: an author writes :invalid in a stylesheet "
                + "and the framework maintains the state, exactly as it does for :focus");
        }

        [TestMethod]
        public void AnyElementCanCarryIt()
        {
            var group = new VisualElement { Name = "group", Role = AccessibleRole.Group };

            _root.AddChild(group);
            group.Error = "two of these are wrong";

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            AccessibleNode node = _surface.BuildAccessibilityTree().Children[1];

            Assert.IsTrue(node.HasState(AccessibleStates.Invalid),
                "a fieldset can be in error as well as a field, which is why this sits on "
                + "VisualElement rather than on TextField");
        }
    }
}
