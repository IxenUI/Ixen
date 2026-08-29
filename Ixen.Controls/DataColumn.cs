using Ixen.Core.Visual;
using System;

namespace Ixen.Controls
{
    public class DataColumn
    {
        public string Header { get; set; }

        public float Width { get; set; }

        public Action<VisualElement, object> Bind { get; set; }

        public Comparison<object> Compare { get; set; }

        public bool IsSortable => Compare != null;
    }
}
