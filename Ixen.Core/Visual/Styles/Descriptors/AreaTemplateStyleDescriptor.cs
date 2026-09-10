using System;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class AreaTemplateStyleDescriptor : StyleDescriptor
    {
        public const string EMPTY = ".";

        internal override string Identifier => StyleIdentifier.AREA_TEMPLATE;

        public List<string[]> Rows { get; set; } = new();

        public int RowCount => Rows.Count;

        public int ColumnCount => Rows.Count == 0 ? 0 : Rows[0].Length;

        public bool IsDeclared => Rows.Count > 0;

        internal bool TryFind(string name, out int row, out int column, out int rowSpan, out int columnSpan)
        {
            row = -1;
            column = -1;
            rowSpan = 0;
            columnSpan = 0;

            int lastRow = -1;
            int lastColumn = -1;

            for (int r = 0; r < Rows.Count; r++)
            {
                string[] cells = Rows[r];

                for (int c = 0; c < cells.Length; c++)
                {
                    if (cells[c] != name)
                    {
                        continue;
                    }

                    if (row < 0)
                    {
                        row = r;
                        column = c;
                    }

                    lastRow = r;
                    column = Math.Min(column, c);
                    lastColumn = Math.Max(lastColumn, c);
                }
            }

            if (row < 0)
            {
                return false;
            }

            rowSpan = lastRow - row + 1;
            columnSpan = lastColumn - column + 1;

            return true;
        }

        internal bool IsRectangular(string name)
        {
            if (!TryFind(name, out int row, out int column, out int rowSpan, out int columnSpan))
            {
                return false;
            }

            for (int r = row; r < row + rowSpan; r++)
            {
                for (int c = column; c < column + columnSpan; c++)
                {
                    if (Rows[r][c] != name)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal IEnumerable<string> Names()
            => Rows.SelectMany(r => r).Where(c => c != EMPTY).Distinct();

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(AreaTemplateStyleDescriptor)} {{ {nameof(Rows)} = new() {{ "
                + string.Join(", ", Rows.Select(r =>
                    "new[] { " + string.Join(", ", r.Select(c => $"\"{c}\"")) + "}"))
                + "} }";
    }
}
