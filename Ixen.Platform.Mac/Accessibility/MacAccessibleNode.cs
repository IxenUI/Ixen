using Ixen.Core.Accessibility;
using Ixen.Platform.Mac.NativeApi;
using System;

namespace Ixen.Platform.Mac.Accessibility
{
    internal readonly struct MacAccessibleNode
    {
        internal MacAccessibleNode(int parent, AccessibleNode node, float scale)
        {
            Parent = parent;
            Role = MacRoles.ToNative(node.Role);
            States = (int)node.States;
            Actions = (int)node.Actions;
            Toggle = MacRoles.ToggleOf(node);
            X = (int)Math.Round(node.X * scale);
            Y = (int)Math.Round(node.Y * scale);
            Width = (int)Math.Round(node.Width * scale);
            Height = (int)Math.Round(node.Height * scale);
            Name = node.Name;
            Value = node.Value;
            Help = MacRoles.HelpOf(node);
        }

        internal int Parent { get; }
        internal string Role { get; }
        internal int States { get; }
        internal int Actions { get; }
        internal int Toggle { get; }
        internal int X { get; }
        internal int Y { get; }
        internal int Width { get; }
        internal int Height { get; }
        internal string Name { get; }
        internal string Value { get; }
        internal string Help { get; }

        internal bool SameAs(MacAccessibleNode other)
            => Parent == other.Parent
                && Role == other.Role
                && States == other.States
                && Actions == other.Actions
                && Toggle == other.Toggle
                && X == other.X
                && Y == other.Y
                && Width == other.Width
                && Height == other.Height
                && Name == other.Name
                && Value == other.Value
                && Help == other.Help;
    }
}
