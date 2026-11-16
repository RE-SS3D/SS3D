using System.Collections.Generic;
using SS3D.Systems.Area;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Marks a machine as humming while its <see cref="IAmbientLoopState"/> says it runs. Client-local
    /// and carries no <see cref="AudioSource"/>: <see cref="AmbientLoopSubSystem"/> lends one of its
    /// voices to the nearest few. Added by <c>AudioContentPrefabRecipes</c>.
    /// </summary>
    public sealed class AmbientLoopSource : MonoBehaviour
    {
        private static readonly List<AmbientLoopSource> s_sources = new();

        [SerializeField]
        private AmbientLoopKind _kind;

        private IAmbientLoopState _state;
        private IAmbientLoopArea _area;

        /// <summary>Every enabled source on this machine.</summary>
        public static IReadOnlyList<AmbientLoopSource> Sources => s_sources;

        public AmbientLoopKind Kind => _kind;

        public bool IsRunning => _state != null && _state.IsLoopRunning;

        /// <summary>The Area this machine serves, when it knows one (<see cref="IAmbientLoopArea"/>).</summary>
        public bool TryGetArea(out AreaId areaId)
        {
            areaId = default;
            return _area != null && _area.TryGetLoopArea(out areaId);
        }

        private void OnEnable()
        {
            _state ??= GetComponentInParent<IAmbientLoopState>();
            _area ??= GetComponentInParent<IAmbientLoopArea>();
            s_sources.Add(this);
        }

        private void OnDisable()
        {
            s_sources.Remove(this);
        }
    }
}
