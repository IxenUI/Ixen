using Ixen.Core.Visual.Styles;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    internal class StyleClass
    {
        public string Name { get; set; }
        public List<Style> Styles { get; set; }

        public StyleClass (string name)
        {
            Name = name;
            Styles = new();
        }

        public StyleClass(string name, List<Style> styles)
        {
            Name = name;
            Styles = styles;
        }
    }
}
