using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Styles
{
    internal class StyleDefinition
    {
        private readonly Func<string, StyleParser> _createParser;
        private readonly Func<StyleParser, StyleDescriptor> _getDescriptor;

        internal string Name { get; }
        internal IReadOnlyList<string> Values { get; }
        internal IReadOnlyList<string> Keywords { get; }

        internal StyleDefinition(string name, Func<string, StyleParser> createParser,
            Func<StyleParser, StyleDescriptor> getDescriptor, string[] values, string[] keywords = null)
        {
            Name = name;
            Values = values ?? new string[0];
            Keywords = keywords ?? Values;

            _createParser = createParser;
            _getDescriptor = getDescriptor;
        }

        internal StyleParser CreateParser(string value) => _createParser(value);

        internal StyleDescriptor DescriptorOf(StyleParser parser) => _getDescriptor(parser);
    }
}
