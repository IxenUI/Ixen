using Ixen.Core.Visual.Styles;

namespace Ixen.Core.Language.Xnl
{
    internal static class XnlStyleHint
    {
        internal static string For(string xnlName)
        {
            if (string.IsNullOrEmpty(xnlName) || StyleDefinitions.Find(xnlName) == null)
            {
                return null;
            }

            return $"'{xnlName}' is a style, not a property: it goes in a stylesheet rule for this element "
                + $"(its name {{ {xnlName}: ... }}), and XNL sets only properties and events.";
        }
    }
}
