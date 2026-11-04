using System.Collections.Generic;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// What wind does to loose things. Picks the bodies in the strongest wind each atmos tick and pushes
    /// them every physics step, so a breach's traffic stays capped however many items a room holds. Item
    /// physics is server-only, so all of this runs on the server and reaches clients by NetworkTransform.
    /// </summary>
    public sealed class AtmosLooseBodies
    {
        /// <summary>
        /// Most bodies wind moves at once. Each costs a NetworkTransform stream to every client, and
        /// patches already use all but about 60 KB/s of the budget.
        /// </summary>
        public const int MaxPushed = 32;

        /// <summary>
        /// Slows a body with no gravity, per second. <see cref="Inventory.Items.Item"/> turns gravity off
        /// in space; this brings the item to rest there, so it stops sending traffic.
        /// </summary>
        public const float DriftDamping = 0.5f;

        private static readonly HashSet<Rigidbody> Bodies = new();
        private static readonly System.Comparison<Push> StrongestFirst = (a, b) => b.Speed.CompareTo(a.Speed);

        private readonly List<Push> _pushed = new();
        private readonly Dictionary<Rigidbody, float> _adrift = new();
        private readonly List<Rigidbody> _settled = new();

        public int PushedCount => _pushed.Count;
        public int AdriftCount => _adrift.Count;

        public static void Register(Rigidbody body) => Bodies.Add(body);
        public static void Unregister(Rigidbody body) => Bodies.Remove(body);

        public bool IsPushed(Rigidbody body)
        {
            foreach (Push push in _pushed)
                if (push.Body == body) return true;
            return false;
        }

        /// <summary>Once per atmos tick: chooses who the wind moves and who drifts.</summary>
        public void Tick(AtmosSimulation simulation)
        {
            _pushed.Clear();
            ReturnToDeck();

            foreach (Rigidbody body in Bodies)
            {
                if (body == null || body.isKinematic) continue;

                if (!body.useGravity)
                {
                    SetAdrift(body);
                    continue;
                }

                Vector3 position = body.position;
                // A sleeping cell has no flow, and most of a station is asleep. This is what keeps the
                // scan cheap: a tile lookup per body instead of a blended wind sample.
                var coord = new TileCoord(simulation.MapId, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
                if (!simulation.TryGetCellIndex(coord, out int index) || !simulation.CellMeta[index].IsSimulated) continue;

                Vector3 wind = AtmosWind.SampleAt(simulation, position);
                float speed = wind.magnitude;
                if (speed >= AtmosWind.MinSpeed) _pushed.Add(new Push(body, wind, speed));
            }

            if (_pushed.Count <= MaxPushed) return;
            _pushed.Sort(StrongestFirst);
            _pushed.RemoveRange(MaxPushed, _pushed.Count - MaxPushed);
        }

        /// <summary>Once per physics step.</summary>
        public void ApplyForces()
        {
            foreach (Push push in _pushed)
            {
                if (push.Body == null || push.Body.isKinematic) continue;
                push.Body.AddForce(
                    AtmosWind.LooseBodyAcceleration(push.Wind, push.Body.linearVelocity, push.Body.mass),
                    ForceMode.Acceleration);
            }
        }

        private void SetAdrift(Rigidbody body)
        {
            if (!_adrift.TryAdd(body, body.linearDamping)) return;
            body.linearDamping = Mathf.Max(body.linearDamping, DriftDamping);
        }

        private void ReturnToDeck()
        {
            foreach (KeyValuePair<Rigidbody, float> adrift in _adrift)
            {
                if (adrift.Key == null)
                {
                    _settled.Add(adrift.Key);
                }
                else if (adrift.Key.useGravity)
                {
                    adrift.Key.linearDamping = adrift.Value;
                    _settled.Add(adrift.Key);
                }
            }

            foreach (Rigidbody body in _settled) _adrift.Remove(body);
            _settled.Clear();
        }

        private readonly struct Push
        {
            public readonly Rigidbody Body;
            public readonly Vector3 Wind;
            public readonly float Speed;

            public Push(Rigidbody body, Vector3 wind, float speed)
            {
                Body = body;
                Wind = wind;
                Speed = speed;
            }
        }
    }
}
