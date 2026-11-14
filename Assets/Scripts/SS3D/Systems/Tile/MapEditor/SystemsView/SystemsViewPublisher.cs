using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// Server: who is watching which systems view, what each of them was last sent, and which networks each traces
    /// (Documents/architecture/2026-11_map-editor-systems-views.md Q2). Pure C#; <c>MapEditorSystemsView</c> owns the
    /// RPCs and calls <see cref="Publish"/> and <see cref="PublishLive"/> on a timer. Clients without the editor open
    /// are never subscribed.
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

        /// <summary>
        /// Most networks one trace follows: as many as a style's <c>TraceTargets</c> may give for one object. Atmos gives
        /// at most four (a manifold's four lines; a filter or mixer has three sides); a style must stay within it.
        /// </summary>
        public const int MaxTracedNetworks = 4;

        /// <summary>Whether any subscriber traces a network, so the live poll has work.</summary>
        public bool HasTraces
        {
            get
            {
                foreach (Subscriber subscriber in _subscribers.Values)
                {
                    if (subscriber.Traced.Count > 0)
                        return true;
                }

                return false;
            }
        }

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

        /// <summary>The editor closed or the client left, which ends its trace too. Returns whether it was subscribed.</summary>
        public bool Unsubscribe(int clientId) => _subscribers.Remove(clientId);

        /// <summary>
        /// Starts, replaces or (with no ids) ends <paramref name="clientId"/>'s trace (Q2's live stream): the view ids of
        /// the networks a Select traced, in the client's order, under the client's own <paramref name="traceId"/>.
        /// Ids of 0 or less and repeats are dropped, and at most <see cref="MaxTracedNetworks"/> are kept. Refused (false)
        /// unless the client watches <paramref name="view"/>; switching views or unsubscribing ends the trace.
        /// </summary>
        public bool Trace(int clientId, SystemsViewKind view, int traceId, IReadOnlyList<int> viewIds)
        {
            if (!_subscribers.TryGetValue(clientId, out Subscriber subscriber) || subscriber.View != view || view == SystemsViewKind.Off)
                return false;

            subscriber.TraceId = traceId;
            subscriber.Traced.Clear();
            subscriber.LiveSent = null;
            if (viewIds == null)
                return true;

            foreach (int viewId in viewIds)
            {
                if (viewId > 0 && !subscriber.Traced.Contains(viewId) && subscriber.Traced.Count < MaxTracedNetworks)
                    subscriber.Traced.Add(viewId);
            }

            return true;
        }

        /// <summary>Ends <paramref name="clientId"/>'s trace, if it has one.</summary>
        public void ClearTrace(int clientId)
        {
            if (_subscribers.TryGetValue(clientId, out Subscriber subscriber))
            {
                subscriber.Traced.Clear();
                subscriber.LiveSent = null;
            }
        }

        /// <summary>What <paramref name="clientId"/> traces now, in its order; empty when nothing.</summary>
        public IReadOnlyList<int> TracedBy(int clientId) =>
            _subscribers.TryGetValue(clientId, out Subscriber subscriber) ? subscriber.Traced : Array.Empty<int>();

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

        /// <summary>
        /// Q2's live stream: reads every traced network from its source and hands each tracing client its numbers, but
        /// only when they differ from what that client was last sent. A network its source no longer knows (it went
        /// in an edit) leaves the trace; when none is left the client gets one empty message and the trace ends. Run it
        /// right after <see cref="Publish"/>, so view ids are those of the structure just sent.
        /// </summary>
        public void PublishLive(Action<int, SystemsViewLive> send)
        {
            if (send == null || _subscribers.Count == 0)
                return;

            _clientIds.Clear();
            _clientIds.AddRange(_subscribers.Keys);
            foreach (int clientId in _clientIds)
            {
                Subscriber subscriber = _subscribers[clientId];
                if (subscriber.Traced.Count == 0 || !_sources.TryGetValue(subscriber.View, out ISystemsViewSource source))
                    continue;

                List<SystemsViewLiveNetwork> networks = new(subscriber.Traced.Count);
                for (int i = subscriber.Traced.Count - 1; i >= 0; i--)
                {
                    if (source.ReadLive(subscriber.Traced[i], out SystemsViewLiveNetwork network))
                        networks.Insert(0, Normalized(network, subscriber.Traced[i]));
                    else
                        subscriber.Traced.RemoveAt(i);
                }

                SystemsViewLive live = new()
                {
                    View = subscriber.View,
                    TraceId = subscriber.TraceId,
                    Paused = source.SimulationPaused,
                    Networks = networks.ToArray(),
                };
                if (subscriber.LiveSent is { } sent && sent.Equals(live))
                    continue;

                subscriber.LiveSent = live;
                send(clientId, live);
                if (subscriber.Traced.Count == 0)
                    subscriber.LiveSent = null;
            }
        }

        private static SystemsViewLiveNetwork Normalized(SystemsViewLiveNetwork network, int viewId)
        {
            network.ViewId = viewId;
            network.Rows ??= Array.Empty<SystemsViewLiveRow>();
            network.Devices ??= Array.Empty<SystemsViewLiveDevice>();
            return network;
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
            /// <summary>The view ids this client traces, in its order; empty when it traces nothing.</summary>
            public readonly List<int> Traced = new(MaxTracedNetworks);
            public int TraceId;
            /// <summary>The live numbers last sent for the current trace; null before the first.</summary>
            public SystemsViewLive? LiveSent;

            public Subscriber(SystemsViewKind view) => View = view;
        }
    }
}
