using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Throws debris away from a fresh blast instead of letting it drop where it broke. The server announces a blast
    /// (<see cref="Tile.TileSubSystem"/> blast-started RPC) before it resolves the damage, so a client knows the
    /// epicenter when the breaks arrive; <see cref="DebrisSpriteVfx.Burst"/> then asks <see cref="TryGetPush"/>.
    /// Client presentation only.
    /// </summary>
    public static class BlastDebrisPush
    {
        /// <summary>Breaks this long after the announcement still count as part of the blast.</summary>
        public const float Window = 0.5f;

        /// <summary>Launch speed range, m/s: the epicenter's own tile gets the top, the blast's edge the bottom.</summary>
        public const float MinSpeed = 3.5f;
        public const float MaxSpeed = 9f;

        /// <summary>Cone half-angle for pushed debris, narrower than a free break so it reads as thrown.</summary>
        public const float Spread = 35f;

        /// <summary>Upward share of the launch direction, so fragments arc rather than skim the floor.</summary>
        public const float Lift = 0.55f;

        private const int Slots = 4;
        private static readonly Vector3[] Epicenters = new Vector3[Slots];
        private static readonly float[] Reaches = new float[Slots];
        private static readonly int[] Ids = new int[Slots];
        private static int _lastId;
        private static readonly float[] Times = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
        private static int _next;

        /// <summary>Records a blast about to land. <paramref name="reach"/> is how far its force carries, in metres.</summary>
        public static void Begin(Vector3 epicenter, float reach)
        {
            Epicenters[_next] = epicenter;
            Reaches[_next] = Mathf.Max(1f, reach);
            Times[_next] = Time.time;
            Ids[_next] = ++_lastId;
            _next = (_next + 1) % Slots;
        }

        /// <summary>
        /// The launch for debris at <paramref name="point"/> if a blast was announced within <see cref="Window"/> and
        /// reaches it: away from the nearest such epicenter, tilted up, faster closer in.
        /// </summary>
        public static bool TryGetPush(Vector3 point, out Vector3 direction, out float speed) =>
            TryGetPush(point, out direction, out speed, out _);

        /// <param name="blast">Which announced blast it is, so its debris can share one emitter.</param>
        public static bool TryGetPush(Vector3 point, out Vector3 direction, out float speed, out int blast)
        {
            blast = 0;
            direction = default;
            speed = 0f;
            float best = float.PositiveInfinity;
            for (int i = 0; i < Slots; i++)
            {
                if (Time.time - Times[i] > Window)
                    continue;

                float distance = Horizontal(point - Epicenters[i]).magnitude;
                if (distance > Reaches[i] + 1f || distance >= best)
                    continue;

                best = distance;
                blast = Ids[i];
                Launch(Epicenters[i], Reaches[i], point, out direction, out speed);
            }

            return best < float.PositiveInfinity;
        }

        /// <summary>Pure launch rule (EditMode-tested): outward and up, straight up on the epicenter itself.</summary>
        public static void Launch(Vector3 epicenter, float reach, Vector3 point, out Vector3 direction, out float speed)
        {
            Vector3 outward = Horizontal(point - epicenter);
            float distance = outward.magnitude;
            direction = distance < 0.25f
                ? Vector3.up
                : (outward / distance + Vector3.up * Lift).normalized;
            float closeness = 1f - Mathf.Clamp01(distance / Mathf.Max(1f, reach));
            speed = Mathf.Lerp(MinSpeed, MaxSpeed, closeness);
        }

        /// <summary>Forgets every blast; for tests and session teardown.</summary>
        public static void Clear()
        {
            for (int i = 0; i < Slots; i++)
                Times[i] = float.NegativeInfinity;
        }

        private static Vector3 Horizontal(Vector3 v) => new(v.x, 0f, v.z);
    }
}
