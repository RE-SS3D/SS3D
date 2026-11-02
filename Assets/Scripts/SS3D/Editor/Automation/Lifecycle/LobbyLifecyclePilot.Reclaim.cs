using System.Linq;
using FishNet;
using FishNet.Component.Animating;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.Automation
{
    public static partial class LobbyLifecyclePilot
    {
        private const string CriticalReclaimMode = "critical-reclaim";
        private static Human _reclaimBody;
        private static bool _reclaimAnimatorInitiallyEnabled;

        [MenuItem("SS3D/Testing/Run Critical Reclaim Regressions")]
        public static void RunCriticalReclaimRegressions() => Start(CriticalReclaimMode);

        private static bool ReclaimBonesAreKinematic(bool value) => _reclaimBody
            .GetComponentsInChildren<RagdollPart>(true).All(part => part.GetComponent<Rigidbody>().isKinematic == value);

        private static Step[] BuildCriticalReclaimSteps() => BuildSandboxSteps().Take(4).Concat(new Step[]
        {
            new Step
            {
                Name = "Critical owner has active ragdoll physics",
                Enter = () =>
                {
                    _reclaimBody = (Human)SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local());
                    _reclaimAnimatorInitiallyEnabled = _reclaimBody.GetComponent<NetworkAnimator>().enabled;
                    _reclaimBody.GetComponent<HumanHealthController>().TickHealth(
                        toxinIntake: HealthConstants.CriticalToxinConcentration + 5f);
                    Require(ReclaimBonesAreKinematic(false), "Collapsed owner simulates its bones");
                    Settle(0.5);
                },
                Until = Settled,
            },
            new Step
            {
                Name = "Losing ownership relinquishes ragdoll physics",
                Enter = () =>
                {
                    _reclaimBody.RemoveOwnership();
                    _reclaimBody.Mind.RemoveOwnership();
                    Settle(0.5);
                },
                Until = () => Settled() && !_reclaimBody.IsOwner,
                Verify = () =>
                {
                    Require(ReclaimBonesAreKinematic(true), "Observer bones stay kinematic after ownership loss");
                    Require(!_reclaimBody.GetComponent<Animator>().enabled, "Collapsed observer does not resume animation");
                },
            },
            new Step
            {
                Name = "Reclaim reapplies the unchanged critical presentation",
                Enter = () =>
                {
                    Require(SubSystems.Get<EntitySubSystem>().TryReclaimEntity(Local(), InstanceFinder.ClientManager.Connection),
                        "Returning player reclaims the existing body");
                    Settle(0.5);
                },
                Until = () => Settled() && _reclaimBody.IsOwner,
                Verify = () =>
                {
                    Require(SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local()) == _reclaimBody,
                        "Reclaim preserves the body identity");
                    Require(_reclaimBody.GetComponent<Ragdoll>().Presentation == BodyPresentationState.Collapsed,
                        "Reclaim preserves critical collapse");
                    Require(ReclaimBonesAreKinematic(false), "Reclaimed owner resumes ragdoll physics");
                },
            },
            new Step
            {
                Name = "Network startup after collapse preserves original animation settings",
                Timeout = 15,
                Enter = () =>
                {
                    // Reproduce initial SyncVar collapse preceding OnStartNetwork on a late join.
                    _reclaimBody.GetComponent<Ragdoll>().OnStartNetwork();
                    Require(!_reclaimBody.GetComponent<NetworkAnimator>().enabled, "Network animation stays suppressed while critical");
                    _reclaimBody.GetComponent<HumanHealthController>().RestoreSystemicPools();
                    Settle(3);
                },
                Until = () => Settled() && _reclaimBody.GetComponent<HumanoidBodyStateMachine>().enabled,
                Verify = () =>
                {
                    Require(_reclaimBody.GetComponent<Animator>().enabled, "Recovery enables the Animator");
                    Require(_reclaimBody.GetComponent<NetworkAnimator>().enabled == _reclaimAnimatorInitiallyEnabled,
                        "Recovery restores the original NetworkAnimator state");
                    Require(_reclaimBody.GetComponent<HumanoidBodyStateMachine>().enabled,
                        "Recovery restores body animation state updates");
                    Require(ReclaimBonesAreKinematic(true), "Recovered bones return to animator control");
                },
            },
        }).ToArray();
    }
}
