using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;

namespace SS3D.Systems.Entities.Observing
{
    /// <summary>
    /// Ghosts are invisible and undetectable to the living (observer.md §1, §3): a ghost is only
    /// spawned on connections that are themselves ghosts. The owner always passes in FishNet, so a
    /// ghost's own player still sees it. Server-side, so no ghost data ever reaches a living client.
    /// </summary>
    [CreateAssetMenu(menuName = "FishNet/SS3D/Observers/Ghost Condition", fileName = "GhostObserverCondition")]
    public sealed class GhostObserverCondition : ObserverCondition
    {
        public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
        {
            notProcessed = false;
            return ConnectionIsGhost(connection);
        }

        /// <summary>True when the connection owns a ghost body.</summary>
        public static bool ConnectionIsGhost(NetworkConnection connection)
        {
            if (connection == null)
            {
                return false;
            }

            foreach (NetworkObject nob in connection.Objects)
            {
                // A destroyed body can still be listed mid-transition; skip it rather than throw.
                if (nob != null && nob.TryGetComponent(out HumanoidGhostController _))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Timed, so death, respawn and reconnect transitions settle without every path having to
        /// remember a rebuild (MindSubSystem.SwapMinds still rebuilds at once on death).
        /// </summary>
        public override bool Timed() => true;

        public override ObserverCondition Clone() => Instantiate(this);
    }
}
