using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Pistol hold for the Ranged stance (humanoid animation pass, Phase 2), driven by
    /// <see cref="PistolStanceAnimation"/>: a pistol leg tree blended against the rifle tree, the pack's
    /// idle as the Upper Body pose, and two arm-only Off Hand layers for a full off-hand.
    /// </summary>
    public static partial class HumanoidLocomotionBlendSetup
    {
        private const string PistolTreeName = "Pistol Locomotion 2D";
        private const string RangedHoldTreeName = "Ranged Hold Blend";
        private const string MaskFolder = "Assets/Content/WorldObjects/Entities/Humanoids/Human";

        private static BlendTree BuildPistolBlendTree(
            AnimatorController controller, System.Func<Vector2, float> targetWorldSpeed, float humanScale)
        {
            (string File, string ClipName, Vector2 Pos)[] entries = PistolStanceAnimation.Locomotion
                .Select(clip => (clip.File, clip.ClipName, clip.Position))
                .ToArray();
            BlendTree tree = BuildBlendTree(
                controller, PistolTreeName, $"{AnimationsRoot}/{PistolStanceAnimation.Folder}", entries,
                BlendTreeType.FreeformDirectional2D, targetWorldSpeed, humanScale);
            if (tree != null)
            {
                tree.blendParameter = PistolStanceAnimation.StrafeParameter;
            }

            return tree;
        }

        /// <summary>
        /// Ranged Locomotion plays this: the rifle and pistol leg trees on PistolHold (0 → 1), so swapping
        /// guns blends the legs instead of needing a second stance state and its transitions.
        /// </summary>
        private static BlendTree BuildRangedHoldBlend(AnimatorController controller, BlendTree rifle, BlendTree pistol)
        {
            BlendTree tree = new BlendTree
            {
                name = RangedHoldTreeName,
                blendType = BlendTreeType.Simple1D,
                blendParameter = PistolStanceAnimation.HoldParameter,
                useAutomaticThresholds = false,
            };
            tree.AddChild(rifle, 0f);
            tree.AddChild(pistol, 1f);
            AssetDatabase.AddObjectToAsset(tree, controller);
            return tree;
        }

        /// <summary>
        /// Upper Body rest pose with a pistol. Every way into Rifle Aim Idle is gated on PistolHold &lt; 0.5
        /// and mirrored here with &gt; 0.5, and the two idles cross-fade when the held gun changes.
        /// </summary>
        private static void WirePistolAimIdle(
            AnimatorStateMachine upper,
            AnimatorState rifleAimIdle,
            AnimatorState holdDefault,
            AnimatorState holdItem,
            AnimatorState holdWeapon)
        {
            AnimationClip idleClip = LoadPackClip(
                $"{AnimationsRoot}/{PistolStanceAnimation.Folder}/{PistolStanceAnimation.Idle.File}",
                PistolStanceAnimation.Idle.ClipName);
            AnimatorState pistolIdle = FindOrCreateState(upper, PistolStanceAnimation.AimIdleState, new Vector3(300, 460, 0));
            if (idleClip != null)
            {
                pistolIdle.motion = idleClip;
                pistolIdle.writeDefaultValues = true;
            }
            else
            {
                Debug.LogError($"[HumanoidLocomotionBlendSetup] Missing clip '{PistolStanceAnimation.Idle.ClipName}'");
            }

            // The clips hold the gun in the right hand; a left-hand pistol mirrors the whole upper body.
            EnableMirrorParameter(pistolIdle, "MirrorUpperBody");
            ClearTransitions(pistolIdle);

            foreach (AnimatorState hold in new[] { holdDefault, holdItem, holdWeapon })
            {
                if (hold == null)
                {
                    continue;
                }

                SetPistolGate(hold, rifleAimIdle, pistol: false);
                EnsureCombatStanceTransition(hold, pistolIdle, combatStance: 2, duration: 0.1f);
                SetPistolGate(hold, pistolIdle, pistol: true);
            }

            if (holdDefault != null)
            {
                EnsureCombatStanceTransition(pistolIdle, holdDefault, combatStance: 0, duration: 0.1f);
                EnsureCombatStanceTransition(pistolIdle, holdDefault, combatStance: 1, duration: 0.1f);
            }

            if (rifleAimIdle != null)
            {
                AddPistolSwap(rifleAimIdle, pistolIdle, pistol: true);
                AddPistolSwap(pistolIdle, rifleAimIdle, pistol: false);
            }
        }

        /// <summary>Adds a PistolHold condition to every transition from <paramref name="from"/> to <paramref name="to"/>.</summary>
        private static void SetPistolGate(AnimatorState from, AnimatorState to, bool pistol)
        {
            if (to == null)
            {
                return;
            }

            foreach (AnimatorStateTransition transition in from.transitions)
            {
                if (transition.destinationState != to)
                {
                    continue;
                }

                transition.conditions = transition.conditions
                    .Where(c => c.parameter != PistolStanceAnimation.HoldParameter)
                    .Append(PistolCondition(pistol))
                    .ToArray();
            }
        }

        private static void AddPistolSwap(AnimatorState from, AnimatorState to, bool pistol)
        {
            foreach (AnimatorStateTransition existing in from.transitions.Where(t => t.destinationState == to).ToArray())
            {
                if (existing.conditions.Any(c => c.parameter == PistolStanceAnimation.HoldParameter))
                {
                    from.RemoveTransition(existing);
                }
            }

            AnimatorStateTransition created = from.AddTransition(to);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = PistolStanceAnimation.BlendSeconds;
            created.AddCondition(AnimatorConditionMode.Equals, 2, "CombatStance");
            created.conditions = created.conditions.Append(PistolCondition(pistol)).ToArray();
        }

        private static AnimatorCondition PistolCondition(bool pistol) => new()
        {
            parameter = PistolStanceAnimation.HoldParameter,
            mode = pistol ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less,
            threshold = 0.5f,
        };

        /// <summary>
        /// Appends the Off Hand Left/Right layers (after Full Body Override, so the orchestrator's fixed
        /// indices 1–3 hold). Each plays the peaceful idle through an arm-only mask, so the arm hangs
        /// relaxed with its item; the orchestrator weights the one opposite the gun when the off-hand is full.
        /// </summary>
        /// <remarks>
        /// Not the Upper Body "Hold Item" motion: HumanHoldItem (and Hold Default / Hold Weapon) are generic
        /// transform clips, which pose nothing on the humanoid rig, so a mask or mirror over them does nothing.
        /// </remarks>
        private static void WireOffHandLayers(AnimatorController controller)
        {
            AnimationClip idle = LoadPackClip($"{LocomotionPack}/idle.fbx", "Mix_Idle");
            if (idle == null)
            {
                Debug.LogError("[HumanoidLocomotionBlendSetup] Missing Mix_Idle; Off Hand layers skipped.");
                return;
            }

            WireOffHandLayer(controller, PistolStanceAnimation.OffHandLeftLayer, "HumanoidOffHandLeftMask",
                AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.LeftHandIK, idle);
            WireOffHandLayer(controller, PistolStanceAnimation.OffHandRightLayer, "HumanoidOffHandRightMask",
                AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.RightFingers, AvatarMaskBodyPart.RightHandIK, idle);
        }

        private static void WireOffHandLayer(
            AnimatorController controller,
            string layerName,
            string maskName,
            AvatarMaskBodyPart arm,
            AvatarMaskBodyPart fingers,
            AvatarMaskBodyPart handIk,
            Motion motion)
        {
            AvatarMask mask = EnsureArmMask(maskName, arm, fingers, handIk);
            if (controller.layers.All(l => l.name != layerName))
            {
                controller.AddLayer(layerName);
            }

            AnimatorControllerLayer[] layers = controller.layers;
            int index = System.Array.FindIndex(layers, l => l.name == layerName);
            layers[index].avatarMask = mask;
            layers[index].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[index].defaultWeight = 0f;
            layers[index].iKPass = false;
            controller.layers = layers;

            AnimatorStateMachine machine = controller.layers[index].stateMachine;
            AnimatorState state = FindOrCreateState(machine, PistolStanceAnimation.OffHandState, new Vector3(300, 0, 0));
            state.motion = motion;
            state.mirror = false;
            state.mirrorParameterActive = false;
            state.writeDefaultValues = false;
            machine.defaultState = state;
        }

        private static AvatarMask EnsureArmMask(string maskName, params AvatarMaskBodyPart[] parts)
        {
            string path = $"{MaskFolder}/{maskName}.mask";
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (mask == null)
            {
                mask = new AvatarMask { name = maskName };
                AssetDatabase.CreateAsset(mask, path);
            }

            for (AvatarMaskBodyPart part = 0; part < AvatarMaskBodyPart.LastBodyPart; part++)
            {
                mask.SetHumanoidBodyPartActive(part, parts.Contains(part));
            }

            EditorUtility.SetDirty(mask);
            return mask;
        }
    }
}
