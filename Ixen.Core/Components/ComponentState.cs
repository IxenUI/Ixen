using System.Collections.Generic;
using System.Globalization;

namespace Ixen.Core.Components
{
    public class ComponentState
    {
        private readonly Dictionary<string, string> _values;

        public ComponentState()
        {
            _values = new Dictionary<string, string>();
        }

        internal ComponentState(Dictionary<string, string> values)
        {
            _values = values;
        }

        public int Count => _values.Count;

        internal IEnumerable<KeyValuePair<string, string>> Values => _values;

        public bool Has(string key) => key != null && _values.ContainsKey(key);

        public void Set(string key, string value)
        {
            if (key == null)
            {
                return;
            }

            if (value == null)
            {
                _values.Remove(key);
                return;
            }

            _values[key] = value;
        }

        public void Set(string key, int value)
            => Set(key, value.ToString(CultureInfo.InvariantCulture));

        public void Set(string key, long value)
            => Set(key, value.ToString(CultureInfo.InvariantCulture));

        public void Set(string key, float value)
            => Set(key, value.ToString("R", CultureInfo.InvariantCulture));

        public void Set(string key, bool value)
            => Set(key, value ? "true" : "false");

        public string Get(string key, string fallback = null)
            => key != null && _values.TryGetValue(key, out string value) ? value : fallback;

        public int Get(string key, int fallback)
        {
            string value = Get(key);

            return value != null
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
        }

        public long Get(string key, long fallback)
        {
            string value = Get(key);

            return value != null
                && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
                ? parsed
                : fallback;
        }

        public float Get(string key, float fallback)
        {
            string value = Get(key);

            return value != null
                && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : fallback;
        }

        public bool Get(string key, bool fallback)
        {
            string value = Get(key);

            if (value == "true")
            {
                return true;
            }

            if (value == "false")
            {
                return false;
            }

            return fallback;
        }
    }
}
