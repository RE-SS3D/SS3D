using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Legacy wrapper — delegates to <see cref="AnimationOrchestrator"/>.
    /// Kept for prefab compatibility during migration.
    /// </summary>
    [RequireComponent(typeof(AnimationOrchestrator))]
    public class HumanoidAnimatorController : Actor
    {
        private AnimationOrchestrator _orchestrator;

        protected override void OnAwake()
        {
            base.OnAwake();
            _orchestrator = GetComponent<AnimationOrchestrator>();
        }
    }
}
