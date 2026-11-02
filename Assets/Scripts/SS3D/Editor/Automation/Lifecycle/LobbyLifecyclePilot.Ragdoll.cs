using System.Linq;
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
        private const string RagdollSpaceMode = "ragdoll-space";
        private static Human _spaceBody;
        private static Vector3 _deckPosition;
        private static float _spaceHipsHeight;

        [MenuItem("SS3D/Testing/Run Ragdoll Space Regressions")]
        public static void RunRagdollSpaceRegressions() => Start(RagdollSpaceMode);

        private static Rigidbody[] RagdollBodies() => _spaceBody.GetComponentsInChildren<RagdollPart>(true)
            .Select(part => part.GetComponent<Rigidbody>()).ToArray();

        private static float HipsHeight() => _spaceBody.GetComponent<Animator>()
            .GetBoneTransform(HumanBodyBones.Hips).position.y;

        private static void RequireRagdollGravity(bool useGravity)
        {
            Rigidbody[] bodies = RagdollBodies();
            Require(bodies.Length > 0, "Body has ragdoll rigidbodies");
            Require(bodies.All(body => !body.isKinematic && body.useGravity == useGravity),
                useGravity ? "Supported ragdoll has dynamic bones with gravity" : "Spaced ragdoll has dynamic bones without gravity");
        }

        private static Step[] BuildRagdollSpaceSteps() => BuildSandboxSteps().Take(4).Concat(new Step[]
        {
            new Step
            {
                Name = "Critical collapse on the deck retains gravity",
                Enter = () =>
                {
                    _spaceBody = (Human)SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local());
                    _deckPosition = _spaceBody.transform.position;
                    _spaceBody.GetComponent<HumanHealthController>().TickHealth(
                        toxinIntake: HealthConstants.CriticalToxinConcentration + 5f);
                    Require(_spaceBody.GetComponent<Ragdoll>().Presentation == BodyPresentationState.Collapsed,
                        "Critical health collapses the body");
                    RequireRagdollGravity(true);
                    _spaceBody.GetComponent<HumanHealthController>().RestoreSystemicPools();
                    Settle(3);
                },
                Until = Settled,
            },
            new Step
            {
                Name = "Recover and move into open space",
                Enter = () =>
                {
                    Require(_spaceBody.GetComponent<Ragdoll>().Presentation == BodyPresentationState.Locomotion,
                        "Treatment restores locomotion");
                    CharacterController controller = _spaceBody.GetComponent<CharacterController>();
                    controller.enabled = false;
                    _spaceBody.transform.position = new Vector3(10000f, _deckPosition.y, 10000f);
                    controller.enabled = true;
                    _spaceBody.GetComponent<HumanoidBodyStateMachine>().SetFloating(true);
                    Settle(0.5);
                },
                Until = Settled,
                Verify = () => Require(HumanoidSpaceSupport.IsUnsupportedAt(_spaceBody.transform.position),
                    "Body is beyond the station's floor support"),
            },
            new Step
            {
                Name = "Critical collapse in space disables gravity",
                Enter = () =>
                {
                    _spaceBody.GetComponent<HumanHealthController>().TickHealth(
                        toxinIntake: HealthConstants.CriticalToxinConcentration + 5f);
                    RequireRagdollGravity(false);
                    _spaceHipsHeight = HipsHeight();
                    Settle(3);
                },
                Until = Settled,
                Verify = () => Require(Mathf.Abs(HipsHeight() - _spaceHipsHeight) < 1f,
                    "Critical ragdoll stays near deck height in open space"),
            },
            new Step
            {
                Name = "Death in space keeps the corpse weightless",
                Enter = () =>
                {
                    _spaceBody.Kill();
                    Require(_spaceBody.GetComponent<Ragdoll>().Presentation == BodyPresentationState.Dead,
                        "Death transfers ragdoll physics to the server");
                    RequireRagdollGravity(false);
                    _spaceHipsHeight = HipsHeight();
                    Settle(3);
                },
                Until = Settled,
                Verify = () => Require(Mathf.Abs(HipsHeight() - _spaceHipsHeight) < 1f,
                    "Unowned corpse stays near deck height in open space"),
            },
            new Step
            {
                Name = "Returning the corpse to the deck restores gravity",
                Enter = () =>
                {
                    _spaceBody.transform.position = _deckPosition;
                    Settle(0.5);
                },
                Until = Settled,
                Verify = () => RequireRagdollGravity(true),
            },
        }).ToArray();
    }
}
