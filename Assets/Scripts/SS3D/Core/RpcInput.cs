using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Server-side checks for numbers a client sends in a ServerRpc (tech-debt P1.5, networking §10). A modified client
    /// can send NaN, infinity or a point a kilometre away; every RPC that takes a vector or a float runs its arguments
    /// through here before using them.
    /// </summary>
    public static class RpcInput
    {
        /// <summary>No map comes close to this; a point further out is not a real click.</summary>
        public const float MaxHorizontalExtent = 4096f;

        /// <summary>A click can land a little under the deck (a sunk object) or above the wall tops (a tall machine).</summary>
        public const float VerticalSlack = 2f;

        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        /// <summary>Finite and inside the station's volume: <see cref="StationBounds"/> vertically, with slack.</summary>
        public static bool IsInStation(Vector3 value) =>
            IsFinite(value)
            && Mathf.Abs(value.x) <= MaxHorizontalExtent
            && Mathf.Abs(value.z) <= MaxHorizontalExtent
            && value.y >= StationBounds.FloorLimit - VerticalSlack
            && value.y <= StationBounds.EscapedAbove + VerticalSlack;

        /// <summary>A surface normal or direction: finite and not zero.</summary>
        public static bool IsDirection(Vector3 value) => IsFinite(value) && value.sqrMagnitude > 1e-8f;
    }
}
