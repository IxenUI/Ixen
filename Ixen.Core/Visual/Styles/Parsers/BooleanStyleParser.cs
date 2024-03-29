﻿using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class BooleanStyleParser : StyleParser
    {
        public BooleanStyleDescriptor Descriptor { get; } = new BooleanStyleDescriptor();

        public BooleanStyleParser(string content)
            : base(content)
        {}

        protected override bool Parse()
        {
            string content = _content.Trim();

            switch (content)
            {
                case "1":
                    Descriptor.Value = true;
                    return true;
                case "0":
                    Descriptor.Value = false;
                    return true;
                default:
                    return false;
            }
        }
    }
}
