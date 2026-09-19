using Ixen.Core.Accessibility;
using Ixen.Platform.Linux.NativeApi;
using System;

namespace Ixen.Platform.Linux.Accessibility
{
    internal readonly struct LinuxAccessibleNode
    {
        internal LinuxAccessibleNode(int parent, AccessibleNode node, bool isRoot, float scale)
        {
            Parent = parent;
            Role = LinuxRoles.RoleOf(node, isRoot);
            States = LinuxRoles.StatesOf(node);
            Actions = (int)node.Actions;
            X = (int)Math.Round(node.X * scale);
            Y = (int)Math.Round(node.Y * scale);
            Width = (int)Math.Round(node.Width * scale);
            Height = (int)Math.Round(node.Height * scale);
            Name = node.Name;
            Description = node.Description;
            Value = node.Value;
            Shortcut = node.Shortcut;
        }

        internal int Parent { get; }
        internal int Role { get; }
        internal long States { get; }
        internal int Actions { get; }
        internal int X { get; }
        internal int Y { get; }
        internal int Width { get; }
        internal int Height { get; }
        internal string Name { get; }
        internal string Description { get; }
        internal string Value { get; }
        internal string Shortcut { get; }

        internal bool SameAs(LinuxAccessibleNode other)
            => Parent == other.Parent
                && Role == other.Role
                && States == other.States
                && Actions == other.Actions
                && X == other.X
                && Y == other.Y
                && Width == other.Width
                && Height == other.Height
                && Name == other.Name
                && Description == other.Description
                && Value == other.Value
                && Shortcut == other.Shortcut;
    }
}
