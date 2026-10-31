using Ixen.Core.Accessibility;
using Ixen.Platform.Linux.Accessibility;
using Ixen.Platform.Linux.NativeApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Ixen.Core.UT.Native
{
    [TestClass]
    public class LinuxAccessibilityTests
    {
        private const int ROLE_CHECK_BOX = 7;
        private const int ROLE_FRAME = 23;
        private const int ROLE_PUSH_BUTTON = 43;
        private const int ROLE_PASSWORD_TEXT = 40;
        private const int ROLE_ENTRY = 79;
        private const int ROLE_UNKNOWN = 67;

        private const int STATE_ACTIVE = 1;
        private const int STATE_CHECKED = 4;
        private const int STATE_EDITABLE = 7;
        private const int STATE_ENABLED = 8;
        private const int STATE_EXPANDABLE = 9;
        private const int STATE_EXPANDED = 10;
        private const int STATE_FOCUSABLE = 11;
        private const int STATE_FOCUSED = 12;
        private const int STATE_MULTI_LINE = 17;
        private const int STATE_MULTISELECTABLE = 18;
        private const int STATE_SELECTABLE = 22;
        private const int STATE_SELECTED = 23;
        private const int STATE_SENSITIVE = 24;
        private const int STATE_SHOWING = 25;
        private const int STATE_SINGLE_LINE = 26;
        private const int STATE_VISIBLE = 30;
        private const int STATE_INVALID_ENTRY = 36;

        private static AccessibleNode Node(AccessibleRole role = AccessibleRole.Button)
        {
            return new AccessibleNode
            {
                Role = role,
                Name = "Save",
                Value = "written",
                Description = "keeps the document",
                Shortcut = "Ctrl+S",
                States = AccessibleStates.Focusable,
                Actions = AccessibleActions.Invoke,
                X = 10,
                Y = 20,
                Width = 30,
                Height = 40
            };
        }

        private static bool Has(long mask, int state) => (mask & (1L << state)) != 0;

        private static long StatesOf(AccessibleStates states, AccessibleActions actions
            = AccessibleActions.None)
        {
            AccessibleNode node = Node();

            node.States = states;
            node.Actions = actions;

            return LinuxRoles.StatesOf(node);
        }

        [TestMethod]
        public void TheRootIsTheWindowRatherThanWhateverRoleItDeclares()
        {
            Assert.AreEqual(ROLE_FRAME, LinuxRoles.RoleOf(Node(AccessibleRole.Group), true),
                "an AT-SPI application's child is a window, so the pushed root is a frame "
                    + "whatever the tree calls it");
        }

        [TestMethod]
        public void AnOrdinaryNodeKeepsItsOwnRole()
        {
            Assert.AreEqual(ROLE_PUSH_BUTTON, LinuxRoles.RoleOf(Node(), false));
            Assert.AreEqual(ROLE_CHECK_BOX, LinuxRoles.RoleOf(Node(AccessibleRole.CheckBox), false));
        }

        [TestMethod]
        public void ARoleWithNothingToSayIsUnknownRatherThanInvalid()
        {
            Assert.AreEqual(ROLE_UNKNOWN, LinuxRoles.ToNative(AccessibleRole.None),
                "ATSPI_ROLE_INVALID is what a dead object answers, and an exposed node is not one");
        }

        [TestMethod]
        public void AMaskedFieldIsAPasswordRoleRatherThanAState()
        {
            AccessibleNode node = Node(AccessibleRole.TextField);

            Assert.AreEqual(ROLE_ENTRY, LinuxRoles.RoleOf(node, false));

            node.States |= AccessibleStates.Protected;

            Assert.AreEqual(ROLE_PASSWORD_TEXT, LinuxRoles.RoleOf(node, false),
                "AT-SPI says a masked field by its role, where UIA and Android say it by a flag");
        }

        [TestMethod]
        public void EveryNodeIsVisibleAndAnOffscreenOneIsNotShowing()
        {
            long visible = StatesOf(AccessibleStates.None);

            Assert.IsTrue(Has(visible, STATE_VISIBLE));
            Assert.IsTrue(Has(visible, STATE_SHOWING));

            long offscreen = StatesOf(AccessibleStates.Offscreen);

            Assert.IsTrue(Has(offscreen, STATE_VISIBLE),
                "it is still part of the window, which is what VISIBLE means");

            Assert.IsFalse(Has(offscreen, STATE_SHOWING),
                "SHOWING is what says a screen reader could point at it");
        }

        [TestMethod]
        public void BeingEnabledIsTheAbsenceOfBeingDisabled()
        {
            long enabled = StatesOf(AccessibleStates.None);

            Assert.IsTrue(Has(enabled, STATE_ENABLED));
            Assert.IsTrue(Has(enabled, STATE_SENSITIVE));

            long disabled = StatesOf(AccessibleStates.Disabled);

            Assert.IsFalse(Has(disabled, STATE_ENABLED),
                "AT-SPI has no disabled state, so a disabled node is one that does not claim "
                    + "ENABLED. Setting a bit for it would say nothing to Orca.");

            Assert.IsFalse(Has(disabled, STATE_SENSITIVE));
        }

        [TestMethod]
        public void AFocusedNodeIsAlsoActive()
        {
            long focused = StatesOf(AccessibleStates.Focusable | AccessibleStates.Focused);

            Assert.IsTrue(Has(focused, STATE_FOCUSABLE));
            Assert.IsTrue(Has(focused, STATE_FOCUSED));
            Assert.IsTrue(Has(focused, STATE_ACTIVE));
        }

        [TestMethod]
        public void AnEditableNodeSaysSoAndSaysHowManyLines()
        {
            long single = StatesOf(AccessibleStates.None, AccessibleActions.SetValue);

            Assert.IsTrue(Has(single, STATE_EDITABLE));
            Assert.IsTrue(Has(single, STATE_SINGLE_LINE));
            Assert.IsFalse(Has(single, STATE_MULTI_LINE));

            long multi = StatesOf(AccessibleStates.Multiline, AccessibleActions.SetValue);

            Assert.IsTrue(Has(multi, STATE_MULTI_LINE));
            Assert.IsFalse(Has(multi, STATE_SINGLE_LINE));
        }

        [TestMethod]
        public void SomethingThatCannotBeWrittenToClaimsNeitherLineCount()
        {
            long plain = StatesOf(AccessibleStates.Multiline);

            Assert.IsFalse(Has(plain, STATE_EDITABLE));
            Assert.IsFalse(Has(plain, STATE_SINGLE_LINE));
            Assert.IsFalse(Has(plain, STATE_MULTI_LINE),
                "a label that happens to wrap is not a multi-line entry");
        }

        [TestMethod]
        public void CheckedSelectedAndInvalidTravelAsTheirOwnBits()
        {
            Assert.IsTrue(Has(StatesOf(AccessibleStates.Checked), STATE_CHECKED));
            Assert.IsTrue(Has(StatesOf(AccessibleStates.Invalid), STATE_INVALID_ENTRY));

            long selected = StatesOf(AccessibleStates.Selected);

            Assert.IsTrue(Has(selected, STATE_SELECTED));
            Assert.IsTrue(Has(selected, STATE_SELECTABLE),
                "something selected is by construction something that could be selected");
        }

        [TestMethod]
        public void AnExpandedNodeSaysItCanCollapse()
        {
            long expanded = StatesOf(AccessibleStates.Expanded);

            Assert.IsTrue(Has(expanded, STATE_EXPANDED));
            Assert.IsTrue(Has(expanded, STATE_EXPANDABLE));
        }

        [TestMethod]
        public void ACollapsedBranchIsExpandableAndALeafIsNot()
        {
            long collapsed = StatesOf(AccessibleStates.Expandable);
            long leaf = StatesOf(AccessibleStates.None);

            Assert.IsTrue(Has(collapsed, STATE_EXPANDABLE));
            Assert.IsFalse(Has(collapsed, STATE_EXPANDED));

            Assert.IsFalse(Has(leaf, STATE_EXPANDABLE),
                "claiming EXPANDABLE on a leaf would make Orca announce 'collapsed' on every "
                    + "leaf of a tree");
        }

        [TestMethod]
        public void SomethingThatCanBeSelectedIsSelectableBeforeItIsSelected()
        {
            Assert.IsTrue(Has(StatesOf(AccessibleStates.None, AccessibleActions.Select), STATE_SELECTABLE),
                "SELECTABLE says a row could be selected, not that it is; only selected rows carried it");
            Assert.IsTrue(Has(StatesOf(AccessibleStates.None, AccessibleActions.AddToSelection), STATE_SELECTABLE));
            Assert.IsFalse(Has(StatesOf(AccessibleStates.None, AccessibleActions.Invoke), STATE_SELECTABLE));
        }

        [TestMethod]
        public void ARadioButtonIsCheckedRatherThanSelectable()
        {
            AccessibleNode radio = Node(AccessibleRole.RadioButton);

            radio.States = AccessibleStates.None;
            radio.Actions = AccessibleActions.Select;

            Assert.IsFalse(Has(LinuxRoles.StatesOf(radio), STATE_SELECTABLE));
        }

        [TestMethod]
        public void TheParentOfRowsThatAddAndRemoveIsMultiSelectable()
        {
            AccessibleNode body = Node(AccessibleRole.Group);
            AccessibleNode row = Node(AccessibleRole.TableRow);

            body.States = AccessibleStates.None;
            row.Actions = AccessibleActions.Select | AccessibleActions.AddToSelection;
            body.ChildList.Add(row);

            Assert.IsTrue(Has(LinuxRoles.StatesOf(body), STATE_MULTISELECTABLE),
                "the Selection interface lives on the rows' parent, so that node must say it holds several");

            row.Actions = AccessibleActions.Select;

            Assert.IsFalse(Has(LinuxRoles.StatesOf(body), STATE_MULTISELECTABLE));

            body.States = AccessibleStates.MultiSelectable;

            Assert.IsTrue(Has(LinuxRoles.StatesOf(body), STATE_MULTISELECTABLE),
                "the table itself says so too");
        }

        [TestMethod]
        public void TheScaleTurnsLogicalUnitsIntoDevicePixels()
        {
            var node = new LinuxAccessibleNode(3, Node(), false, 2f);

            Assert.AreEqual(20, node.X);
            Assert.AreEqual(40, node.Y);
            Assert.AreEqual(60, node.Width);
            Assert.AreEqual(80, node.Height);
        }

        [TestMethod]
        public void TheShortcutTravelsOnItsOwnRatherThanInsideTheDescription()
        {
            var node = new LinuxAccessibleNode(3, Node(), false, 1f);

            Assert.AreEqual("keeps the document", node.Description,
                "AT-SPI has a slot for an accelerator, so unlike macOS nothing has to be folded");

            Assert.AreEqual("Ctrl+S", node.Shortcut);
        }

        [TestMethod]
        public void EveryFieldThatTravelsIsInTheComparison()
        {
            var mutations = new Dictionary<string, Action<AccessibleNode>>
            {
                { "Role", node => node.Role = AccessibleRole.Slider },
                { "States", node => node.States = AccessibleStates.Disabled },
                { "Actions", node => node.Actions = AccessibleActions.Focus },
                { "Name", node => node.Name = "Open" },
                { "Value", node => node.Value = "other" },
                { "Description", node => node.Description = "other" },
                { "Shortcut", node => node.Shortcut = "Ctrl+O" },
                { "X", node => node.X = 99 },
                { "Y", node => node.Y = 99 },
                { "Width", node => node.Width = 99 },
                { "Height", node => node.Height = 99 },
                { "HasRange", node => node.HasRange = true },
                { "RangeMinimum", node => node.RangeMinimum = -5 },
                { "RangeMaximum", node => node.RangeMaximum = 50 },
                { "RangeValue", node => node.RangeValue = 7 },
                { "RangeStep", node => node.RangeStep = 2 },
                { "RowCount", node => node.RowCount = 9 },
                { "ColumnCount", node => node.ColumnCount = 3 },
                { "RowIndex", node => node.RowIndex = 2 },
                { "ColumnIndex", node => node.ColumnIndex = 1 }
            };

            var unguarded = new List<string>();

            foreach (System.Reflection.PropertyInfo property in typeof(LinuxAccessibleNode).GetProperties(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
            {
                if (property.Name != "Parent" && !mutations.ContainsKey(property.Name))
                {
                    unguarded.Add(property.Name);
                }
            }

            Assert.AreEqual(0, unguarded.Count,
                "every field pushed to the bridge needs a mutation here: " + string.Join(", ", unguarded));

            var reference = new LinuxAccessibleNode(3, Node(), false, 1f);

            Assert.IsTrue(reference.SameAs(new LinuxAccessibleNode(3, Node(), false, 1f)),
                "an unchanged node has to compare equal, or every frame pushes everything");

            Assert.IsFalse(reference.SameAs(new LinuxAccessibleNode(4, Node(), false, 1f)),
                "the parent is what Commit rebuilds the tree from");

            Assert.IsFalse(reference.SameAs(new LinuxAccessibleNode(3, Node(), true, 1f)),
                "being the root is what turns the role into a frame");

            Assert.IsFalse(reference.SameAs(new LinuxAccessibleNode(3, Node(), false, 2f)),
                "the scale turns logical units into the device pixels an AT is given");

            foreach (KeyValuePair<string, Action<AccessibleNode>> mutation in mutations)
            {
                AccessibleNode node = Node();

                mutation.Value(node);

                Assert.IsFalse(reference.SameAs(new LinuxAccessibleNode(3, node, false, 1f)),
                    $"a change of {mutation.Key} would never be pushed, so Orca would go on "
                        + "reading whatever was there before");
            }
        }

        [TestMethod]
        public void TheSpokenTextPrefersTheHalfThatActuallyMoved()
        {
            AccessibleNode was = Node();
            AccessibleNode now = Node();

            now.Value = "saved";

            var moved = new AccessibilityChange
            {
                Kind = AccessibilityChangeKind.LiveRegion,
                Id = 1,
                Previous = was,
                Current = now
            };

            Assert.AreEqual("saved", LinuxAccessibility.Spoken(moved));

            AccessibleNode renamed = Node();

            renamed.Name = "Saved";

            var named = new AccessibilityChange
            {
                Kind = AccessibilityChangeKind.LiveRegion,
                Id = 1,
                Previous = Node(),
                Current = renamed
            };

            Assert.AreEqual("Saved", LinuxAccessibility.Spoken(named));
        }

        private static AccessibilityChange StateChange(AccessibleStates was, AccessibleStates now)
            => new AccessibilityChange
            {
                Kind = AccessibilityChangeKind.States,
                Previous = new AccessibleNode { States = was },
                Current = new AccessibleNode { States = now }
            };

        [TestMethod]
        public void EachStateThatMovedIsOneSignal()
        {
            var both = new List<LinuxAccessibilityNotice>(LinuxAccessibility.StateNotices(
                StateChange(AccessibleStates.None, AccessibleStates.Checked | AccessibleStates.Selected)));

            CollectionAssert.AreEqual(
                new[] { LinuxAccessibilityNotice.Checked, LinuxAccessibilityNotice.Selected },
                both);
        }

        [TestMethod]
        public void ALeafGainingChildrenIsAnExpansionSignal()
        {
            var notices = new List<LinuxAccessibilityNotice>(LinuxAccessibility.StateNotices(
                StateChange(AccessibleStates.None, AccessibleStates.Expandable)));

            CollectionAssert.AreEqual(new[] { LinuxAccessibilityNotice.Expanded }, notices,
                "Orca learns that the node is now a closed branch rather than a leaf");
        }

        [TestMethod]
        public void AFocusMoveIsNotAStateSignal()
        {
            var notices = new List<LinuxAccessibilityNotice>(LinuxAccessibility.StateNotices(
                StateChange(AccessibleStates.Focused, AccessibleStates.None)));

            Assert.AreEqual(0, notices.Count, "focus has its own notice already");
        }

        private static List<LinuxTextChange> Edits(string before, string after)
            => new List<LinuxTextChange>(LinuxAccessibility.TextChanges(new AccessibilityChange
            {
                Kind = AccessibilityChangeKind.Value,
                Previous = new AccessibleNode { Role = AccessibleRole.TextField, Value = before },
                Current = new AccessibleNode { Role = AccessibleRole.TextField, Value = after }
            }));

        [TestMethod]
        public void TypingIsOneInsertAtTheCaret()
        {
            List<LinuxTextChange> edits = Edits("Ad", "Ada");

            Assert.AreEqual(1, edits.Count);
            Assert.IsTrue(edits[0].Inserted);
            Assert.AreEqual(2, edits[0].Offset);
            Assert.AreEqual(1, edits[0].Length);
            Assert.AreEqual("a", edits[0].Text);
        }

        [TestMethod]
        public void AReplacementIsADeleteThenAnInsert()
        {
            List<LinuxTextChange> edits = Edits("Hello Ada", "Hello Zoe");

            Assert.AreEqual(2, edits.Count);
            Assert.IsFalse(edits[0].Inserted, "what went is said before what came, as GTK does");
            Assert.AreEqual("Ada", edits[0].Text);
            Assert.IsTrue(edits[1].Inserted);
            Assert.AreEqual("Zoe", edits[1].Text);
            Assert.AreEqual(6, edits[1].Offset);
        }

        [TestMethod]
        public void OffsetsCountCharactersNotUtf16Units()
        {
            List<LinuxTextChange> edits = Edits("\U0001F600", "\U0001F600!");

            Assert.AreEqual(1, edits[0].Offset,
                "AT-SPI offsets are characters; the emoji before the caret is two UTF-16 units "
                + "and one character");
            Assert.AreEqual(1, edits[0].Length);

            List<LinuxTextChange> emoji = Edits("a", "a\U0001F600");

            Assert.AreEqual(1, emoji[0].Length, "and an inserted emoji is one character long");
        }
    }
}
