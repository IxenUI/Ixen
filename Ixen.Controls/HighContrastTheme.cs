using Ixen.Core;
using Ixen.Core.Visual.Classes;

namespace Ixen.Controls
{
    public static class HighContrastTheme
    {
        public const string SURFACE = "control_surface";
        public const string SURFACE_OVER = "control_surface_over";
        public const string SURFACE_DOWN = "control_surface_down";
        public const string SURFACE_OFF = "control_surface_off";
        public const string FIELD = "control_field";
        public const string PANEL = "control_panel";
        public const string LINE = "control_line";
        public const string LINE_OFF = "control_line_off";
        public const string TEXT = "control_text";
        public const string TEXT_OFF = "control_text_off";
        public const string INVERSE = "control_inverse";
        public const string INVERSE_TEXT = "control_inverse_text";
        public const string FOCUS = "control_focus";
        public const string FOCUS_SOFT = "control_focus_soft";

        internal static readonly string[] Names =
        {
            SURFACE, SURFACE_OVER, SURFACE_DOWN, SURFACE_OFF, FIELD, PANEL, LINE, LINE_OFF,
            TEXT, TEXT_OFF, INVERSE, INVERSE_TEXT, FOCUS, FOCUS_SOFT
        };

        public static bool Sync(IElementHost host)
        {
            if (host == null)
            {
                return false;
            }

            SystemPalette palette = host.SystemColors;

            if (!host.HighContrast || palette == null || !palette.IsComplete)
            {
                Release(host);

                return false;
            }

            host.SetToken(SURFACE, palette.Control);
            host.SetToken(SURFACE_OVER, palette.Control);
            host.SetToken(SURFACE_DOWN, palette.Control);
            host.SetToken(SURFACE_OFF, palette.Surface);
            host.SetToken(FIELD, palette.Surface);
            host.SetToken(PANEL, palette.Surface);
            host.SetToken(LINE, palette.ControlText);
            host.SetToken(LINE_OFF, palette.Disabled);
            host.SetToken(TEXT, palette.ControlText);
            host.SetToken(TEXT_OFF, palette.Disabled);
            host.SetToken(INVERSE, palette.ControlText);
            host.SetToken(INVERSE_TEXT, palette.Control);
            host.SetToken(FOCUS, palette.Accent);
            host.SetToken(FOCUS_SOFT, palette.Accent);

            return true;
        }

        private static void Release(IElementHost host)
        {
            for (int index = 0; index < Names.Length; index++)
            {
                host.ResetToken(Names[index]);
            }
        }
    }
}
