using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.IO;
using UIKit;

namespace Ixen.View.iOS
{
    public partial class IxenView
    {
        private const string DROP_FOLDER = "ixen-drops";

        private DropTarget _dropTarget;

        private void AttachDrop()
        {
            _dropTarget = new DropTarget(this);
            AddInteraction(new UIDropInteraction(_dropTarget));
        }

        private void Deliver(CGPoint point, string[] paths)
        {
            if (paths.Length == 0)
            {
                return;
            }

            float scale = DeviceScale;

            _host.PointerDrop((float)point.X * scale, (float)point.Y * scale, paths);
        }

        private static string DropFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), DROP_FOLDER, Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(folder);

            return folder;
        }

        private sealed class DropTarget : NSObject, IUIDropInteractionDelegate
        {
            private readonly IxenView _view;

            internal DropTarget(IxenView view)
            {
                _view = view;
            }

            [Export("dropInteraction:canHandleSession:")]
            public bool CanHandleSession(UIDropInteraction interaction, IUIDropSession session)
            {
                return session.Items.Length > 0 && _view._host.Surface.AcceptsDrops;
            }

            [Export("dropInteraction:sessionDidUpdate:")]
            public UIDropProposal SessionDidUpdate(UIDropInteraction interaction, IUIDropSession session)
            {
                return new UIDropProposal(_view._host.Surface.AcceptsDrops
                    ? UIDropOperation.Copy
                    : UIDropOperation.Forbidden);
            }

            [Export("dropInteraction:performDrop:")]
            public void PerformDrop(UIDropInteraction interaction, IUIDropSession session)
            {
                CGPoint point = session.LocationInView(_view);
                UIDragItem[] items = session.Items;
                string folder = DropFolder();
                var paths = new string[items.Length];
                int remaining = items.Length;
                object gate = new object();

                for (int index = 0; index < items.Length; index++)
                {
                    int slot = index;

                    Load(items[index].ItemProvider, 0, folder, path =>
                    {
                        bool done;

                        lock (gate)
                        {
                            paths[slot] = path;
                            done = --remaining == 0;
                        }

                        if (done)
                        {
                            _view.BeginInvokeOnMainThread(() => _view.Deliver(point, Collected(paths)));
                        }
                    });
                }
            }

            private static string[] Collected(string[] paths)
            {
                var kept = new List<string>(paths.Length);

                foreach (string path in paths)
                {
                    if (path != null)
                    {
                        kept.Add(path);
                    }
                }

                return kept.ToArray();
            }

            private static void Load(NSItemProvider provider, int attempt, string folder, Action<string> loaded)
            {
                string[] types = provider?.RegisteredTypeIdentifiers ?? new string[0];

                if (attempt >= types.Length)
                {
                    loaded(null);
                    return;
                }

                provider.LoadFileRepresentation(types[attempt], (url, error) =>
                {
                    string copied = url == null ? null : Copy(url, provider.SuggestedName, folder);

                    if (copied == null)
                    {
                        Load(provider, attempt + 1, folder, loaded);
                        return;
                    }

                    loaded(copied);
                });
            }

            private static string Copy(NSUrl url, string suggested, string folder)
            {
                try
                {
                    string source = url.Path;
                    string extension = Path.GetExtension(source);
                    string name = string.IsNullOrWhiteSpace(suggested)
                        ? Path.GetFileName(source)
                        : Path.HasExtension(suggested) ? suggested : suggested + extension;
                    string target = Path.Combine(folder, Path.GetFileName(name));

                    if (File.Exists(target))
                    {
                        target = Path.Combine(folder, Guid.NewGuid().ToString("N") + extension);
                    }

                    File.Copy(source, target);

                    return target;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
