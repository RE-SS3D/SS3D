using SS3D.Systems.Furniture;
using System.Collections.Generic;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// One connected disposal pipe network: its segments and the bins/outlets reachable through it.
    /// In-transit capsules (design doc §5) are tracked centrally by <c>DisposalSubSystem</c> rather than
    /// per-network, so a capsule already past a cut segment survives that network's topology rebuild.
    /// </summary>
    public sealed class DisposalNetworkRecord
    {
        public DisposalNetworkId Id { get; }

        public HashSet<DisposalSegmentKey> Segments { get; } = new();

        public List<IDisposalElement> Terminals { get; } = new();

        public DisposalNetworkRecord(DisposalNetworkId id)
        {
            Id = id;
        }
    }
}
