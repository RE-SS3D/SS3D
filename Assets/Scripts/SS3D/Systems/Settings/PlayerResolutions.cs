using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Settings
{
    /// <summary>Resolution dropdown rules (Decision 5): one entry per size, largest first; native when unset.</summary>
    public static class PlayerResolutions
    {
        public static Vector2Int[] Choices(IEnumerable<Vector2Int> modes) =>
            modes.Distinct().OrderByDescending(m => m.x * m.y).ThenByDescending(m => m.x).ToArray();

        public static Vector2Int Resolve(int width, int height, IReadOnlyList<Vector2Int> choices, Vector2Int native)
        {
            Vector2Int saved = new(width, height);
            return choices.Contains(saved) ? saved : native;
        }
    }

    /// <summary>
    /// The "Keep these settings?" countdown (Decision 3). <typeparamref name="T"/> is the last confirmed state; a
    /// second change while pending restarts the countdown but still reverts to that state.
    /// </summary>
    public sealed class DisplayChangeConfirmation<T>
    {
        public const float TimeoutSeconds = 15f;

        private float _deadline;

        public bool IsPending { get; private set; }

        public T Previous { get; private set; }

        public void Begin(T previous, float now)
        {
            if (!IsPending) Previous = previous;
            IsPending = true;
            _deadline = now + TimeoutSeconds;
        }

        public void Confirm() => IsPending = false;

        public bool ShouldRevert(float now) => IsPending && now >= _deadline;

        public float SecondsLeft(float now) => IsPending ? Mathf.Max(0f, _deadline - now) : 0f;
    }
}
