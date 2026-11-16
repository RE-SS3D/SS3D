using FishNet.Connection;
using FishNet.Observing;
using SS3D.Core;
using SS3D.Systems.Tile.MapEditor;
using UnityEngine;

namespace SS3D.Systems.Tile.Observing
{
    /// <summary>
    /// FishNet observer condition for underfloor tile NetworkObjects (plenum/wires/pipes/disposal).
    /// <list type="bullet">
    /// <item>Host + covered by floor/wall/door → fail (FishNet <c>SetRenderersVisible(false)</c>); plenums also need all eight surrounding cells covered.</item>
    /// <item>Remote clients → always pass (keep object spawned for client tile occupancy).</item>
    /// <item>Map Editor authoring → always pass.</item>
    /// </list>
    /// AND with <see cref="FishNet.Component.Observing.GridCondition"/> on underfloor prefabs.
    /// Remotes still need local presentational hide (<c>forceRenderingOff</c>) when covered.
    /// </summary>
    [CreateAssetMenu(menuName = "FishNet/Observers/SS3D Underfloor Cover Condition", fileName = "UnderfloorCoverCondition")]
    public sealed class UnderfloorCoverCondition : ObserverCondition
    {
        public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
        {
            notProcessed = false;

            if (NetworkObject == null)
                return true;

            if (SubSystems.TryGet(out MapEditorSubSystem editor) && editor.IsActive)
                return true;

            if (!NetworkObject.TryGetComponent(out PlacedTileObject placed) || placed.tileObjectSO == null)
                return true;

            if (!TileUnderfloorVisibility.IsUnderfloorLayer(placed.Layer))
                return true;

            // Remotes keep the NO for occupancy; host visibility is what we gate.
            bool isHostConnection = NetworkObject.IsServer
                && NetworkObject.NetworkManager != null
                && NetworkObject.NetworkManager.ClientManager != null
                && connection == NetworkObject.NetworkManager.ClientManager.Connection
                && connection.IsValid;

            if (!isHostConnection)
                return true;

            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null)
                return true;

            Vector3 world = NetworkObject.transform.position;
            world.y = 0f;
            if (TileUnderfloorVisibility.ShouldHideUnderfloor(tiles.CurrentMap, world, placed.Layer, mapEditorAuthoring: false))
                return false;

            Vector2Int origin = placed.WorldOrigin;
            Vector3 synced = new Vector3(origin.x, 0f, origin.y);
            return !TileUnderfloorVisibility.ShouldHideUnderfloor(
                tiles.CurrentMap, synced, placed.Layer, mapEditorAuthoring: false);
        }

        public override ObserverConditionType GetConditionType() => ObserverConditionType.Timed;

        public override ObserverCondition Clone()
        {
            UnderfloorCoverCondition copy = CreateInstance<UnderfloorCoverCondition>();
            return copy;
        }
    }
}
