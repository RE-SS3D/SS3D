using FishNet.Connection;
using FishNet.Observing;
using SS3D.Core;
using UnityEngine;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Observer condition that gates network object visibility on late-join preload completion.
    /// Add this to the <c>ObserverManager</c>'s default conditions so that addressable objects
    /// are not spawned on a client until it has loaded all required prefabs.
    /// <para>
    /// Objects whose <see cref="NetworkObserver"/> is set to <see cref="NetworkObserver.ConditionOverrideType.IgnoreManager"/> (e.g. global objects
    /// like <see cref="NetworkBarrier"/>) are unaffected.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "FishNet/SS3D/Observers/Preload Condition", fileName = "PreloadCondition")]
    public sealed class PreloadCondition : ObserverCondition
    {
        public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed)
        {
            notProcessed = false;

            if (!SubSystems.TryGet(out AssetSubSystem assetSubSystem) || !assetSubSystem)
            {
                return false;
            }

            NetworkBarrier barrier = assetSubSystem.NetworkBarrier;

            return barrier && barrier.IsClientPreloadComplete(connection.ClientId);
        }

        public override ObserverConditionType GetConditionType() => ObserverConditionType.Normal;

        public override ObserverCondition Clone()
        {
            return CreateInstance<PreloadCondition>();
        }
    }
}