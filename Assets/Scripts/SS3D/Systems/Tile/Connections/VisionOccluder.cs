using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// What a wall tile blocks sight with (fov.md §2, systems/fov.md Sight blockers): boxes on the
    /// <see cref="LayerName"/> layer under a <see cref="HolderName"/> child, a centre block plus one arm
    /// toward each connected cardinal neighbour, as thick as the wall mesh at <see cref="EyeHeight"/>.
    /// Built by <c>VisionOccluderRecipe</c>; <see cref="TileAdjacencyView"/> switches the arms when it
    /// swaps the mesh. The holder is kept grid-aligned because the wall root turns with its mesh.
    /// </summary>
    public sealed class VisionOccluder : MonoBehaviour
    {
        /// <summary>Physics layer holding only sight blockers. It collides with nothing; only queries use it.</summary>
        public const string LayerName = "VisionOccluder";

        /// <summary>Name of the child that holds the occluder boxes, on walls and on door leaves.</summary>
        public const string HolderName = "VisionOccluder";

        /// <summary>Height of a standing human's <c>Entity.ViewPoint</c>: where the rays run and the boxes are measured.</summary>
        public const float EyeHeight = 1.5f;

        [SerializeField] private Transform _holder;
        [SerializeField] private GameObject _north;
        [SerializeField] private GameObject _east;
        [SerializeField] private GameObject _south;
        [SerializeField] private GameObject _west;

        private static int _layer = -1;

        public static int Layer => _layer >= 0 ? _layer : _layer = LayerMask.NameToLayer(LayerName);

        public static int Mask => 1 << Layer;

        public Transform Holder => _holder;

        /// <summary>Arm toward <paramref name="direction"/> (a cardinal), or null.</summary>
        public GameObject Arm(Direction direction) => direction switch
        {
            Direction.North => _north,
            Direction.East => _east,
            Direction.South => _south,
            Direction.West => _west,
            _ => null,
        };

        public void ApplyConnections(AdjacencyMap map)
        {
            if (_holder == null)
                return;

            _holder.rotation = Quaternion.identity;
            SetArm(_north, map.HasConnection(Direction.North));
            SetArm(_east, map.HasConnection(Direction.East));
            SetArm(_south, map.HasConnection(Direction.South));
            SetArm(_west, map.HasConnection(Direction.West));
        }

        private static void SetArm(GameObject arm, bool connected)
        {
            if (arm != null && arm.activeSelf != connected)
                arm.SetActive(connected);
        }
    }
}
