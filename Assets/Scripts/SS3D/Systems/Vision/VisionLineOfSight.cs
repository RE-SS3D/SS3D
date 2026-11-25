using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// The one line of sight (fov.md §6, systems/fov.md Gameplay): blocked only by what blocks sight, the
    /// <see cref="VisionOccluder.LayerName"/> layer. Needs no rendering, so a server can ask it too.
    /// </summary>
    public static class VisionLineOfSight
    {
        public static bool HasSight(Vector3 from, Vector3 to) =>
            !Physics.Linecast(from, to, VisionOccluder.Mask, QueryTriggerInteraction.Ignore);
    }
}
