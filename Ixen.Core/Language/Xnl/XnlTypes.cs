using Ixen.Core.Visual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ixen.Core.Language.Xnl
{
    internal static class XnlTypes
    {
        internal const string CLASS_PROPERTY = "class";
        internal const string SLOT_PROPERTY = "slot";

        internal static readonly string[] UniversalProperties = { CLASS_PROPERTY, SLOT_PROPERTY };

        private static readonly Dictionary<string, Type> _byName = BuildTypes();
        private static readonly string[] _names = _byName.Keys.OrderBy(n => n, StringComparer.Ordinal).ToArray();

        internal static IReadOnlyList<string> Names => _names;

        internal static Type Find(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return typeof(VisualElement);
            }

            return _byName.TryGetValue(name, out Type type) ? type : null;
        }

        internal static IReadOnlyList<string> PropertiesOf(Type type)
        {
            var names = new List<string>(UniversalProperties);

            if (type != null)
            {
                foreach (PropertyInfo property in Settable(type))
                {
                    names.Add(ToXnlName(property.Name));
                }

                var events = new HashSet<string>(StringComparer.Ordinal);

                foreach (EventInfo handler in type.GetEvents(BindingFlags.Public | BindingFlags.Instance))
                {
                    events.Add(handler.Name);
                    names.Add(ToXnlName(handler.Name));
                }

                foreach (string alias in XnlEvents.Aliases)
                {
                    if (events.Contains(XnlEvents.Resolve(alias, null)))
                    {
                        names.Add(alias);
                    }
                }
            }

            names.Sort(StringComparer.Ordinal);
            return names;
        }

        internal static IReadOnlyList<string> ValuesOf(Type type, string xnlName)
        {
            if (type == null || string.IsNullOrEmpty(xnlName))
            {
                return new string[0];
            }

            PropertyInfo property = Settable(type)
                .FirstOrDefault(p => ToXnlName(p.Name) == xnlName);

            if (property == null)
            {
                return new string[0];
            }

            Type valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (valueType == typeof(bool))
            {
                return new[] { "false", "true" };
            }

            return valueType.IsEnum ? Enum.GetNames(valueType) : new string[0];
        }

        internal static string ToXnlName(string propertyName)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < propertyName.Length; i++)
            {
                char c = propertyName[i];

                if (char.IsUpper(c) && i > 0)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }

        private static IEnumerable<PropertyInfo> Settable(Type type)
        {
            return type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
                .Where(p => IsWritableFromXnl(p.PropertyType));
        }

        private static bool IsWritableFromXnl(Type type)
        {
            Type target = Nullable.GetUnderlyingType(type) ?? type;

            if (target.IsEnum)
            {
                return true;
            }

            switch (Type.GetTypeCode(target))
            {
                case TypeCode.String:
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;

                default:
                    return false;
            }
        }

        private static Dictionary<string, Type> BuildTypes()
        {
            var types = new Dictionary<string, Type>();

            foreach (Type type in typeof(VisualElement).Assembly.GetTypes())
            {
                if (!type.IsPublic || type.IsAbstract || !typeof(VisualElement).IsAssignableFrom(type))
                {
                    continue;
                }

                if (type.Namespace != "Ixen.Core.Visual" && type.Namespace != "Ixen.Core")
                {
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                types[type.Name] = type;
            }

            return types;
        }
    }
}
