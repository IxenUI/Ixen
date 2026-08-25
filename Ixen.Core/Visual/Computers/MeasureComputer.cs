using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Computers
{
    internal class MeasureComputer
    {
        private const string ELLIPSIS = "\u2026";

        private static readonly SizeStyleDescriptor _fillTrack = new SizeStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
        private static readonly SizeStyleDescriptor _contentTrack = new SizeStyleDescriptor { Unit = SizeUnit.Content, Value = 1 };

        private readonly ITextMeasurer _textMeasurer;
        private readonly IImageMeasurer _imageMeasurer;

        private float _viewportWidth;
        private float _viewportHeight;

        internal MeasureComputer(ITextMeasurer textMeasurer, IImageMeasurer imageMeasurer = null)
        {
            _textMeasurer = textMeasurer;
            _imageMeasurer = imageMeasurer;
        }

        internal void Measure(VisualElement element, float availableWidth, float availableHeight, bool widthIsDefinite, bool heightIsDefinite)
        {
            if (element.Parent == null)
            {
                _viewportWidth = availableWidth;
                _viewportHeight = availableHeight;
            }

            ResolveBorders(element);

            availableWidth = Bounded(element, availableWidth, true);
            availableHeight = Bounded(element, availableHeight, false);

            bool scrollable = element.Scrollable;

            if (scrollable)
            {
                element.ScrollbarGutterWidth = 0;
                element.ScrollbarGutterHeight = 0;
            }

            LayoutType type = LayoutTypeOf(element);

            float contentWidth = ContentSize(element, availableWidth, true);
            float contentHeight = ContentSize(element, availableHeight, false);

            MeasureContent(element, type, contentWidth, contentHeight, widthIsDefinite, heightIsDefinite,
                out float intrinsicWidth, out float intrinsicHeight);

            float aggregateWidth = widthIsDefinite && !scrollable ? 0 : AggregateWidth(element, type);
            float aggregateHeight = heightIsDefinite && !scrollable ? 0 : AggregateHeight(element, type);

            if (scrollable && ReserveGutters(element, aggregateWidth, aggregateHeight,
                intrinsicWidth, intrinsicHeight, contentWidth, contentHeight))
            {
                contentWidth = ContentSize(element, availableWidth, true);
                contentHeight = ContentSize(element, availableHeight, false);

                MeasureContent(element, type, contentWidth, contentHeight, widthIsDefinite, heightIsDefinite,
                    out intrinsicWidth, out intrinsicHeight);

                aggregateWidth = AggregateWidth(element, type);
                aggregateHeight = AggregateHeight(element, type);
            }

            element.Width = widthIsDefinite
                ? availableWidth
                : Bounded(element, Math.Max(aggregateWidth, intrinsicWidth)
                    + element.HorizontalPadding + element.HorizontalBorderInside, true);

            element.Height = heightIsDefinite
                ? availableHeight
                : Bounded(element, Math.Max(aggregateHeight, intrinsicHeight)
                    + element.VerticalPadding + element.VerticalBorderInside, false);

            if (scrollable)
            {
                element.ScrollExtentWidth = Math.Max(aggregateWidth, intrinsicWidth);
                element.ScrollExtentHeight = Math.Max(aggregateHeight, intrinsicHeight);
            }
            else
            {
                element.ScrollExtentWidth = 0;
                element.ScrollExtentHeight = 0;
            }

            element.ClampScroll();

            if (element is TextField field)
            {
                ClampFieldOffset(field, intrinsicWidth);
            }

            if (element.HasChrome)
            {
                MeasureScrollbars(element, scrollable);
            }
        }

        private static float Bounded(VisualElement element, float size, bool horizontal)
        {
            VisualElementStylesHandlers handlers = element.StylesHandlers;

            if (handlers == null)
            {
                return size;
            }

            BoundStyleDescriptor max = horizontal
                ? (BoundStyleDescriptor)handlers.MaxWidth.Descriptor
                : handlers.MaxHeight.Descriptor;

            if (max.IsDeclared && size > max.Value)
            {
                size = max.Value;
            }

            BoundStyleDescriptor min = horizontal
                ? (BoundStyleDescriptor)handlers.MinWidth.Descriptor
                : handlers.MinHeight.Descriptor;

            return min.IsDeclared && size < min.Value ? min.Value : size;
        }

        private float ContentSize(VisualElement element, float available, bool horizontal)
        {
            float taken = horizontal
                ? element.HorizontalPadding + element.HorizontalBorderInside + element.ScrollbarGutterWidth
                : element.VerticalPadding + element.VerticalBorderInside + element.ScrollbarGutterHeight;

            return Math.Max(0, available - taken);
        }

        private void MeasureContent(VisualElement element, LayoutType type, float contentWidth, float contentHeight,
            bool widthIsDefinite, bool heightIsDefinite, out float intrinsicWidth, out float intrinsicHeight)
        {
            switch (type)
            {
                case LayoutType.Grid:
                    MeasureGrid(element, contentWidth, contentHeight);
                    break;

                case LayoutType.Absolute:
                    MeasureAnchored(element, contentWidth, contentHeight);
                    break;

                case LayoutType.Fixed:
                    MeasureAnchored(element, _viewportWidth, _viewportHeight);
                    break;

                case LayoutType.Dock:
                    MeasureDock(element, contentWidth, contentHeight);
                    break;

                default:
                    MeasureChildren(element, contentWidth, contentHeight);
                    break;
            }

            LayoutText(element, contentWidth, out intrinsicWidth, out intrinsicHeight);

            if (element is Image image)
            {
                MeasureImage(image, contentWidth, contentHeight, widthIsDefinite, heightIsDefinite,
                    ref intrinsicWidth, ref intrinsicHeight);
            }
        }

        private void MeasureImage(Image image, float contentWidth, float contentHeight,
            bool widthIsDefinite, bool heightIsDefinite, ref float intrinsicWidth, ref float intrinsicHeight)
        {
            image.NaturalWidth = 0;
            image.NaturalHeight = 0;

            if (_imageMeasurer == null
                || !_imageMeasurer.TryMeasure(image.Source, out float natural, out float naturalHeight)
                || natural <= 0 || naturalHeight <= 0)
            {
                return;
            }

            image.NaturalWidth = natural;
            image.NaturalHeight = naturalHeight;

            float width = natural;
            float height = naturalHeight;

            if (widthIsDefinite && !heightIsDefinite)
            {
                width = contentWidth;
                height = naturalHeight * (contentWidth / natural);
            }
            else if (heightIsDefinite && !widthIsDefinite)
            {
                height = contentHeight;
                width = natural * (contentHeight / naturalHeight);
            }

            intrinsicWidth = Math.Max(intrinsicWidth, width);
            intrinsicHeight = Math.Max(intrinsicHeight, height);
        }

        private bool ReserveGutters(VisualElement element, float aggregateWidth, float aggregateHeight,
            float intrinsicWidth, float intrinsicHeight, float contentWidth, float contentHeight)
        {
            float vertical = Math.Max(aggregateHeight, intrinsicHeight) > contentHeight ? Scrollbar.THICKNESS : 0;
            float horizontal = Math.Max(aggregateWidth, intrinsicWidth) > contentWidth ? Scrollbar.THICKNESS : 0;

            if (element.ScrollbarGutterWidth == vertical && element.ScrollbarGutterHeight == horizontal)
            {
                return false;
            }

            element.ScrollbarGutterWidth = vertical;
            element.ScrollbarGutterHeight = horizontal;

            return true;
        }

        private void MeasureScrollbars(VisualElement element, bool scrollable)
        {
            bool vertical = scrollable && element.MaxScrollY > 0;
            bool horizontal = scrollable && element.MaxScrollX > 0;

            float thickness = Scrollbar.THICKNESS;
            float width = element.ActualWidth - (vertical ? thickness : 0);
            float height = element.ActualHeight - (horizontal ? thickness : 0);

            foreach (VisualElement chrome in element.Chrome)
            {
                if (!(chrome is Scrollbar bar))
                {
                    continue;
                }

                if (bar.IsVertical ? !vertical : !horizontal)
                {
                    bar.Hide();
                }
                else if (bar.IsVertical)
                {
                    bar.Layout(element, element.ActualWidth - thickness, 0, Math.Max(0, height), thickness);
                }
                else
                {
                    bar.Layout(element, 0, element.ActualHeight - thickness, Math.Max(0, width), thickness);
                }

                Measure(bar, bar.Styles.Width.Value, bar.Styles.Height.Value, true, true);
            }
        }

        private void LayoutText(VisualElement element, float availableWidth, out float width, out float height)
        {
            width = 0;
            height = 0;

            if (element is TextField field)
            {
                LayoutField(field, availableWidth, out width, out height);
                return;
            }

            if (_textMeasurer == null || string.IsNullOrEmpty(element.Text))
            {
                element.TextLines?.Clear();
                return;
            }

            FontSpec fontSpec = FontSpec.From(element.StylesHandlers);
            bool wrap = element.StylesHandlers.TextWrap.Descriptor.Value == TextWrap.Wrap;
            bool ellipsis = element.StylesHandlers.TextOverflow.Descriptor.Value == TextOverflow.Ellipsis;

            List<string> lines = element.EnsureTextLines();

            width = BuildLines(element.Text, fontSpec, availableWidth, wrap, ellipsis, lines);
            height = _textMeasurer.GetLineHeight(fontSpec) * lines.Count;
        }

        private void LayoutField(TextField field, float availableWidth, out float width, out float height)
        {
            List<string> lines = field.EnsureTextLines();

            if (_textMeasurer == null)
            {
                width = 0;
                height = 0;
                return;
            }

            string value = field.DisplayText;
            FontSpec fontSpec = FontSpec.From(field.StylesHandlers);

            bool breaks = field.Multiline;
            bool wraps = breaks
                && field.StylesHandlers.TextWrap.Descriptor.Value == TextWrap.Wrap
                && availableWidth > 0;

            float[] offsets = EnsureCaretOffsets(field, value.Length + 1);
            int[] starts = EnsureLineStarts(field, value, wraps);

            offsets[0] = 0;
            starts[0] = 0;

            int line = 0;
            int lineStart = 0;
            int lastSpace = -1;

            width = 0;

            for (int i = 0; i < value.Length; i++)
            {
                if (breaks && value[i] == '\n')
                {
                    lines.Add(value.Substring(lineStart, i - lineStart));
                    width = Math.Max(width, offsets[i]);

                    line++;
                    starts[line] = i + 1;
                    lineStart = i + 1;
                    offsets[i + 1] = 0;
                    continue;
                }

                _textMeasurer.MeasureText(value.Substring(lineStart, i + 1 - lineStart), fontSpec,
                    out float prefix, out _);

                offsets[i + 1] = prefix;

                bool blank = value[i] == ' ' || value[i] == '\t';

                if (wraps && !blank && prefix > availableWidth && lastSpace > lineStart)
                {
                    int next = lastSpace + 1;

                    lines.Add(value.Substring(lineStart, next - lineStart));
                    width = Math.Max(width, offsets[next]);

                    line++;
                    starts[line] = next;
                    lineStart = next;
                    offsets[next] = 0;
                    i = next - 1;
                    continue;
                }

                if (blank)
                {
                    lastSpace = i;
                }
            }

            lines.Add(value.Substring(lineStart));
            width = Math.Max(width, offsets[value.Length]);

            field.CaretOffsetCount = value.Length + 1;
            field.LineCount = line + 1;
            field.LineHeight = _textMeasurer.GetLineHeight(fontSpec);

            height = field.LineHeight * field.LineCount;

            if (!field.ShowsPlaceholder)
            {
                return;
            }

            _textMeasurer.MeasureText(field.Placeholder, fontSpec, out float placeholder, out _);
            width = Math.Max(width, placeholder);
        }

        private static int[] EnsureLineStarts(TextField field, string value, bool wraps)
        {
            int capacity = 1;

            if (field.Multiline)
            {
                foreach (char c in value)
                {
                    if (c == '\n' || (wraps && (c == ' ' || c == '\t')))
                    {
                        capacity++;
                    }
                }
            }

            return field.EnsureLineStarts(capacity);
        }

        private static float[] EnsureCaretOffsets(TextField field, int count)
        {
            if (field.CaretOffsets == null || field.CaretOffsets.Length < count)
            {
                field.CaretOffsets = new float[count];
            }

            return field.CaretOffsets;
        }

        private static void ClampFieldOffset(TextField field, float textWidth)
        {
            float contentWidth = field.ContentWidth;
            float caret = field.OffsetAt(field.CaretIndex);
            float offset = field.ContentOffset;

            if (caret - offset > contentWidth)
            {
                offset = caret - contentWidth;
            }

            if (caret - offset < 0)
            {
                offset = caret;
            }

            float max = Math.Max(0, textWidth - contentWidth);

            field.ContentOffset = Math.Max(0, Math.Min(offset, max));

            ScrollCaretIntoView(field);
        }

        private static void ScrollCaretIntoView(TextField field)
        {
            if (!field.Multiline || !field.CaretMoved || field.LineHeight <= 0)
            {
                return;
            }

            field.CaretMoved = false;

            float top = field.LineAt(field.CaretIndex) * field.LineHeight;
            float bottom = top + field.LineHeight;

            if (bottom - field.ScrollY > field.ContentHeight)
            {
                field.ScrollY = bottom - field.ContentHeight;
            }
            else if (top - field.ScrollY < 0)
            {
                field.ScrollY = top;
            }
        }

        private float BuildLines(string text, FontSpec fontSpec, float maxWidth, bool wrap, bool ellipsis,
            List<string> lines)
        {
            float widest = 0;

            if (text.IndexOf('\n') < 0)
            {
                AppendLine(text, fontSpec, maxWidth, wrap, ellipsis, lines, ref widest);
                return widest;
            }

            foreach (string hardLine in text.Split('\n'))
            {
                AppendLine(hardLine.TrimEnd('\r'), fontSpec, maxWidth, wrap, ellipsis, lines, ref widest);
            }

            return widest;
        }

        private void AppendLine(string line, FontSpec fontSpec, float maxWidth,
            bool wrap, bool ellipsis, List<string> lines, ref float widest)
        {
            if (!wrap)
            {
                AddLine(lines, line, fontSpec, maxWidth, ellipsis, ref widest);
                return;
            }

            int lineStart = 0;
            int lastSpace = -1;
            int index = 0;

            while (index <= line.Length)
            {
                bool atEnd = index == line.Length;

                if (!atEnd && line[index] != ' ' && line[index] != '\t')
                {
                    index++;
                    continue;
                }

                _textMeasurer.MeasureText(line.Substring(lineStart, index - lineStart),
                    fontSpec, out float candidateWidth, out _);

                if (candidateWidth > maxWidth && lastSpace > lineStart)
                {
                    AddLine(lines, line.Substring(lineStart, lastSpace - lineStart).TrimEnd(),
                        fontSpec, maxWidth, ellipsis, ref widest);

                    lineStart = lastSpace + 1;
                    lastSpace = -1;
                    continue;
                }

                if (atEnd)
                {
                    break;
                }

                lastSpace = index;
                index++;
            }

            AddLine(lines, line.Substring(lineStart), fontSpec, maxWidth, ellipsis, ref widest);
        }

        private void AddLine(List<string> lines, string line, FontSpec fontSpec, float maxWidth,
            bool ellipsis, ref float widest)
        {
            _textMeasurer.MeasureText(line, fontSpec, out float width, out _);

            if (ellipsis && width > maxWidth)
            {
                line = Ellipsize(line, fontSpec, maxWidth);
                _textMeasurer.MeasureText(line, fontSpec, out width, out _);
            }

            lines.Add(line);

            if (width > widest)
            {
                widest = width;
            }
        }

        private string Ellipsize(string line, FontSpec fontSpec, float maxWidth)
        {
            _textMeasurer.MeasureText(ELLIPSIS, fontSpec, out float ellipsisWidth, out _);

            if (ellipsisWidth > maxWidth)
            {
                return ELLIPSIS;
            }

            int low = 0;
            int high = line.Length;

            while (low < high)
            {
                int mid = (low + high + 1) / 2;

                _textMeasurer.MeasureText(line.Substring(0, mid), fontSpec, out float width, out _);

                if (width + ellipsisWidth <= maxWidth)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return line.Substring(0, low).TrimEnd() + ELLIPSIS;
        }

        private void MeasureChildren(VisualElement element, float contentWidth, float contentHeight)
        {
            if (element.Children.Count == 0)
            {
                return;
            }

            bool isRow = IsRow(element);

            foreach (VisualElement child in element.Children)
            {
                ResolveBorders(child);
                ResolveHorizontalSpacing(child, contentWidth);
                ResolveVerticalSpacing(child, contentHeight);
            }

            float pool = isRow ? contentWidth : contentHeight;
            float totalWeight = 0;

            foreach (VisualElement child in element.Children)
            {
                SizeStyleDescriptor mainStyle = GetSizeStyleDescriptor(child, isRow
                    ? SizeStyleDescriptorType.Width
                    : SizeStyleDescriptorType.Height);

                if (IsFilling(mainStyle))
                {
                    totalWeight += mainStyle.Value;
                    pool -= isRow
                        ? child.HorizontalMargin + child.HorizontalBorderOutside
                        : child.VerticalMargin + child.VerticalBorderOutside;
                    continue;
                }

                MeasureChild(child, contentWidth, contentHeight, isRow, 0, false);
                pool -= isRow ? child.BoxWidth : child.BoxHeight;
            }

            if (totalWeight <= 0)
            {
                return;
            }

            pool = Math.Max(0, pool);

            foreach (VisualElement child in element.Children)
            {
                SizeStyleDescriptor mainStyle = GetSizeStyleDescriptor(child, isRow
                    ? SizeStyleDescriptorType.Width
                    : SizeStyleDescriptorType.Height);

                if (!IsFilling(mainStyle))
                {
                    continue;
                }

                float share = (pool / totalWeight) * mainStyle.Value;
                MeasureChild(child, contentWidth, contentHeight, isRow, share, true);
            }
        }

        private void MeasureChild(VisualElement child, float contentWidth, float contentHeight, bool isRow, float mainShare, bool useMainShare)
        {
            SizeStyleDescriptor widthStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Width);
            SizeStyleDescriptor heightStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Height);

            ResolveAxis(widthStyle, contentWidth, child.HorizontalMargin + child.HorizontalBorderOutside,
                useMainShare && isRow, mainShare, out float availableWidth, out bool widthIsDefinite);

            ResolveAxis(heightStyle, contentHeight, child.VerticalMargin + child.VerticalBorderOutside,
                useMainShare && !isRow, mainShare, out float availableHeight, out bool heightIsDefinite);

            Measure(child, availableWidth, availableHeight, widthIsDefinite, heightIsDefinite);
        }

        private void ResolveAxis(SizeStyleDescriptor style, float contentAvailable, float margin, bool useShare, float share,
            out float available, out bool isDefinite)
        {
            if (useShare)
            {
                available = share;
                isDefinite = true;
                return;
            }

            switch (style.Unit)
            {
                case SizeUnit.Pixels:
                    available = style.Value;
                    isDefinite = true;
                    return;

                case SizeUnit.Percents:
                    available = (contentAvailable / 100) * style.Value;
                    isDefinite = true;
                    return;

                case SizeUnit.Content:
                    available = Math.Max(0, contentAvailable - margin);
                    isDefinite = false;
                    return;

                default:
                    available = Math.Max(0, contentAvailable - margin) * style.Value;
                    isDefinite = true;
                    return;
            }
        }

        private void MeasureGrid(VisualElement element, float contentWidth, float contentHeight)
        {
            if (element.Children.Count == 0)
            {
                return;
            }

            foreach (VisualElement child in element.Children)
            {
                ResolveBorders(child);
                ResolveHorizontalSpacing(child, contentWidth);
                ResolveVerticalSpacing(child, contentHeight);
            }

            List<SizeStyleDescriptor> columnTemplate = element.StylesHandlers.RowTemplate.Descriptor.Value;
            List<SizeStyleDescriptor> rowTemplate = element.StylesHandlers.ColumnTemplate.Descriptor.Value;

            int columnCount = columnTemplate.Count > 0 ? columnTemplate.Count : 1;
            int rowCount = PlaceCells(element, columnCount);

            float[] columns = element.EnsureGridColumns(columnCount);
            float[] rows = element.EnsureGridRows(rowCount);

            ResolveColumnTracks(element, columnTemplate, columns, contentWidth);
            ResolveRowTracks(element, rowTemplate, columns, rows, contentHeight);

            foreach (VisualElement child in element.Children)
            {
                MeasureCell(child,
                    Extent(columns, child.GridColumn, child.GridColumnSpan),
                    Extent(rows, child.GridRow, child.GridRowSpan),
                    true, true);
            }
        }

        private static float Extent(float[] tracks, int start, int span)
        {
            float extent = 0;

            for (int i = start; i < start + span && i < tracks.Length; i++)
            {
                extent += tracks[i];
            }

            return extent;
        }

        private static int PlaceCells(VisualElement element, int columnCount)
        {
            var taken = new List<bool[]>();
            int rowCount = 0;

            foreach (VisualElement child in element.Children)
            {
                child.GridColumnSpan = Clamp(child.StylesHandlers.ColumnSpan.Descriptor.Value, columnCount);
                child.GridRowSpan = Math.Max(1, child.StylesHandlers.RowSpan.Descriptor.Value);
            }

            foreach (VisualElement child in element.Children)
            {
                GridIndexStyleDescriptor columnStyle = child.StylesHandlers.ColumnIndex.Descriptor;
                GridIndexStyleDescriptor rowStyle = child.StylesHandlers.RowIndex.Descriptor;

                if (columnStyle.IsAuto && rowStyle.IsAuto)
                {
                    continue;
                }

                int column = columnStyle.IsAuto ? 0 : Math.Min(columnStyle.Value, columnCount - 1);
                int row = rowStyle.IsAuto ? 0 : rowStyle.Value;

                if (columnStyle.IsAuto)
                {
                    column = FirstFreeColumn(taken, row, columnCount, child.GridColumnSpan);
                }

                Occupy(taken, column, row, child.GridColumnSpan, child.GridRowSpan, columnCount);

                child.GridColumn = column;
                child.GridRow = row;

                rowCount = Math.Max(rowCount, row + child.GridRowSpan);
            }

            int cursor = 0;

            foreach (VisualElement child in element.Children)
            {
                if (!child.StylesHandlers.ColumnIndex.Descriptor.IsAuto
                    || !child.StylesHandlers.RowIndex.Descriptor.IsAuto)
                {
                    continue;
                }

                while (!Fits(taken, cursor % columnCount, cursor / columnCount, child.GridColumnSpan, columnCount))
                {
                    cursor++;
                }

                int column = cursor % columnCount;
                int row = cursor / columnCount;

                Occupy(taken, column, row, child.GridColumnSpan, child.GridRowSpan, columnCount);

                child.GridColumn = column;
                child.GridRow = row;

                rowCount = Math.Max(rowCount, row + child.GridRowSpan);
                cursor += child.GridColumnSpan;
            }

            return Math.Max(1, rowCount);
        }

        private static int Clamp(int span, int columnCount)
            => Math.Min(Math.Max(1, span), columnCount);

        private static bool[] RowOf(List<bool[]> taken, int row, int columnCount)
        {
            while (taken.Count <= row)
            {
                taken.Add(new bool[columnCount]);
            }

            return taken[row];
        }

        private static bool Fits(List<bool[]> taken, int column, int row, int span, int columnCount)
        {
            if (column + span > columnCount)
            {
                return false;
            }

            bool[] cells = RowOf(taken, row, columnCount);

            for (int i = column; i < column + span; i++)
            {
                if (cells[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static int FirstFreeColumn(List<bool[]> taken, int row, int columnCount, int span)
        {
            for (int column = 0; column + span <= columnCount; column++)
            {
                if (Fits(taken, column, row, span, columnCount))
                {
                    return column;
                }
            }

            return 0;
        }

        private static void Occupy(List<bool[]> taken, int column, int row, int columnSpan, int rowSpan,
            int columnCount)
        {
            for (int r = row; r < row + rowSpan; r++)
            {
                bool[] cells = RowOf(taken, r, columnCount);

                for (int c = column; c < column + columnSpan && c < columnCount; c++)
                {
                    cells[c] = true;
                }
            }
        }

        private void ResolveColumnTracks(VisualElement element, List<SizeStyleDescriptor> template, float[] columns, float available)
        {
            float pool = available;
            float totalWeight = 0;

            for (int i = 0; i < columns.Length; i++)
            {
                SizeStyleDescriptor style = TrackStyle(template, i, _fillTrack);

                switch (style.Unit)
                {
                    case SizeUnit.Pixels:
                        columns[i] = style.Value;
                        break;

                    case SizeUnit.Percents:
                        columns[i] = (available / 100) * style.Value;
                        break;

                    case SizeUnit.Content:
                        columns[i] = MeasureColumnExtent(element, columns.Length, i, available);
                        break;

                    default:
                        columns[i] = 0;
                        totalWeight += style.Value;
                        continue;
                }

                pool -= columns[i];
            }

            ShareRemainder(template, columns, _fillTrack, pool, totalWeight);
        }

        private void ResolveRowTracks(VisualElement element, List<SizeStyleDescriptor> template, float[] columns, float[] rows, float available)
        {
            float pool = available;
            float totalWeight = 0;

            for (int i = 0; i < rows.Length; i++)
            {
                SizeStyleDescriptor style = TrackStyle(template, i, _contentTrack);

                switch (style.Unit)
                {
                    case SizeUnit.Pixels:
                        rows[i] = style.Value;
                        break;

                    case SizeUnit.Percents:
                        rows[i] = (available / 100) * style.Value;
                        break;

                    case SizeUnit.Content:
                        rows[i] = MeasureRowExtent(element, columns, i, available);
                        break;

                    default:
                        rows[i] = 0;
                        totalWeight += style.Value;
                        continue;
                }

                pool -= rows[i];
            }

            ShareRemainder(template, rows, _contentTrack, pool, totalWeight);
        }

        private void ShareRemainder(List<SizeStyleDescriptor> template, float[] tracks, SizeStyleDescriptor fallback,
            float pool, float totalWeight)
        {
            if (totalWeight <= 0)
            {
                return;
            }

            pool = Math.Max(0, pool);

            for (int i = 0; i < tracks.Length; i++)
            {
                SizeStyleDescriptor style = TrackStyle(template, i, fallback);

                if (style.Unit == SizeUnit.Weight || style.Unit == SizeUnit.Unset)
                {
                    tracks[i] = (pool / totalWeight) * style.Value;
                }
            }
        }

        private float MeasureColumnExtent(VisualElement element, int columnCount, int column, float available)
        {
            float extent = 0;

            foreach (VisualElement child in element.Children)
            {
                if (child.GridColumn != column || child.GridColumnSpan > 1)
                {
                    continue;
                }

                MeasureCell(child, available, available, false, false);

                if (child.BoxWidth > extent)
                {
                    extent = child.BoxWidth;
                }
            }

            return extent;
        }

        private float MeasureRowExtent(VisualElement element, float[] columns, int row, float available)
        {
            float extent = 0;

            foreach (VisualElement child in element.Children)
            {
                if (child.GridRow != row || child.GridRowSpan > 1)
                {
                    continue;
                }

                MeasureCell(child, Extent(columns, child.GridColumn, child.GridColumnSpan), available, true, false);

                if (child.BoxHeight > extent)
                {
                    extent = child.BoxHeight;
                }
            }

            return extent;
        }

        private void MeasureCell(VisualElement child, float cellWidth, float cellHeight,
            bool widthCellIsDefinite, bool heightCellIsDefinite)
        {
            ResolveCellAxis(child.StylesHandlers.Width.Descriptor, cellWidth,
                child.HorizontalMargin + child.HorizontalBorderOutside,
                widthCellIsDefinite, out float availableWidth, out bool widthIsDefinite);

            ResolveCellAxis(child.StylesHandlers.Height.Descriptor, cellHeight,
                child.VerticalMargin + child.VerticalBorderOutside,
                heightCellIsDefinite, out float availableHeight, out bool heightIsDefinite);

            Measure(child, availableWidth, availableHeight, widthIsDefinite, heightIsDefinite);
        }

        private void ResolveCellAxis(SizeStyleDescriptor style, float cell, float margin, bool cellIsDefinite,
            out float available, out bool isDefinite)
        {
            if (!cellIsDefinite && style.Unit != SizeUnit.Pixels)
            {
                available = Math.Max(0, cell - margin);
                isDefinite = false;
                return;
            }

            ResolveAxis(style, cell, margin, false, 0, out available, out isDefinite);
        }

        private static SizeStyleDescriptor TrackStyle(List<SizeStyleDescriptor> template, int index, SizeStyleDescriptor fallback)
            => template.Count > 0 ? template[index % template.Count] : fallback;

        private static float Sum(float[] values)
        {
            float total = 0;

            if (values == null)
            {
                return total;
            }

            foreach (float value in values)
            {
                total += value;
            }

            return total;
        }

        private float AggregateWidth(VisualElement element, LayoutType type)
        {
            if (type == LayoutType.Grid)
            {
                return Sum(element.GridColumns);
            }

            if (type == LayoutType.Fixed)
            {
                return 0;
            }

            if (type == LayoutType.Absolute || type == LayoutType.Dock)
            {
                return PlacedExtentWidth(element);
            }

            float total = 0;
            bool isRow = IsRow(element);

            foreach (VisualElement child in element.Children)
            {
                if (isRow)
                {
                    total += child.BoxWidth;
                }
                else if (child.BoxWidth > total)
                {
                    total = child.BoxWidth;
                }
            }

            return total;
        }

        private float AggregateHeight(VisualElement element, LayoutType type)
        {
            if (type == LayoutType.Grid)
            {
                return Sum(element.GridRows);
            }

            if (type == LayoutType.Fixed)
            {
                return 0;
            }

            if (type == LayoutType.Absolute || type == LayoutType.Dock)
            {
                return PlacedExtentHeight(element);
            }

            float total = 0;
            bool isRow = IsRow(element);

            foreach (VisualElement child in element.Children)
            {
                if (!isRow)
                {
                    total += child.BoxHeight;
                }
                else if (child.BoxHeight > total)
                {
                    total = child.BoxHeight;
                }
            }

            return total;
        }

        private void MeasureAnchored(VisualElement element, float contentWidth, float contentHeight)
        {
            if (element.Children.Count == 0)
            {
                return;
            }

            foreach (VisualElement child in element.Children)
            {
                ResolveBorders(child);
                ResolveHorizontalSpacing(child, contentWidth);
                ResolveVerticalSpacing(child, contentHeight);
            }

            foreach (VisualElement child in element.Children)
            {
                ResolveAnchor(AnimatedOffset(child, StyleIdentifier.LEFT, child.StylesHandlers.Left.Descriptor),
                    contentWidth, out float left, out bool hasLeft);

                ResolveAnchor(AnimatedOffset(child, StyleIdentifier.RIGHT, child.StylesHandlers.Right.Descriptor),
                    contentWidth, out float right, out bool hasRight);

                ResolveAnchor(AnimatedOffset(child, StyleIdentifier.TOP, child.StylesHandlers.Top.Descriptor),
                    contentHeight, out float top, out bool hasTop);

                ResolveAnchor(AnimatedOffset(child, StyleIdentifier.BOTTOM, child.StylesHandlers.Bottom.Descriptor),
                    contentHeight, out float bottom, out bool hasBottom);

                float horizontalSpacing = child.HorizontalMargin + child.HorizontalBorderOutside;
                float verticalSpacing = child.VerticalMargin + child.VerticalBorderOutside;

                ResolveAnchoredAxis(GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Width),
                    contentWidth, horizontalSpacing, left, right,
                    out float availableWidth, out bool widthIsDefinite);

                ResolveAnchoredAxis(GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Height),
                    contentHeight, verticalSpacing, top, bottom,
                    out float availableHeight, out bool heightIsDefinite);

                Measure(child, availableWidth, availableHeight, widthIsDefinite, heightIsDefinite);

                child.LayoutOffsetX = AnchoredOffset(left, right, hasLeft, hasRight, contentWidth, child.BoxWidth);
                child.LayoutOffsetY = AnchoredOffset(top, bottom, hasTop, hasBottom, contentHeight, child.BoxHeight);
            }
        }

        private void ResolveAnchor(OffsetStyleDescriptor style, float contentAvailable, out float value, out bool isSet)
        {
            if (style == null || style.Unit == SizeUnit.Unset)
            {
                value = 0;
                isSet = false;
                return;
            }

            value = style.Unit == SizeUnit.Percents
                ? (contentAvailable / 100) * style.Value
                : style.Value;

            isSet = true;
        }

        private void ResolveAnchoredAxis(SizeStyleDescriptor style, float contentAvailable, float spacing,
            float from, float to, out float available, out bool isDefinite)
        {
            switch (style.Unit)
            {
                case SizeUnit.Pixels:
                    available = style.Value;
                    isDefinite = true;
                    return;

                case SizeUnit.Percents:
                    available = (contentAvailable / 100) * style.Value;
                    isDefinite = true;
                    return;

                case SizeUnit.Content:
                    available = Math.Max(0, contentAvailable - from - to - spacing);
                    isDefinite = false;
                    return;

                default:
                    available = Math.Max(0, contentAvailable - from - to - spacing);
                    isDefinite = true;
                    return;
            }
        }

        private float AnchoredOffset(float from, float to, bool hasFrom, bool hasTo, float contentAvailable, float boxSize)
        {
            if (hasFrom)
            {
                return from;
            }

            if (hasTo)
            {
                return contentAvailable - to - boxSize;
            }

            return 0;
        }

        private void MeasureDock(VisualElement element, float contentWidth, float contentHeight)
        {
            if (element.Children.Count == 0)
            {
                return;
            }

            foreach (VisualElement child in element.Children)
            {
                ResolveBorders(child);
                ResolveHorizontalSpacing(child, contentWidth);
                ResolveVerticalSpacing(child, contentHeight);
            }

            float left = 0;
            float top = 0;
            float right = contentWidth;
            float bottom = contentHeight;

            foreach (VisualElement child in element.Children)
            {
                float bandWidth = Math.Max(0, right - left);
                float bandHeight = Math.Max(0, bottom - top);

                ResolveDockAxis(GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Width),
                    contentWidth, bandWidth, child.HorizontalMargin + child.HorizontalBorderOutside,
                    out float availableWidth, out bool widthIsDefinite);

                ResolveDockAxis(GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Height),
                    contentHeight, bandHeight, child.VerticalMargin + child.VerticalBorderOutside,
                    out float availableHeight, out bool heightIsDefinite);

                Measure(child, availableWidth, availableHeight, widthIsDefinite, heightIsDefinite);

                switch (child.StylesHandlers.Dock.Descriptor.Side)
                {
                    case DockSide.Left:
                        child.LayoutOffsetX = left;
                        child.LayoutOffsetY = top;
                        left += child.BoxWidth;
                        break;

                    case DockSide.Right:
                        right -= child.BoxWidth;
                        child.LayoutOffsetX = right;
                        child.LayoutOffsetY = top;
                        break;

                    case DockSide.Top:
                        child.LayoutOffsetX = left;
                        child.LayoutOffsetY = top;
                        top += child.BoxHeight;
                        break;

                    case DockSide.Bottom:
                        bottom -= child.BoxHeight;
                        child.LayoutOffsetX = left;
                        child.LayoutOffsetY = bottom;
                        break;

                    default:
                        child.LayoutOffsetX = left;
                        child.LayoutOffsetY = top;
                        left = right;
                        top = bottom;
                        break;
                }
            }
        }

        private void ResolveDockAxis(SizeStyleDescriptor style, float contentAvailable, float band, float spacing,
            out float available, out bool isDefinite)
        {
            switch (style.Unit)
            {
                case SizeUnit.Pixels:
                    available = style.Value;
                    isDefinite = true;
                    return;

                case SizeUnit.Percents:
                    available = (contentAvailable / 100) * style.Value;
                    isDefinite = true;
                    return;

                case SizeUnit.Content:
                    available = Math.Max(0, band - spacing);
                    isDefinite = false;
                    return;

                default:
                    available = Math.Max(0, band - spacing);
                    isDefinite = true;
                    return;
            }
        }

        private float PlacedExtentWidth(VisualElement element)
        {
            float total = 0;

            foreach (VisualElement child in element.Children)
            {
                float edge = child.LayoutOffsetX + child.BoxWidth;

                if (edge > total)
                {
                    total = edge;
                }
            }

            return total;
        }

        private float PlacedExtentHeight(VisualElement element)
        {
            float total = 0;

            foreach (VisualElement child in element.Children)
            {
                float edge = child.LayoutOffsetY + child.BoxHeight;

                if (edge > total)
                {
                    total = edge;
                }
            }

            return total;
        }

        internal static LayoutType LayoutTypeOf(VisualElement element)
        {
            LayoutStyleDescriptor layoutStyle = element.StylesHandlers.Layout.Descriptor;
            return layoutStyle != null ? layoutStyle.Type : LayoutType.Column;
        }

        private static bool IsRow(VisualElement element)
            => LayoutTypeOf(element) == LayoutType.Row;

        private static bool IsGrid(VisualElement element)
            => LayoutTypeOf(element) == LayoutType.Grid;

        private static bool IsFilling(SizeStyleDescriptor style)
            => style.Unit == SizeUnit.Weight || style.Unit == SizeUnit.Unset;

        private static SizeStyleDescriptor Animated(VisualElement element, string identifier,
            SizeStyleDescriptor style)
            => element.AnimatedSize(identifier) ?? style;

        private static OffsetStyleDescriptor AnimatedOffset(VisualElement element, string identifier,
            OffsetStyleDescriptor style)
        {
            if (!element.HasAnimations)
            {
                return style;
            }

            return element.AnimatedSize(identifier) as OffsetStyleDescriptor ?? style;
        }

        private SizeStyleDescriptor GetSizeStyleDescriptor(VisualElement element, SizeStyleDescriptorType sizeType)
        {
            // Direct size style has priority
            SizeStyleDescriptor sizeStyle = (sizeType == SizeStyleDescriptorType.Width)
                ? element.StylesHandlers.Width.Descriptor
                : element.StylesHandlers.Height.Descriptor;

            if (element.HasAnimations)
            {
                sizeStyle = Animated(element, sizeType == SizeStyleDescriptorType.Width
                    ? StyleIdentifier.WIDTH
                    : StyleIdentifier.HEIGHT, sizeStyle);
            }

            // Get the templated size if any
            if (sizeStyle.Unit == SizeUnit.Unset && element.Parent != null)
            {
                LayoutStyleDescriptor layoutStyle = element.Parent.StylesHandlers.Layout.Descriptor;
                SizeTemplateStyleDescriptor sizeTemplateStyle = (sizeType == SizeStyleDescriptorType.Width)
                    ? element.Parent.StylesHandlers.RowTemplate.Descriptor
                    : element.Parent.StylesHandlers.ColumnTemplate.Descriptor;

                if (sizeTemplateStyle.Value.Count > 0
                    && ((layoutStyle.Type == LayoutType.Column && sizeType == SizeStyleDescriptorType.Height)
                    || (layoutStyle.Type == LayoutType.Row && sizeType == SizeStyleDescriptorType.Width)))
                {
                    int index = element.ChildIndex % sizeTemplateStyle.Value.Count;
                    sizeStyle = sizeTemplateStyle.Value[index];
                }
            }

            return sizeStyle;
        }

        private void ResolveBorders(VisualElement element)
        {
            BorderStyleDescriptor border = element.StylesHandlers.Border.Descriptor;

            if (border == null)
            {
                element.BorderInsideTop = 0;
                element.BorderInsideRight = 0;
                element.BorderInsideBottom = 0;
                element.BorderInsideLeft = 0;

                element.BorderOutsideTop = 0;
                element.BorderOutsideRight = 0;
                element.BorderOutsideBottom = 0;
                element.BorderOutsideLeft = 0;

                return;
            }

            element.BorderInsideTop = Inside(border, border.Top);
            element.BorderInsideRight = Inside(border, border.Right);
            element.BorderInsideBottom = Inside(border, border.Bottom);
            element.BorderInsideLeft = Inside(border, border.Left);

            element.BorderOutsideTop = Outside(border, border.Top);
            element.BorderOutsideRight = Outside(border, border.Right);
            element.BorderOutsideBottom = Outside(border, border.Bottom);
            element.BorderOutsideLeft = Outside(border, border.Left);
        }

        private static float Inside(BorderStyleDescriptor border, float thickness)
        {
            if (thickness <= 0)
            {
                return 0;
            }

            switch (border.Type)
            {
                case BorderType.Inner:
                    return thickness;

                case BorderType.Outer:
                    return 0;

                default:
                    return thickness / 2;
            }
        }

        private static float Outside(BorderStyleDescriptor border, float thickness)
        {
            if (thickness <= 0)
            {
                return 0;
            }

            switch (border.Type)
            {
                case BorderType.Inner:
                    return 0;

                case BorderType.Outer:
                    return thickness;

                default:
                    return thickness / 2;
            }
        }

        private void ResolveHorizontalSpacing(VisualElement element, float contentWidth)
        {
            MarginStyleDescriptor marginStyle = element.StylesHandlers.Margin.Descriptor;
            element.MarginLeft = ResolveSpace(marginStyle.Left, contentWidth);
            element.MarginRight = ResolveSpace(marginStyle.Right, contentWidth);

            MarginStyleDescriptor paddingStyle = element.StylesHandlers.Padding.Descriptor;
            element.PaddingLeft = ResolveSpace(paddingStyle.Left, contentWidth);
            element.PaddingRight = ResolveSpace(paddingStyle.Right, contentWidth);
        }

        private void ResolveVerticalSpacing(VisualElement element, float contentHeight)
        {
            MarginStyleDescriptor marginStyle = element.StylesHandlers.Margin.Descriptor;
            element.MarginTop = ResolveSpace(marginStyle.Top, contentHeight);
            element.MarginBottom = ResolveSpace(marginStyle.Bottom, contentHeight);

            MarginStyleDescriptor paddingStyle = element.StylesHandlers.Padding.Descriptor;
            element.PaddingTop = ResolveSpace(paddingStyle.Top, contentHeight);
            element.PaddingBottom = ResolveSpace(paddingStyle.Bottom, contentHeight);
        }

        private float ResolveSpace(SizeStyleDescriptor style, float contentAvailable)
        {
            switch (style.Unit)
            {
                case SizeUnit.Pixels:
                    return style.Value;

                case SizeUnit.Percents:
                    return (contentAvailable / 100) * style.Value;

                default:
                    return 0;
            }
        }
    }
}
