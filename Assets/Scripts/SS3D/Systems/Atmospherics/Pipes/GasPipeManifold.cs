using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// A straight pipe on every line it spans, all one network: its front and back connect to
    /// whatever runs up to them on those lines, and it needs no pipes on its own tile.
    /// </summary>
    [RequireComponent(typeof(PlacedTileObject))]
    public sealed class GasPipeManifold : MonoBehaviour, IPipePortDevice
    {
        [SerializeField]
        private TileLayer[] _lines = System.Array.Empty<TileLayer>();

        private PlacedTileObject _placedObject;

        public PlacedTileObject PlacedObject =>
            _placedObject != null ? _placedObject : _placedObject = GetComponent<PlacedTileObject>();

        public TileLayer[] PortLines => _lines;

        public bool Spans(TileLayer line) => System.Array.IndexOf(_lines, line) >= 0;

        public bool HasPortToward(Direction direction) =>
            AtmosDevicePipeResolver.HasInlinePortToward(PlacedObject.Direction, direction);

        /// <summary>
        /// Lets recipes and tests set the lines; prefabs carry them serialized.
        /// </summary>
        public void SetLines(TileLayer[] lines) => _lines = lines;
    }
}
