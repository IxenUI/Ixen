using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;
using System.Globalization;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class SizeTemplateStyleParser : StyleParser
    {
        internal const int MAX_TRACKS = 1000;

        private const string REPEAT = "repeat";
        private const string MINMAX = "minmax";
        private const string AUTO_FILL = "auto-fill";

        public SizeTemplateStyleDescriptor Descriptor { get; } = new();

        public SizeTemplateStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string[] tokens = SplitTokens(_content);

            if (tokens == null || tokens.Length < 1)
            {
                return false;
            }

            foreach (string token in tokens)
            {
                string repeat = CallBody(token, REPEAT);

                if (repeat != null)
                {
                    if (Descriptor.AutoFill || !ParseRepeat(repeat))
                    {
                        return false;
                    }

                    continue;
                }

                if (Descriptor.AutoFill || !AddTrack(token))
                {
                    return false;
                }
            }

            return Descriptor.Value.Count > 0
                && (!Descriptor.AutoFill || AutoFillable());
        }

        private bool ParseRepeat(string body)
        {
            string[] arguments = SplitArguments(body);

            if (arguments == null || arguments.Length < 2)
            {
                return false;
            }

            int count = 1;

            if (arguments[0] == AUTO_FILL)
            {
                if (Descriptor.Value.Count > 0)
                {
                    return false;
                }

                Descriptor.AutoFill = true;
            }
            else if (!int.TryParse(arguments[0], NumberStyles.None, CultureInfo.InvariantCulture, out count)
                || count < 1)
            {
                return false;
            }

            var group = new List<string>();

            for (int index = 1; index < arguments.Length; index++)
            {
                string[] tokens = SplitTokens(arguments[index]);

                if (tokens == null || tokens.Length < 1)
                {
                    return false;
                }

                group.AddRange(tokens);
            }

            for (int pass = 0; pass < count; pass++)
            {
                foreach (string token in group)
                {
                    if (!AddTrack(token) || Descriptor.Value.Count > MAX_TRACKS)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool AddTrack(string token)
        {
            string minmax = CallBody(token, MINMAX);

            if (minmax == null)
            {
                var plain = new SizeStyleParser(token);

                if (!plain.IsValid)
                {
                    return false;
                }

                Descriptor.Value.Add(plain.Descriptor);

                return true;
            }

            string[] bounds = SplitArguments(minmax);

            if (bounds == null || bounds.Length != 2)
            {
                return false;
            }

            var low = new SizeStyleParser(bounds[0]);
            var high = new SizeStyleParser(bounds[1]);

            if (!low.IsValid || !high.IsValid)
            {
                return false;
            }

            if (low.Descriptor.Unit == SizeUnit.Weight || low.Descriptor.Unit == SizeUnit.Unset)
            {
                return false;
            }

            high.Descriptor.TrackMin = low.Descriptor;
            Descriptor.Value.Add(high.Descriptor);

            return true;
        }

        private bool AutoFillable()
        {
            foreach (SizeStyleDescriptor track in Descriptor.Value)
            {
                SizeStyleDescriptor floor = track.TrackMin ?? track;

                if (floor.Unit != SizeUnit.Pixels && floor.Unit != SizeUnit.Percents)
                {
                    return false;
                }

                if (floor.Value <= 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
