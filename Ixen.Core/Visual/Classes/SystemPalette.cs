using Ixen.Core.Visual.Styles;

namespace Ixen.Core.Visual.Classes
{
    public sealed class SystemPalette
    {
        public const string SURFACE = "system_surface";
        public const string TEXT = "system_text";
        public const string CONTROL = "system_control";
        public const string CONTROL_TEXT = "system_control_text";
        public const string ACCENT = "system_accent";
        public const string ACCENT_TEXT = "system_accent_text";
        public const string DISABLED = "system_disabled";

        public string Surface { get; set; }
        public string Text { get; set; }
        public string Control { get; set; }
        public string ControlText { get; set; }
        public string Accent { get; set; }
        public string AccentText { get; set; }
        public string Disabled { get; set; }

        public bool IsComplete
            => StyleColors.IsLiteral(Surface)
                && StyleColors.IsLiteral(Text)
                && StyleColors.IsLiteral(Control)
                && StyleColors.IsLiteral(ControlText)
                && StyleColors.IsLiteral(Accent)
                && StyleColors.IsLiteral(AccentText)
                && StyleColors.IsLiteral(Disabled);

        internal void Publish(StyleTokens tokens)
        {
            if (tokens == null)
            {
                return;
            }

            Declare(tokens, SURFACE, Surface);
            Declare(tokens, TEXT, Text);
            Declare(tokens, CONTROL, Control);
            Declare(tokens, CONTROL_TEXT, ControlText);
            Declare(tokens, ACCENT, Accent);
            Declare(tokens, ACCENT_TEXT, AccentText);
            Declare(tokens, DISABLED, Disabled);
        }

        private static void Declare(StyleTokens tokens, string name, string color)
        {
            if (StyleColors.IsLiteral(color))
            {
                tokens.Declare(name, color, true);
            }
        }
    }
}
