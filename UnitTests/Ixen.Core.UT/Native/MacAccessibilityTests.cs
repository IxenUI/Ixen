using Ixen.Core.Accessibility;
using Ixen.Platform.Mac.Accessibility;
using Ixen.Platform.Mac.NativeApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Ixen.Core.UT.Native
{
    [TestClass]
    public class MacAccessibilityTests
    {
        private static AccessibleNode Node(AccessibleRole role = AccessibleRole.Button)
        {
            return new AccessibleNode
            {
                Role = role,
                Name = "Save",
                Value = "written",
                Description = "keeps the document",
                Shortcut = "Cmd+S",
                States = AccessibleStates.Focusable,
                Actions = AccessibleActions.Invoke,
                X = 10,
                Y = 20,
                Width = 30,
                Height = 40
            };
        }

        [TestMethod]
        public void TheShortcutRidesInTheHelpBecauseMacOSHasNowhereElseToPutIt()
        {
            Assert.AreEqual("keeps the document (Cmd+S)", MacRoles.HelpOf(Node()));
        }

        [TestMethod]
        public void AShortcutWithNoDescriptionIsTheWholeHelp()
        {
            AccessibleNode node = Node();

            node.Description = null;

            Assert.AreEqual("Cmd+S", MacRoles.HelpOf(node));
        }

        [TestMethod]
        public void ADescriptionWithNoShortcutIsLeftExactlyAsItIs()
        {
            AccessibleNode node = Node();

            node.Shortcut = null;

            Assert.AreEqual("keeps the document", MacRoles.HelpOf(node));
        }

        [TestMethod]
        public void ACheckBoxReadsItsToggleFromTheTick()
        {
            AccessibleNode node = Node(AccessibleRole.CheckBox);

            Assert.AreEqual(0, MacRoles.ToggleOf(node));

            node.States |= AccessibleStates.Checked;

            Assert.AreEqual(1, MacRoles.ToggleOf(node));
        }

        [TestMethod]
        public void ATabReadsItsToggleFromTheSelectionInstead()
        {
            AccessibleNode node = Node(AccessibleRole.Tab);

            node.States |= AccessibleStates.Checked;

            Assert.AreEqual(0, MacRoles.ToggleOf(node),
                "a tab is an AXRadioButton whose value is whether it is the chosen one, and "
                    + "nothing in the framework ever ticks a tab");

            node.States = AccessibleStates.Selected;

            Assert.AreEqual(1, MacRoles.ToggleOf(node));
        }

        [TestMethod]
        public void EverythingElseCarriesNoToggleAtAll()
        {
            AccessibleNode node = Node();

            node.States |= AccessibleStates.Checked | AccessibleStates.Selected;

            Assert.AreEqual(MacRoles.NOT_A_TOGGLE, MacRoles.ToggleOf(node),
                "a node with no toggle answers with its text, so a number here would be read "
                    + "out in place of the value");
        }

        [TestMethod]
        public void AnAnnouncementPrefersTheHalfThatActuallyMoved()
        {
            AccessibleNode before = Node();
            AccessibleNode after = Node();

            after.Value = "saved";

            var change = new AccessibilityChange { Previous = before, Current = after };

            Assert.AreEqual("saved", MacAccessibility.Spoken(change));
        }

        [TestMethod]
        public void AnAnnouncementFallsBackToTheNameWhenTheValueDidNotMove()
        {
            AccessibleNode before = Node();
            AccessibleNode after = Node();

            after.Name = "Saved";

            var change = new AccessibilityChange { Previous = before, Current = after };

            Assert.AreEqual("Saved", MacAccessibility.Spoken(change));
        }

        [TestMethod]
        public void EveryFieldThatTravelsIsInTheComparison()
        {
            var mutations = new Dictionary<string, Action<AccessibleNode>>
            {
                { "role", node => node.Role = AccessibleRole.Slider },
                { "states", node => node.States = AccessibleStates.Disabled },
                { "actions", node => node.Actions = AccessibleActions.Focus },
                { "name", node => node.Name = "Open" },
                { "value", node => node.Value = "other" },
                { "description", node => node.Description = "other" },
                { "shortcut", node => node.Shortcut = "Cmd+O" },
                { "x", node => node.X = 99 },
                { "y", node => node.Y = 99 },
                { "width", node => node.Width = 99 },
                { "height", node => node.Height = 99 }
            };

            var reference = new MacAccessibleNode(3, Node(), 1f);

            Assert.IsTrue(reference.SameAs(new MacAccessibleNode(3, Node(), 1f)),
                "an unchanged node has to compare equal, or every frame pushes everything");

            Assert.IsFalse(reference.SameAs(new MacAccessibleNode(4, Node(), 1f)),
                "the parent is what Commit rebuilds the tree from");

            Assert.IsFalse(reference.SameAs(new MacAccessibleNode(3, Node(), 2f)),
                "the scale turns logical units into the device pixels the frame is built from");

            foreach (KeyValuePair<string, Action<AccessibleNode>> mutation in mutations)
            {
                AccessibleNode node = Node();

                mutation.Value(node);

                Assert.IsFalse(reference.SameAs(new MacAccessibleNode(3, node, 1f)),
                    $"a change of {mutation.Key} would never be pushed, so VoiceOver would go "
                        + "on reading whatever was there before");
            }
        }
    }
}
