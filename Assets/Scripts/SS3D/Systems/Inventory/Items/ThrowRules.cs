using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// How far a held item can be thrown and the launch that lands it on a point
    /// (Documents/architecture/2026-11_humanoid-animation-pass.md, Decision 8). Weights are the
    /// <c>ItemSizeWeightTable</c> scale (about kilograms): light items carry about 10 m, heavy ones
    /// about 3 m, and above <see cref="MaxThrowableWeight"/> an item cannot be thrown.
    /// </summary>
    public static class ThrowRules
    {
        /// <summary>At or below this weight an item flies the full <see cref="LightRange"/>.</summary>
        public const float LightWeight = 0.5f;
        public const float LightRange = 10f;

        /// <summary>At this weight an item flies <see cref="HeavyRange"/>; range falls linearly in between.</summary>
        public const float HeavyWeight = 5f;
        public const float HeavyRange = 3f;

        /// <summary>Gas tanks (5) still throw; a hardsuit (6) is the heaviest thing that does.</summary>
        public const float MaxThrowableWeight = 6f;

        /// <summary>Horizontal speed the flight time is sized from, and its bounds, in seconds.</summary>
        public const float FlightSpeed = 8f;
        public const float MinFlightSeconds = 0.3f;
        public const float MaxFlightSeconds = 1.2f;

        /// <summary>Release point: above the shoulder and in front of the body, as in the clips' release frames.</summary>
        public const float ReleaseHeight = 1.6f;
        public const float ReleaseForward = 0.45f;
        public const float ReleaseSide = 0.15f;

        public static bool IsThrowableWeight(float weight) => weight <= MaxThrowableWeight;

        /// <summary>Planar distance an item of <paramref name="weight"/> carries; 0 when it is too heavy to throw.</summary>
        public static float MaxRange(float weight)
        {
            if (!IsThrowableWeight(weight))
            {
                return 0f;
            }

            float t = Mathf.InverseLerp(LightWeight, HeavyWeight, weight);
            return Mathf.Lerp(LightRange, HeavyRange, t);
        }

        /// <summary>The item's own weight (times its stack) plus everything stored inside it.</summary>
        public static float ThrownWeight(Item item)
        {
            if (item == null)
            {
                return 0f;
            }

            float total = item.Weight * Mathf.Max(1, item.StackCount);
            foreach (AttachedContainer container in item.GetComponentsInChildren<AttachedContainer>())
            {
                total += container.Weight;
            }

            return total;
        }

        /// <summary>
        /// Where a throw aimed at <paramref name="target"/> lands. Within range it is the target itself;
        /// beyond it, the point at <paramref name="maxRange"/> along the same heading, at the thrower's
        /// feet height so it falls short onto the floor instead of hanging in the air.
        /// </summary>
        public static Vector3 ClampToRange(Vector3 throwerFeet, Vector3 target, float maxRange, out bool clamped)
        {
            Vector3 planar = target - throwerFeet;
            planar.y = 0f;
            float distance = planar.magnitude;
            clamped = distance > maxRange;
            if (!clamped)
            {
                return target;
            }

            Vector3 heading = distance > 0.0001f ? planar / distance : Vector3.forward;
            return throwerFeet + heading * maxRange;
        }

        public static float FlightSeconds(Vector3 origin, Vector3 target)
        {
            Vector3 planar = target - origin;
            planar.y = 0f;
            return Mathf.Clamp(planar.magnitude / FlightSpeed, MinFlightSeconds, MaxFlightSeconds);
        }

        /// <summary>Velocity that carries a body from <paramref name="origin"/> to <paramref name="target"/> in <paramref name="flightSeconds"/>.</summary>
        public static Vector3 LaunchVelocity(Vector3 origin, Vector3 target, Vector3 gravity, float flightSeconds)
        {
            float flight = Mathf.Max(0.01f, flightSeconds);
            return (target - origin) / flight - gravity * (0.5f * flight);
        }

        /// <summary>Release point for a thrower at <paramref name="throwerFeet"/> facing <paramref name="heading"/>.</summary>
        public static Vector3 ReleasePoint(Vector3 throwerFeet, Vector3 heading, bool leftHand)
        {
            heading.y = 0f;
            heading = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, heading);
            return throwerFeet + Vector3.up * ReleaseHeight + heading * ReleaseForward
                + right * (leftHand ? -ReleaseSide : ReleaseSide);
        }
    }

    /// <summary>
    /// On an item's components: told when it is thrown, and which clip throws it. The server calls
    /// <see cref="ServerOnThrown"/> right after the launch (the minibomb arms here).
    /// </summary>
    public interface IThrowReleaseHandler
    {
        /// <summary>True to throw with the grenade lob instead of the overhand throw.</summary>
        bool UsesGrenadeThrow { get; }

        void ServerOnThrown();
    }
}
