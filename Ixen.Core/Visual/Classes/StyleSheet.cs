using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    public class ClassesSet
    {
        public string Scope { get; set; }
        public List<StyleClass> Classes { get; set; }
        public List<KeyframesSet> Keyframes { get; set; }
    }

    public class StyleSheet : ClassesSet
    {
        public StyleSheet()
        {
            Classes = new List<StyleClass>();
            Keyframes = new List<KeyframesSet>();
        }

        protected void AddClass(StyleClass styleClass)
        {
            Classes.Add(styleClass);
        }

        protected void AddKeyframes(KeyframesSet keyframes)
        {
            Keyframes.Add(keyframes);
        }
    }
}
