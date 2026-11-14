using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// Server: who is watching which systems view, and what each of them was last sent
    /// (Documents/architecture/2026-11_map-editor-systems-views.md Q2). Pure C#; <c>MapEditorSystemsView</c> owns the
    /// RPCs and calls <see cref="Publish"/> on a timer. Clients without the editor open are never subscribed.
    /// </summary>
    public sealed class SystemsViewPublisher
    {
        private readonly Dictionary<SystemsViewKind, ISystemsViewSource> _sources = new();
        private readonly Dictionary<SystemsViewKind, Built> _built = new();
        private readonly Dictionary<int, Subscriber> _subscribers = new();
        private readonly List<int> _clientIds = new();
        private int _buildStamp;
        private int _publishCount;

        /// <summary>
        /// Most bytes one send may take (<see cref="SystemsViewWire"/>). A bigger delta, such as a large station's first
        /// open, goes out over several polls, the way the atmos chunk patches keep under their budget: 16 patches of
        /// 3,084 bytes per 0.2 s tick. At the hub's 0.25 s poll this is about 190 KB/s at most, under the 300 KB/s
        /// per client those patches are held to.
        /// </summary>
        public int MaxBytesPerSend { get; set; } = 48 * 1024;

        public bool HasSubscribers => _subscribers.Count > 0;

        public void Register(ISystemsViewSource source)
        {
            if (source == null || source.View == SystemsViewKind.Off)
                throw new ArgumentException("A systems view source needs a view.", nameof(source));

            _sources[source.View] = source;
            _built.Remove(source.View);
        }

        public void Unregister(ISystemsViewSource source)
        {
            if (source == null || !_sources.TryGetValue(source.View, out ISystemsViewSource registered) || registered != source)
                return;

            _sources.Remove(source.View);
            _built.Remove(source.View);
        }

        /// <summary>The view <paramref name="clientId"/> watches, or <see cref="SystemsViewKind.Off"/>.</summary>
        public SystemsViewKind ViewOf(int clientId) =>
            _subscribers.TryGetValue(clientId, out Subscriber subscriber) ? subscriber.View : SystemsViewKind.Off;

        /// <summary>
        /// Starts or switches <paramref name="clientId"/>'s view; <see cref="SystemsViewKind.Off"/> unsubscribes. A new
        /// view starts from nothing sent, so the next <see cref="Publish"/> sends it a reset.
        /// </summary>
        public void Subscribe(int clientId, SystemsViewKind view)
        {
            if (view == SystemsViewKind.Off)
            {
                Unsubscribe(clientId);
                return;
            }

            if (_subscribers.TryGetValue(clientId, out Subscriber current) && current.View == view)
                return;

            _subscribers[clientId] = new Subscriber(view);
        }

        /// <summary>The editor closed or the client left. Returns whether it was subscribed.</summary>
        public bool Unsubscribe(int clientId) => _subscribers.Remove(clientId);

        public void Clear()
        {
            _subscribers.Clear();
            _built.Clear();
        }

        /// <summary>
        /// Rebuilds each watched view whose source moved, then hands each subscriber the delta between what it was
        /// last sent and the current structure. A subscriber with nothing new is sent nothing.
        /// </summary>
        public void Publish(Action<int, SystemsViewDelta> send)
        {
            if (send == null || _subscribers.Count == 0)
                return;

            _publishCount++;
            _clientIds.Clear();
            _clientIds.AddRange(_subscribers.Keys);
            foreach (int clientId in _clientIds)
            {
                Subscriber subscriber = _subscribers[clientId];
                Built built = Current(subscriber.View);
                if (subscriber.SentStamp == built.Stamp)
                    continue;

                SystemsViewDelta delta = SystemsViewStructure.Diff(subscriber.Sent, built.Structure);
                // Over budget: send a part now and leave the stamp, so the next poll diffs again and sends the rest.
                if (!SystemsViewWire.Limit(ref delta, MaxBytesPerSend))
                    subscriber.SentStamp = built.Stamp;
                if (delta.IsEmpty)
                    continue;

                subscriber.Sent ??= new SystemsViewStructure(subscriber.View);
                subscriber.Sent.Apply(delta);
                send(clientId, delta);
            }
        }

        private Built Current(SystemsViewKind view)
        {
            // A source's version may poll device state, so read it once per publish however many clients watch.
            if (_built.TryGetValue(view, out Built built) && built.CheckedAt == _publishCount)
                return built;

            bool hasSource = _sources.TryGetValue(view, out ISystemsViewSource source);
            int version = hasSource ? source.StructureVersion : 0;
            if (built != null && built.Version == version)
            {
                built.CheckedAt = _publishCount;
                return built;
            }

            // A view with no source yet (Phase 0) is an empty structure: the client still gets its reset.
            built = new Built(hasSource ? source.BuildStructure() ?? new SystemsViewStructure(view) : new SystemsViewStructure(view),
                version, ++_buildStamp) { CheckedAt = _publishCount };
            _built[view] = built;
            return built;
        }

        private sealed class Built
        {
            public readonly SystemsViewStructure Structure;
            public readonly int Version;
            public readonly int Stamp;
            public int CheckedAt;

            public Built(SystemsViewStructure structure, int version, int stamp)
            {
                Structure = structure;
                Version = version;
                Stamp = stamp;
            }
        }

        private sealed class Subscriber
        {
            public readonly SystemsViewKind View;
            /// <summary>What this client holds, built by applying every delta it was sent; null before the first.</summary>
            public SystemsViewStructure Sent;
            /// <summary>The build last compared against; 0 before the first.</summary>
            public int SentStamp;

            public Subscriber(SystemsViewKind view) => View = view;
        }
    }
}
