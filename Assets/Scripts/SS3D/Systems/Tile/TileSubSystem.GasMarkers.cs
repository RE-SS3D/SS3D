using FishNet.Object;
using SS3D.Systems.Tile.GasMarkers;
using System.Linq;

namespace SS3D.Systems.Tile
{
    public partial class TileSubSystem
    {
        private readonly GasMarkerRegistry _gasMarkers = new();

        public GasMarkerRegistry GasMarkers => _gasMarkers;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _gasMarkers.Changed += SyncGasMarkersToClients;
            SyncGasMarkersToClients();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _gasMarkers.Changed -= SyncGasMarkersToClients;
        }

        private void SyncGasMarkersToClients()
        {
            RpcSyncGasMarkers(_gasMarkers.Records.ToArray());
        }

        [ObserversRpc(BufferLast = true)]
        private void RpcSyncGasMarkers(GasMarkerRecord[] records)
        {
            if (!IsServer)
                _gasMarkers.ReplaceAll(records);
        }
    }
}
