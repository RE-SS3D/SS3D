using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// A target whose reachable surface isn't where its colliders are, such as a wall mount inside
    /// its wall's solid. <see cref="Extensions.InteractionExtensions.IsReachUnobstructed"/> checks the
    /// path to the point returned here.
    /// </summary>
    public interface IReachSurface
    {
        /// <summary>The point a hand reaches for when it aims at <paramref name="point"/>.</summary>
        Vector3 GetReachPoint(Vector3 point);
    }
}
