using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    public class ClassesSet
    {
        public string Scope { get; set; }
        public List<StyleClass> Classes { get; set; }
        public List<KeyframesSet> Keyframes { get; set; }
        public Dictionary<string, string> Tokens { get; set; }
    }

    public static class StyleFormat
    {
        public const int VERSION = 1;
    }

    public class StyleSheet : ClassesSet
    {
        public StyleSheet()
        {
            Classes = new List<StyleClass>();
            Keyframes = new List<KeyframesSet>();
            Tokens = new Dictionary<string, string>();
        }

        public virtual int FormatVersion => StyleFormat.VERSION;

        protected void AddClass(StyleClass styleClass)
        {
            Classes.Add(styleClass);
        }

        protected void AddKeyframes(KeyframesSet keyframes)
        {
            Keyframes.Add(keyframes);
        }

        protected void AddToken(string name, string color)
        {
            Tokens[name] = color;
        }
    }
}
