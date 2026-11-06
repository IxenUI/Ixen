using Ixen.Core.Visual;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class DataColumn
    {
        public string Header { get; set; }

        public float Width { get; set; }

        public bool Resizable { get; set; } = true;

        public float MinWidth { get; set; } = 32;

        public Action<VisualElement, object> Bind { get; set; }

        public Func<VisualElement> Create { get; set; }

        public Comparison<object> Compare { get; set; }

        public Func<object, string> Value { get; set; }

        public Action<object, string> Commit { get; set; }

        public Func<object, bool> Checked { get; set; }

        public Action<object, bool> Toggle { get; set; }

        public IReadOnlyList<string> Choices { get; set; }

        public bool IsChoice => Choices != null && IsEditable;

        public bool IsSortable => Compare != null;

        public bool IsEditable => Commit != null && Checked == null;

        public bool IsCheck => Checked != null;

        public bool IsToggleable => Checked != null && Toggle != null;
    }
}
