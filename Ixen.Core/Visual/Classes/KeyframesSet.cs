using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    public class Keyframe
    {
        public float Offset { get; set; }
        public List<StyleDescriptor> Styles { get; set; }

        public Keyframe()
        {
            Styles = new List<StyleDescriptor>();
        }

        public Keyframe(float offset, List<StyleDescriptor> styles)
        {
            Offset = offset;
            Styles = styles ?? new List<StyleDescriptor>();
        }
    }

    internal struct ColorStop
    {
        internal float Offset;
        internal Color Value;
    }

    internal struct SizeStop
    {
        internal float Offset;
        internal SizeUnit Unit;
        internal float Value;
    }

    internal struct TransformStop
    {
        internal float Offset;
        internal TransformStyleDescriptor Value;
    }

    public class KeyframesSet
    {
        private Dictionary<string, ColorStop[]> _colorTracks;
        private Dictionary<string, SizeStop[]> _sizeTracks;
        private Dictionary<string, TransformStop[]> _transformTracks;
        private List<string> _properties;

        public string Name { get; set; }
        public List<Keyframe> Frames { get; set; }

        public KeyframesSet()
        {
            Frames = new List<Keyframe>();
        }

        public KeyframesSet(string name, List<Keyframe> frames)
        {
            Name = name;
            Frames = frames ?? new List<Keyframe>();
        }

        internal IReadOnlyList<string> Properties
        {
            get
            {
                Prepare();
                return _properties;
            }
        }

        internal ColorStop[] ColorTrack(string identifier)
        {
            Prepare();
            return _colorTracks.TryGetValue(identifier, out ColorStop[] track) ? track : null;
        }

        internal SizeStop[] SizeTrack(string identifier)
        {
            Prepare();
            return _sizeTracks.TryGetValue(identifier, out SizeStop[] track) ? track : null;
        }

        internal TransformStop[] TransformTrack(string identifier)
        {
            Prepare();
            return _transformTracks.TryGetValue(identifier, out TransformStop[] track) ? track : null;
        }

        internal static bool IsTransformProperty(string identifier)
            => identifier == StyleIdentifier.TRANSFORM;

        internal static bool IsColorProperty(string identifier)
            => identifier == StyleIdentifier.BACKGROUND
                || identifier == StyleIdentifier.COLOR
                || identifier == StyleIdentifier.BORDER;

        internal static bool IsSizeProperty(string identifier)
            => identifier == StyleIdentifier.WIDTH
                || identifier == StyleIdentifier.HEIGHT
                || identifier == StyleIdentifier.LEFT
                || identifier == StyleIdentifier.TOP
                || identifier == StyleIdentifier.RIGHT
                || identifier == StyleIdentifier.BOTTOM;

        private void Prepare()
        {
            if (_properties != null)
            {
                return;
            }

            _properties = new List<string>();
            var colors = new Dictionary<string, List<ColorStop>>();
            var sizes = new Dictionary<string, List<SizeStop>>();
            var transforms = new Dictionary<string, List<TransformStop>>();

            Frames.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            foreach (Keyframe frame in Frames)
            {
                foreach (StyleDescriptor descriptor in frame.Styles)
                {
                    if (descriptor == null)
                    {
                        continue;
                    }

                    string identifier = descriptor.Identifier;

                    if (IsColorProperty(identifier))
                    {
                        string text = ColorTextOf(descriptor);

                        if (text == null)
                        {
                            continue;
                        }

                        Track(colors, identifier).Add(new ColorStop
                        {
                            Offset = frame.Offset,
                            Value = new Color(text)
                        });

                        continue;
                    }

                    if (IsSizeProperty(identifier) && descriptor is SizeStyleDescriptor size
                        && size.Offset == 0)
                    {
                        Track(sizes, identifier).Add(new SizeStop
                        {
                            Offset = frame.Offset,
                            Unit = size.Unit,
                            Value = size.Value
                        });

                        continue;
                    }

                    if (IsTransformProperty(identifier) && descriptor is TransformStyleDescriptor transform)
                    {
                        Track(transforms, identifier).Add(new TransformStop
                        {
                            Offset = frame.Offset,
                            Value = transform
                        });
                    }
                }
            }

            _colorTracks = new Dictionary<string, ColorStop[]>();
            _sizeTracks = new Dictionary<string, SizeStop[]>();
            _transformTracks = new Dictionary<string, TransformStop[]>();

            foreach (KeyValuePair<string, List<ColorStop>> entry in colors)
            {
                if (entry.Value.Count < 2)
                {
                    continue;
                }

                _colorTracks[entry.Key] = entry.Value.ToArray();
                _properties.Add(entry.Key);
            }

            foreach (KeyValuePair<string, List<SizeStop>> entry in sizes)
            {
                if (entry.Value.Count < 2)
                {
                    continue;
                }

                _sizeTracks[entry.Key] = entry.Value.ToArray();
                _properties.Add(entry.Key);
            }

            foreach (KeyValuePair<string, List<TransformStop>> entry in transforms)
            {
                if (entry.Value.Count < 2)
                {
                    continue;
                }

                _transformTracks[entry.Key] = entry.Value.ToArray();
                _properties.Add(entry.Key);
            }
        }

        private static List<TStop> Track<TStop>(Dictionary<string, List<TStop>> tracks, string identifier)
        {
            if (!tracks.TryGetValue(identifier, out List<TStop> track))
            {
                track = new List<TStop>();
                tracks[identifier] = track;
            }

            return track;
        }

        private static string ColorTextOf(StyleDescriptor descriptor)
        {
            if (descriptor is BackgroundStyleDescriptor background)
            {
                return background.Color;
            }

            if (descriptor is ColorStyleDescriptor color)
            {
                return color.Value;
            }

            if (descriptor is BorderStyleDescriptor border)
            {
                return border.Color;
            }

            return null;
        }
    }
}
