using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Rebuilds Base Layer locomotion into Peaceful / Melee / Ranged / Injured
    /// 2D locomotion blend trees (Peaceful/Injured Cartesian, Melee/Ranged Directional) switched by CombatStance and LimpSide.
    /// </summary>
    public static partial class HumanoidLocomotionBlendSetup
    {
        private const string ControllerPath =
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanCharacterAnimator.controller";
        private const string LocomotionPack = "Assets/Art/Animations/Locomotion Pack";
        private const string MeleePack = "Assets/Art/Animations/Pro Melee Axe Pack";
        private const string ShooterPack = "Assets/Art/Animations/Basic Shooter Pack";
        private const string InjuredPack = "Assets/Art/Animations/Male Injured Pack";

        // Soft blend into/out of hit flinch (Any State entry + exit-time return to loco).
        private const float FlinchEntryDuration = 0.2f;
        private const float FlinchExitDuration = 0.28f;
        private const float FlinchExitTime = 0.7f;

        private static readonly (string File, string ClipName, Vector2 Pos)[] PeacefulClips =
        {
            ("idle.fbx", "Mix_Idle", new Vector2(0f, 0f)),
            ("walking.fbx", "Mix_Walking", new Vector2(0f, 0.3f)),
            ("running.fbx", "Mix_Running", new Vector2(0f, 1f)),
            ("left strafe walking.fbx", "Mix_LeftStrafeWalking", new Vector2(-1f, 0.3f)),
            ("right strafe walking.fbx", "Mix_RightStrafeWalking", new Vector2(1f, 0.3f)),
            ("left strafe.fbx", "Mix_LeftStrafe", new Vector2(-1f, 1f)),
            ("right strafe.fbx", "Mix_RightStrafe", new Vector2(1f, 1f)),
            ("left strafe walking.fbx", "Mix_LeftStrafeWalking", new Vector2(-1f, 0f)),
            ("right strafe walking.fbx", "Mix_RightStrafeWalking", new Vector2(1f, 0f)),
        };

        // Melee and Ranged are FreeformDirectional2D (M1m): one sample per direction per gait
        // magnitude (walk 0.3, run 1.0) on the pure axes, idle at the centre. Diagonals blend the
        // two neighbouring directions with no idle weight. Do not add strafe clips at corner
        // positions like (±0.3, 0.3) — in a directional tree a corner means "diagonal", and a
        // pure-strafe clip there makes diagonal walking slide sideways.
        // Paths are relative to AnimationsRoot. Pro Melee Axe Pack has no run strafes, so the run
        // ring's lateral samples come from Pro Sword and Shield Pack (Mixamo "strafe (3)/(4)",
        // renamed run left/right). Melee keeps Upper Body at weight 1, which masks arms and spine,
        // so the shield-arm pose in those clips never shows; only legs and hips come through.
        private static readonly (string File, string ClipName, Vector2 Pos)[] MeleeClips =
        {
            ("Pro Melee Axe Pack/standing idle.fbx", "Mix_StandingIdle", new Vector2(0f, 0f)),
            ("Pro Melee Axe Pack/standing walk forward.fbx", "Mix_StandingWalkForward", new Vector2(0f, 0.3f)),
            ("Pro Melee Axe Pack/standing walk back.fbx", "Mix_StandingWalkBack", new Vector2(0f, -0.3f)),
            ("Pro Melee Axe Pack/standing walk left.fbx", "Mix_StandingWalkLeft", new Vector2(-0.3f, 0f)),
            ("Pro Melee Axe Pack/standing walk right.fbx", "Mix_StandingWalkRight", new Vector2(0.3f, 0f)),
            ("Pro Melee Axe Pack/standing run forward.fbx", "Mix_StandingRunForward", new Vector2(0f, 1f)),
            ("Pro Melee Axe Pack/standing run back.fbx", "Mix_StandingRunBack", new Vector2(0f, -1f)),
            ("Pro Sword and Shield Pack/run left.fbx", "Mix_SwordShieldRunLeft", new Vector2(-1f, 0f)),
            ("Pro Sword and Shield Pack/run right.fbx", "Mix_SwordShieldRunRight", new Vector2(1f, 0f)),
        };

        // Pro Rifle Pack (Mixamo full version of Basic Shooter): true 8-direction walk and run.
        // Paths are relative to AnimationsRoot because the aiming idle stays in Basic Shooter Pack.
        // Diagonals sit on the walk (0.3) and run (1.0) rings: 0.3 * cos 45° = 0.2121.
        private static readonly (string File, string ClipName, Vector2 Pos)[] RangedClips =
        {
            ("Basic Shooter Pack/rifle aiming idle.fbx", "Mix_AimingIdle", new Vector2(0f, 0f)),
            ("Pro Rifle Pack/walk forward.fbx", "Mix_ProRifleWalkForward", new Vector2(0f, 0.3f)),
            ("Pro Rifle Pack/walk forward right.fbx", "Mix_ProRifleWalkForwardRight", new Vector2(0.2121f, 0.2121f)),
            ("Pro Rifle Pack/walk right.fbx", "Mix_ProRifleWalkRight", new Vector2(0.3f, 0f)),
            ("Pro Rifle Pack/walk backward right.fbx", "Mix_ProRifleWalkBackwardRight", new Vector2(0.2121f, -0.2121f)),
            ("Pro Rifle Pack/walk backward.fbx", "Mix_ProRifleWalkBackward", new Vector2(0f, -0.3f)),
            ("Pro Rifle Pack/walk backward left.fbx", "Mix_ProRifleWalkBackwardLeft", new Vector2(-0.2121f, -0.2121f)),
            ("Pro Rifle Pack/walk left.fbx", "Mix_ProRifleWalkLeft", new Vector2(-0.3f, 0f)),
            ("Pro Rifle Pack/walk forward left.fbx", "Mix_ProRifleWalkForwardLeft", new Vector2(-0.2121f, 0.2121f)),
            ("Pro Rifle Pack/run forward.fbx", "Mix_ProRifleRunForward", new Vector2(0f, 1f)),
            ("Pro Rifle Pack/run forward right.fbx", "Mix_ProRifleRunForwardRight", new Vector2(0.7071f, 0.7071f)),
            ("Pro Rifle Pack/run right.fbx", "Mix_ProRifleRunRight", new Vector2(1f, 0f)),
            ("Pro Rifle Pack/run backward right.fbx", "Mix_ProRifleRunBackwardRight", new Vector2(0.7071f, -0.7071f)),
            ("Pro Rifle Pack/run backward.fbx", "Mix_ProRifleRunBackward", new Vector2(0f, -1f)),
            ("Pro Rifle Pack/run backward left.fbx", "Mix_ProRifleRunBackwardLeft", new Vector2(-0.7071f, -0.7071f)),
            ("Pro Rifle Pack/run left.fbx", "Mix_ProRifleRunLeft", new Vector2(-1f, 0f)),
            ("Pro Rifle Pack/run forward left.fbx", "Mix_ProRifleRunForwardLeft", new Vector2(-0.7071f, 0.7071f)),
        };

        private const string AnimationsRoot = "Assets/Art/Animations";

        private const string HumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";

        /// <summary>Base-layer locomotion playback multiplier (exhaustion), set by AnimationOrchestrator. Default 1.</summary>
        private const string LocoRateParameter = "LocoRate";

        /// <summary>Melee swing state speed multiplier (wind-up tempo), set by AnimationOrchestrator. Default 1.</summary>
        private const string AttackRateParameter = "AttackRate";

        /// <summary>Avatar every Mixamo clip copies (CopyFromOther) so humanoid retargeting matches the rig.</summary>
        private const string XBotAvatarPath = "Assets/Art/Animations/Locomotion Pack/X Bot.fbx";

        /// <summary>Soft blend from a hold pose into the swing wind-up; 0.05 s read as a snap.</summary>
        private const float SwingEntryDuration = 0.1f;
        private const float SwingExitTime = 0.9f;
        private const float SwingExitDuration = 0.15f;

        /// <summary>Calibrated child timeScale bounds — beyond this the clip reads as slow-motion or frantic.</summary>
        private const float MinCalibratedTimeScale = 0.5f;
        private const float MaxCalibratedTimeScale = 2f;

        /// <summary>
        /// Male Injured Pack has no dedicated strafes — sample walk/run forward/back and reuse
        /// walk at lateral positions so VelX still blends.
        /// </summary>
        private static readonly (string File, string ClipName, Vector2 Pos)[] InjuredClips =
        {
            ("injured idle.fbx", "Mix_InjuredIdle", new Vector2(0f, 0f)),
            ("injured walk.fbx", "Mix_InjuredWalk", new Vector2(0f, 0.3f)),
            ("injured walk backwards.fbx", "Mix_InjuredWalkBackwards", new Vector2(0f, -0.3f)),
            ("injured walk.fbx", "Mix_InjuredWalk", new Vector2(-0.3f, 0.3f)),
            ("injured walk.fbx", "Mix_InjuredWalk", new Vector2(0.3f, 0.3f)),
            ("injured walk.fbx", "Mix_InjuredWalk", new Vector2(-0.3f, 0f)),
            ("injured walk.fbx", "Mix_InjuredWalk", new Vector2(0.3f, 0f)),
            ("injured run.fbx", "Mix_InjuredRun", new Vector2(0f, 1f)),
            ("injured run backwards.fbx", "Mix_InjuredRunBackwards", new Vector2(0f, -1f)),
            ("injured walk.fbx", "Mix_InjuredWalk", new Vector2(-1f, 1f)),
            ("injured walk.fbx", "Mix_InjuredWalk", new Vector2(1f, 1f)),
            ("injured walk.fbx", "Mix_InjuredWalk", new Vector2(-1f, 0f)),
            ("injured walk.fbx", "Mix_InjuredWalk", new Vector2(1f, 0f)),
        };

        /// <summary>
        /// Parameters expected by <c>Animations.Humanoid</c> / AnimationOrchestrator.
        /// Order is preserved when force-rebinding so the Animator window stays readable.
        /// </summary>
        private static readonly (string Name, AnimatorControllerParameterType Type)[] RequiredHumanoidParameters =
        {
            ("Speed", AnimatorControllerParameterType.Float),
            ("Floating", AnimatorControllerParameterType.Bool),
            ("LimpSide", AnimatorControllerParameterType.Int),
            ("IsCrawling", AnimatorControllerParameterType.Bool),
            ("IsDragging", AnimatorControllerParameterType.Bool),
            ("ArmHold", AnimatorControllerParameterType.Int),
            ("InjuredArmLeft", AnimatorControllerParameterType.Float),
            ("InjuredArmRight", AnimatorControllerParameterType.Float),
            ("InjuredLeg", AnimatorControllerParameterType.Float),
            ("IsSeated", AnimatorControllerParameterType.Bool),
            ("CombatMode", AnimatorControllerParameterType.Bool),
            ("CombatStance", AnimatorControllerParameterType.Int),
            ("AimYaw", AnimatorControllerParameterType.Float),
            ("AimPitch", AnimatorControllerParameterType.Float),
            ("AttackSwing", AnimatorControllerParameterType.Trigger),
            ("AttackStab", AnimatorControllerParameterType.Trigger),
            ("AttackVariant", AnimatorControllerParameterType.Int),
            (AttackRateParameter, AnimatorControllerParameterType.Float),
            ("MirrorUpperBody", AnimatorControllerParameterType.Bool),
            (PistolStanceAnimation.HoldParameter, AnimatorControllerParameterType.Float),
            (PistolStanceAnimation.StrafeParameter, AnimatorControllerParameterType.Float),
            ("Throw", AnimatorControllerParameterType.Trigger),
            ("Emote", AnimatorControllerParameterType.Trigger),
            ("Flinch", AnimatorControllerParameterType.Trigger),
            ("FireRifle", AnimatorControllerParameterType.Trigger),
            ("Reload", AnimatorControllerParameterType.Trigger),
            ("VelX", AnimatorControllerParameterType.Float),
            ("VelZ", AnimatorControllerParameterType.Float),
            ("Turn", AnimatorControllerParameterType.Float),
            (LocoRateParameter, AnimatorControllerParameterType.Float),
            ("Jump", AnimatorControllerParameterType.Trigger),
            ("TurnLeft90", AnimatorControllerParameterType.Trigger),
            ("TurnRight90", AnimatorControllerParameterType.Trigger),
        };

        [MenuItem("SS3D/Animation/Rebuild Combat Stance Blend Trees")]
        public static void RebuildCombatStanceBlendTreesMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Combat Stance Blend Trees",
                    "Rebuild Base Layer Peaceful / Melee / Ranged / Injured FreeformCartesian2D locomotion " +
                    "switched by CombatStance + LimpSide. Restores AttackSwing trigger on Upper Body; " +
                    "wires stance-aware Flinch (GettingHit / gut / HitReaction); Ranged Upper Body Aim Idle + Fire/Reload; " +
                    "remaps injured-arm additive.\n\n" +
                    "Modifies HumanCharacterAnimator.controller.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            string result = RebuildCombatStanceBlendTrees();
            EditorUtility.DisplayDialog("Rebuild Combat Stance Blend Trees", result, "OK");
        }

        /// <summary>
        /// Drop <c>artifacts/force-rebuild-animator.flag</c> then wait for script reload —
        /// used when batchmode cannot open the project because the interactive Editor holds it.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RebuildFromFlagIfPresent()
        {
            const string flagPath = "artifacts/force-rebuild-animator.flag";
            if (!System.IO.File.Exists(flagPath))
            {
                return;
            }

            try
            {
                System.IO.File.Delete(flagPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HumanoidLocomotionBlendSetup] Could not delete rebuild flag: {ex.Message}");
                return;
            }

            string result = RebuildCombatStanceBlendTrees();
            Debug.Log($"[HumanoidLocomotionBlendSetup] Flag-triggered rebuild: {result}");
            System.IO.File.WriteAllText("artifacts/force-rebuild-animator.result", result);
        }

        /// <summary>Batchmode: -executeMethod SS3D.Editor.HumanoidLocomotionBlendSetup.RebuildCombatStanceBlendTreesBatch</summary>
        public static void RebuildCombatStanceBlendTreesBatch()
        {
            string result = RebuildCombatStanceBlendTrees();
            Debug.Log($"[HumanoidLocomotionBlendSetup] {result}");
            if (result.StartsWith("ERROR"))
            {
                EditorApplication.Exit(1);
            }
        }

        public static void RebuildLocomotionBlendTreeBatch() => RebuildCombatStanceBlendTreesBatch();

        [MenuItem("SS3D/Animation/Rebind Humanoid Animator Parameters")]
        public static void RebindHumanoidAnimatorParametersMenu()
        {
            string result = RebindHumanoidAnimatorParameters();
            EditorUtility.DisplayDialog("Rebind Humanoid Animator Parameters", result, "OK");
        }

        /// <summary>Batchmode: -executeMethod SS3D.Editor.HumanoidLocomotionBlendSetup.RebindHumanoidAnimatorParametersBatch</summary>
        public static void RebindHumanoidAnimatorParametersBatch()
        {
            string result = RebindHumanoidAnimatorParameters();
            Debug.Log($"[HumanoidLocomotionBlendSetup] {result}");
            if (result.StartsWith("ERROR"))
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Force-removes and re-adds every orchestrator parameter through the AnimatorController API.
        /// Hand-edited YAML parameter entries can appear in the asset yet fail Animator.Set* at runtime.
        /// </summary>
        public static string RebindHumanoidAnimatorParameters()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                return $"ERROR: Missing controller at {ControllerPath}";
            }

            var before = controller.parameters.Select(p => $"{p.name}:{p.type}:{p.nameHash}").ToArray();
            Debug.Log($"[HumanoidLocomotionBlendSetup] Parameters before rebind ({before.Length}):\n - "
                      + string.Join("\n - ", before));

            // Remove required params by name (keep any unexpected extras).
            for (int i = controller.parameters.Length - 1; i >= 0; i--)
            {
                string name = controller.parameters[i].name;
                if (RequiredHumanoidParameters.Any(p => p.Name == name))
                {
                    controller.RemoveParameter(i);
                }
            }

            foreach ((string Name, AnimatorControllerParameterType Type) required in RequiredHumanoidParameters)
            {
                controller.AddParameter(required.Name, required.Type);
            }

            // LocoRate multiplies locomotion state speed: a 0 default would freeze every animator
            // that never sets it (preview booth, fresh spawns before the first orchestrator tick).
            AnimatorControllerParameter[] withDefaults = controller.parameters;
            for (int i = 0; i < withDefaults.Length; i++)
            {
                if (withDefaults[i].name == LocoRateParameter || withDefaults[i].name == AttackRateParameter)
                {
                    withDefaults[i].defaultFloat = 1f;
                }
            }

            controller.parameters = withDefaults;

            var after = controller.parameters.Select(p => $"{p.name}:{p.type}:{p.nameHash}").ToArray();
            Debug.Log($"[HumanoidLocomotionBlendSetup] Parameters after rebind ({after.Length}):\n - "
                      + string.Join("\n - ", after));

            // Verify hashes match Animator.StringToHash (what AnimationOrchestrator uses).
            var mismatches = new List<string>();
            foreach ((string Name, AnimatorControllerParameterType Type) required in RequiredHumanoidParameters)
            {
                AnimatorControllerParameter param = controller.parameters.FirstOrDefault(p => p.name == required.Name);
                int expected = Animator.StringToHash(required.Name);
                if (param == null)
                {
                    mismatches.Add($"{required.Name}: missing after rebind");
                }
                else if (param.nameHash != expected)
                {
                    mismatches.Add($"{required.Name}: nameHash {param.nameHash} != StringToHash {expected}");
                }
                else if (param.type != required.Type)
                {
                    mismatches.Add($"{required.Name}: type {param.type} != {required.Type}");
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (mismatches.Count > 0)
            {
                return "ERROR: Rebind finished with mismatches:\n - " + string.Join("\n - ", mismatches);
            }

            return $"OK: Re-bound {RequiredHumanoidParameters.Length} humanoid animator parameters via AnimatorController API.";
        }

        public static string RebuildCombatStanceBlendTrees()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                return $"ERROR: Missing controller at {ControllerPath}";
            }

            // Prefer a full API rebind so hand-edited YAML stubs cannot leave SetBool/SetInteger broken.
            string rebind = RebindHumanoidAnimatorParameters();
            if (rebind.StartsWith("ERROR"))
            {
                return rebind;
            }

            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            RemoveGeneratedBlendTrees(controller);

            AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;

            if (!TryReadCombatGaitSpeeds(out float combatWalkSpeed, out float combatRunSpeed, out float humanScale, out string gaitError))
            {
                return gaitError;
            }

            float CombatTargetSpeed(Vector2 pos) => pos.magnitude <= 0.31f ? combatWalkSpeed : combatRunSpeed;
            Debug.Log($"[HumanoidLocomotionBlendSetup] Combat gait targets: walk {combatWalkSpeed:F2} m/s, run {combatRunSpeed:F2} m/s.");

            BlendTree peacefulTree = BuildBlendTree(
                controller, "Peaceful Locomotion 2D", LocomotionPack, PeacefulClips,
                BlendTreeType.FreeformCartesian2D, null, humanScale);
            BlendTree meleeTree = BuildBlendTree(
                controller, "Melee Locomotion 2D", AnimationsRoot, MeleeClips,
                BlendTreeType.FreeformDirectional2D, CombatTargetSpeed, humanScale);
            BlendTree rangedTree = BuildBlendTree(
                controller, "Ranged Locomotion 2D", AnimationsRoot, RangedClips,
                BlendTreeType.FreeformDirectional2D, CombatTargetSpeed, humanScale);
            BlendTree pistolTree = BuildPistolBlendTree(controller, CombatTargetSpeed, humanScale);
            BlendTree injuredTree = BuildInjuredBlendTree(controller);
            if (peacefulTree == null || meleeTree == null || rangedTree == null || pistolTree == null || injuredTree == null)
            {
                return "ERROR: Failed to build one or more stance blend trees (check Mix_* clip names after reimport).";
            }

            AnimatorState peaceful = FindOrCreateState(baseMachine, "Peaceful Locomotion", new Vector3(300, 0, 0));
            AnimatorState melee = FindOrCreateState(baseMachine, "Melee Locomotion", new Vector3(300, 120, 0));
            AnimatorState ranged = FindOrCreateState(baseMachine, "Ranged Locomotion", new Vector3(300, 240, 0));
            AnimatorState injured = FindOrCreateState(baseMachine, "Injured Locomotion", new Vector3(550, 120, 0));
            peaceful.motion = peacefulTree;
            melee.motion = meleeTree;
            rangedTree.blendParameter = PistolStanceAnimation.StrafeParameter;
            ranged.motion = BuildRangedHoldBlend(controller, rangedTree, pistolTree);
            // Only one-handed guns set MirrorUpperBody (two-handed rifles never mirror), so this mirrors
            // the legs for a left-hand pistol alone. See PistolStanceAnimation.StrafeParameter.
            EnableMirrorParameter(ranged, "MirrorUpperBody");
            injured.motion = injuredTree;
            foreach (AnimatorState locomotionState in new[] { peaceful, melee, ranged, injured })
            {
                locomotionState.speedParameter = LocoRateParameter;
                locomotionState.speedParameterActive = true;
            }
            baseMachine.defaultState = peaceful;

            // Retarget legacy Movement state if present.
            AnimatorState legacyMovement = FindState(baseMachine, "Movement");
            if (legacyMovement != null)
            {
                legacyMovement.motion = peacefulTree;
            }

            // Drop placeholder Limp Left/Right states — Injured Locomotion replaces them.
            RemoveStateIfPresent(baseMachine, "Limp Left");
            RemoveStateIfPresent(baseMachine, "Limp Right");

            ClearTransitions(peaceful);
            ClearTransitions(melee);
            ClearTransitions(ranged);
            ClearTransitions(injured);

            WireStanceTransitions(peaceful, melee, ranged);
            WireStanceTransitions(melee, peaceful, ranged);
            WireStanceTransitions(ranged, peaceful, melee);

            WireLimpEntry(peaceful, injured);
            WireLimpEntry(melee, injured);
            WireLimpEntry(ranged, injured);
            WireLimpExit(injured, peaceful, 0);
            WireLimpExit(injured, melee, 1);
            WireLimpExit(injured, ranged, 2);

            WireGetUpStates(baseMachine, peaceful, melee, ranged);
            WireJumpAndTurnOneshots(controller, baseMachine, peaceful, melee, ranged, injured);
            WireInjuredWaveEmote(baseMachine, injured);

            WireStanceAwareFlinch(baseMachine, peaceful, melee, ranged, injured);
            // Base layer must not consume AttackSwing / ranged oneshots — Upper Body owns those.
            // Fire on Base resets the Ranged FreeformCartesian (feet pop back to idle every shot).
            MuteAnyStateTrigger(baseMachine, "AttackSwing");
            MuteAnyStateTrigger(baseMachine, "FireRifle");
            MuteAnyStateTrigger(baseMachine, "Reload");

            // Upper-body Attack Swing variants (cycle via AttackVariant 0/1/2); exit-time → Hold Default.
            if (controller.layers.Length > 1)
            {
                AnimatorStateMachine upper = controller.layers[1].stateMachine;
                MuteAnyStateTrigger(upper, "AttackSwing");

                ConfigureMeleeSwingImports();
                AnimatorState holdDefault = FindState(upper, "Hold Default");
                AnimatorState holdItemPose = FindState(upper, "Hold Item");
                AnimatorState holdWeaponPose = FindState(upper, "Hold Weapon");
                for (int variant = 0; variant < MeleeSwingAnimation.Variants.Length; variant++)
                {
                    MeleeSwingAnimation.Swing swing = MeleeSwingAnimation.Variants[variant];
                    AnimationClip attackClip = LoadPackClip($"{AnimationsRoot}/{swing.File}", swing.ClipName);
                    AnimatorState upperAttack = FindOrCreateState(upper, swing.StateName, new Vector3(600, 40 + 60 * variant, 0));
                    if (attackClip != null)
                    {
                        upperAttack.motion = attackClip;
                        upperAttack.writeDefaultValues = true;
                    }
                    else
                    {
                        Debug.LogError($"[HumanoidLocomotionBlendSetup] Missing swing clip '{swing.ClipName}' in {swing.File}");
                    }

                    // Wind-up tempo: AnimationOrchestrator sets AttackRate so the strike frame lands at connect.
                    upperAttack.speed = 1f;
                    upperAttack.speedParameterActive = true;
                    upperAttack.speedParameter = AttackRateParameter;

                    EnableMirrorParameter(upperAttack, "MirrorUpperBody");
                    EnsureAnyStateTriggerWithInt(
                        upper, upperAttack, "AttackSwing", "AttackVariant", variant, canTransitionToSelf: true);
                    foreach (AnimatorStateTransition entry in upper.anyStateTransitions)
                    {
                        if (entry.destinationState == upperAttack)
                        {
                            entry.hasFixedDuration = true;
                            entry.duration = SwingEntryDuration;
                        }
                    }

                    // Return straight to the pose for what the hand holds, not via Hold Default (two blends).
                    ClearTransitions(upperAttack);
                    AddSwingExit(upperAttack, holdItemPose, armHold: 1);
                    AddSwingExit(upperAttack, holdWeaponPose, armHold: 2);
                    AddSwingExit(upperAttack, holdDefault, armHold: 0);
                }

                // Mixamo holds are authored for the right hand — mirror when active hand is left.
                foreach (string holdName in new[] { "Hold Default", "Hold Item", "Hold Weapon" })
                {
                    AnimatorState hold = FindState(upper, holdName);
                    if (hold != null)
                    {
                        EnableMirrorParameter(hold, "MirrorUpperBody");
                    }
                }

                // Fire + Reload on Upper Body over Rifle Aim Idle. Upper Body stays weighted for the
                // whole Ranged stance so shots don't pulse layer weight (that snap was the shot twitch).
                AnimatorState holdItem = FindState(upper, "Hold Item");
                AnimatorState holdWeapon = FindState(upper, "Hold Weapon");
                AnimatorState aimIdle = WireRangedAimIdle(upper, holdDefault, holdItem, holdWeapon);
                WireRangedFireOneshot(upper, aimIdle);
                WireRangedReloadOneshot(upper, aimIdle);
                WirePistolAimIdle(upper, aimIdle, holdDefault, holdItem, holdWeapon);
                WireOffHandLayers(controller);
            }

            RemapStateMotion(
                baseMachine,
                "Floating",
                "Assets/Art/Animations/Misc/X Bot@Floating.fbx",
                "Mix_Floating");

            if (controller.layers.Length > 2)
            {
                AnimatorStateMachine additive = controller.layers[2].stateMachine;
                RemapStateMotion(additive, "Flinch",
                    $"{MeleePack}/standing react large gut.fbx", "Mix_StandingReactLargeGut");
                RemapStateMotion(additive, "Empty Additive",
                    $"{InjuredPack}/injured hurting idle.fbx", "Mix_InjuredHurtingIdle");
                // Additive Flinch is the soft upper-body hit overlay (orchestrator lerps layer weight).
                // Keep unmuted — muted Additive + stagger weight only showed Empty Additive hurting idle.
                WireAdditiveFlinch(additive);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return "OK: Rebuilt stance blends; get-up states; injured idle severity; limp Jump/Turn90; Injured Wave Emote; "
                   + "AttackSwing variants + MirrorUpperBody; stance-aware Flinch (GettingHit/gut/HitReaction); "
                   + "Ranged Upper Body Aim Idle + FireRifle/Reload; pistol hold + Off Hand layers; Floating → Mix_Floating.";
        }

        /// <summary>
        /// Ranged rest pose on Upper Body (Mix_AimingIdle). Orchestrator keeps Upper Body
        /// weight at 1 for the whole Ranged stance so Fire/Reload can oneshot without pulsing layer weight.
        /// All Hold* states must reach Aim Idle when CombatStance becomes Ranged — otherwise a held
        /// weapon leaves Upper Body on Hold Weapon and shows a melee pose until the first shot.
        /// </summary>
        private static AnimatorState WireRangedAimIdle(
            AnimatorStateMachine upper,
            AnimatorState holdDefault,
            AnimatorState holdItem,
            AnimatorState holdWeapon)
        {
            AnimationClip aimClip = LoadPackClip($"{ShooterPack}/rifle aiming idle.fbx", "Mix_AimingIdle");
            AnimatorState aimIdle = FindOrCreateState(upper, "Rifle Aim Idle", new Vector3(300, 400, 0));
            if (aimClip != null)
            {
                aimIdle.motion = aimClip;
                aimIdle.writeDefaultValues = true;
            }

            foreach (AnimatorState hold in new[] { holdDefault, holdItem, holdWeapon })
            {
                if (hold != null)
                {
                    EnsureCombatStanceTransition(hold, aimIdle, combatStance: 2, duration: 0.1f);
                }
            }

            if (holdDefault != null)
            {
                EnsureCombatStanceTransition(aimIdle, holdDefault, combatStance: 0, duration: 0.1f);
                EnsureCombatStanceTransition(aimIdle, holdDefault, combatStance: 1, duration: 0.1f);
            }

            return aimIdle;
        }

        /// <summary>
        /// Short M4 fire oneshot on Upper Body. Exits to Rifle Aim Idle — Base Ranged FreeformCartesian
        /// keeps foot phase; Upper Body weight stays up for the stance so there is no per-shot snap.
        /// </summary>
        private static void WireRangedFireOneshot(AnimatorStateMachine upper, AnimatorState aimIdle)
        {
            AnimationClip fireClip = LoadPackClip($"{ShooterPack}/firing rifle.fbx", "Mix_FiringRifle");
            AnimatorState fireState = FindOrCreateState(upper, "Fire Rifle", new Vector3(600, 220, 0));
            if (fireClip != null)
            {
                fireState.motion = fireClip;
                fireState.writeDefaultValues = true;
            }

            UnmuteAnyStateTrigger(upper, "FireRifle");
            EnsureAnyStateTriggerWithCombatStance(
                upper, fireState, "FireRifle", combatStance: 2, canTransitionToSelf: true, entryDuration: 0.05f);

            ClearTransitions(fireState);
            if (aimIdle != null)
            {
                EnsureExitToState(fireState, aimIdle, hasExitTime: true, exitTime: 0.85f, duration: 0.1f);
            }
        }

        /// <summary>
        /// Long M4 reload oneshot on Upper Body; exits to Rifle Aim Idle.
        /// </summary>
        private static void WireRangedReloadOneshot(AnimatorStateMachine upper, AnimatorState aimIdle)
        {
            AnimationClip reloadClip = LoadPackClip($"{ShooterPack}/reloading.fbx", "Mix_Reloading");
            AnimatorState reloadState = FindOrCreateState(upper, "Reload Rifle", new Vector3(600, 280, 0));
            if (reloadClip != null)
            {
                reloadState.motion = reloadClip;
                reloadState.writeDefaultValues = true;
            }

            UnmuteAnyStateTrigger(upper, "Reload");
            EnsureAnyStateTriggerWithCombatStance(
                upper, reloadState, "Reload", combatStance: 2, canTransitionToSelf: false, entryDuration: 0.1f);

            ClearTransitions(reloadState);
            if (aimIdle != null)
            {
                // Exit when the hands are back in the aim pose: the same moment the reload completes.
                EnsureExitToState(
                    reloadState,
                    aimIdle,
                    hasExitTime: true,
                    exitTime: RifleReloadClipTiming.Normalized(RifleReloadClipTiming.ReadySeconds),
                    duration: 0.15f);
            }
        }

        private static void EnsureCombatStanceTransition(
            AnimatorState from,
            AnimatorState to,
            int combatStance,
            float duration)
        {
            foreach (AnimatorStateTransition transition in from.transitions)
            {
                if (transition.destinationState != to)
                {
                    continue;
                }

                bool stanceOk = transition.conditions.Any(
                    c => c.parameter == "CombatStance"
                         && c.mode == AnimatorConditionMode.Equals
                         && (int)c.threshold == combatStance);
                if (stanceOk)
                {
                    transition.hasExitTime = false;
                    transition.hasFixedDuration = true;
                    transition.duration = duration;
                    return;
                }
            }

            AnimatorStateTransition created = from.AddTransition(to);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = duration;
            created.AddCondition(AnimatorConditionMode.Equals, combatStance, "CombatStance");
        }

        private static void EnsureAnyStateTriggerWithCombatStance(
            AnimatorStateMachine machine,
            AnimatorState destination,
            string triggerName,
            int combatStance,
            bool canTransitionToSelf,
            float entryDuration)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.destinationState != destination)
                {
                    continue;
                }

                if (!transition.conditions.Any(c => c.parameter == triggerName))
                {
                    continue;
                }

                bool stanceOk = transition.conditions.Any(
                    c => c.parameter == "CombatStance" && (int)c.threshold == combatStance);
                if (!stanceOk)
                {
                    continue;
                }

                transition.mute = false;
                transition.canTransitionToSelf = canTransitionToSelf;
                transition.hasExitTime = false;
                transition.hasFixedDuration = true;
                transition.duration = entryDuration;
                MuteDuplicateAnyStateTriggers(machine, destination, triggerName, transition);
                return;
            }

            AnimatorStateTransition created = machine.AddAnyStateTransition(destination);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = entryDuration;
            created.canTransitionToSelf = canTransitionToSelf;
            created.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
            created.AddCondition(AnimatorConditionMode.Equals, combatStance, "CombatStance");
            MuteDuplicateAnyStateTriggers(machine, destination, triggerName, created);
        }

        /// <summary>
        /// Base Flinch oneshots: GettingHit (Peaceful or limp), StandingReactLargeGut (Melee),
        /// HitReaction (Ranged). Legacy single Flinch state is removed.
        /// </summary>
        private static void WireStanceAwareFlinch(
            AnimatorStateMachine baseMachine,
            AnimatorState peaceful,
            AnimatorState melee,
            AnimatorState ranged,
            AnimatorState injured)
        {
            MuteAnyStateTrigger(baseMachine, "Flinch");
            RemoveStateIfPresent(baseMachine, "Flinch");

            AnimationClip gettingHit = LoadPackClip(
                "Assets/Art/Animations/Misc/X Bot@Getting Hit.fbx", "Mix_GettingHit");
            AnimationClip gut = LoadPackClip(
                $"{MeleePack}/standing react large gut.fbx", "Mix_StandingReactLargeGut");
            AnimationClip hitReaction = LoadPackClip(
                $"{ShooterPack}/hit reaction.fbx", "Mix_HitReaction");

            AnimatorState flinchGettingHit = FindOrCreateState(
                baseMachine, "Flinch GettingHit", new Vector3(50, 280, 0));
            if (gettingHit != null)
            {
                flinchGettingHit.motion = gettingHit;
                flinchGettingHit.writeDefaultValues = true;
            }

            AnimatorState flinchMelee = FindOrCreateState(
                baseMachine, "Flinch Melee", new Vector3(50, 340, 0));
            if (gut != null)
            {
                flinchMelee.motion = gut;
                flinchMelee.writeDefaultValues = true;
            }

            AnimatorState flinchRanged = FindOrCreateState(
                baseMachine, "Flinch Ranged", new Vector3(50, 400, 0));
            if (hitReaction != null)
            {
                flinchRanged.motion = hitReaction;
                flinchRanged.writeDefaultValues = true;
            }

            // Limp → GettingHit; Peaceful (stance 0, not limping) → GettingHit.
            EnsureAnyStateFlinch(baseMachine, flinchGettingHit, limpGreaterThanZero: true, combatStance: null);
            EnsureAnyStateFlinch(baseMachine, flinchGettingHit, limpGreaterThanZero: false, combatStance: 0);
            EnsureAnyStateFlinch(baseMachine, flinchMelee, limpGreaterThanZero: false, combatStance: 1);
            EnsureAnyStateFlinch(baseMachine, flinchRanged, limpGreaterThanZero: false, combatStance: 2);

            ClearTransitions(flinchGettingHit);
            EnsureExitToStateWithLimpGate(flinchGettingHit, injured, requireLimping: true, combatStance: null);
            EnsureExitToStateWithLimpGate(flinchGettingHit, peaceful, requireLimping: false, combatStance: 0);
            EnsureExitToStateWithLimpGate(flinchGettingHit, melee, requireLimping: false, combatStance: 1);
            EnsureExitToStateWithLimpGate(flinchGettingHit, ranged, requireLimping: false, combatStance: 2);
            SoftenFlinchStateExits(flinchGettingHit);

            ClearTransitions(flinchMelee);
            EnsureExitToStateWithLimpGate(flinchMelee, peaceful, requireLimping: false, combatStance: 0);
            EnsureExitToStateWithLimpGate(flinchMelee, melee, requireLimping: false, combatStance: 1);
            EnsureExitToStateWithLimpGate(flinchMelee, ranged, requireLimping: false, combatStance: 2);
            SoftenFlinchStateExits(flinchMelee);

            ClearTransitions(flinchRanged);
            EnsureExitToStateWithLimpGate(flinchRanged, peaceful, requireLimping: false, combatStance: 0);
            EnsureExitToStateWithLimpGate(flinchRanged, melee, requireLimping: false, combatStance: 1);
            EnsureExitToStateWithLimpGate(flinchRanged, ranged, requireLimping: false, combatStance: 2);
            SoftenFlinchStateExits(flinchRanged);
        }

        private static void SoftenFlinchStateExits(AnimatorState flinchState)
        {
            foreach (AnimatorStateTransition transition in flinchState.transitions)
            {
                transition.hasExitTime = true;
                transition.exitTime = FlinchExitTime;
                transition.hasFixedDuration = true;
                transition.duration = FlinchExitDuration;
            }
        }

        /// <summary>
        /// Unmute Additive Any→Flinch and soften entry/exit so gut overlay matches Base flinch blends.
        /// </summary>
        private static void WireAdditiveFlinch(AnimatorStateMachine additive)
        {
            AnimatorState flinch = FindState(additive, "Flinch");
            if (flinch == null)
            {
                return;
            }

            foreach (AnimatorStateTransition transition in additive.anyStateTransitions)
            {
                if (transition.destinationState != flinch
                    || !transition.conditions.Any(c => c.parameter == "Flinch"))
                {
                    continue;
                }

                transition.mute = false;
                transition.canTransitionToSelf = false;
                transition.hasExitTime = false;
                transition.hasFixedDuration = true;
                transition.duration = FlinchEntryDuration;
            }

            SoftenFlinchStateExits(flinch);
        }

        private static void EnsureAnyStateFlinch(
            AnimatorStateMachine machine,
            AnimatorState destination,
            bool limpGreaterThanZero,
            int? combatStance)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.destinationState != destination)
                {
                    continue;
                }

                if (!transition.conditions.Any(c => c.parameter == "Flinch"))
                {
                    continue;
                }

                bool limpOk = limpGreaterThanZero
                    ? transition.conditions.Any(c => c.parameter == "LimpSide" && c.mode == AnimatorConditionMode.Greater)
                    : transition.conditions.Any(c => c.parameter == "LimpSide" && c.mode == AnimatorConditionMode.Equals);
                bool stanceOk = combatStance == null
                    ? !transition.conditions.Any(c => c.parameter == "CombatStance")
                    : transition.conditions.Any(
                        c => c.parameter == "CombatStance" && (int)c.threshold == combatStance.Value);

                if (!limpOk || !stanceOk)
                {
                    continue;
                }

                transition.mute = false;
                transition.canTransitionToSelf = false;
                transition.hasExitTime = false;
                transition.hasFixedDuration = true;
                transition.duration = FlinchEntryDuration;
                return;
            }

            AnimatorStateTransition created = machine.AddAnyStateTransition(destination);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = FlinchEntryDuration;
            created.canTransitionToSelf = false;
            created.AddCondition(AnimatorConditionMode.If, 0f, "Flinch");
            if (limpGreaterThanZero)
            {
                created.AddCondition(AnimatorConditionMode.Greater, 0f, "LimpSide");
            }
            else
            {
                created.AddCondition(AnimatorConditionMode.Equals, 0f, "LimpSide");
            }

            if (combatStance.HasValue)
            {
                created.AddCondition(AnimatorConditionMode.Equals, combatStance.Value, "CombatStance");
            }
        }

        private static BlendTree BuildInjuredBlendTree(AnimatorController controller)
        {
            AnimationClip idle = LoadPackClip($"{InjuredPack}/injured idle.fbx", "Mix_InjuredIdle");
            AnimationClip stumble = LoadPackClip($"{InjuredPack}/injured stumble idle.fbx", "Mix_InjuredStumbleIdle");
            if (idle == null || stumble == null)
            {
                Debug.LogError("[HumanoidLocomotionBlendSetup] Missing injured idle/stumble clips.");
                return null;
            }

            BlendTree idleSeverity = new BlendTree
            {
                name = "Injured Idle Severity",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "InjuredLeg",
                useAutomaticThresholds = false,
            };
            // Moderate limp keeps injured idle; severe leg damage blends to stumble.
            idleSeverity.AddChild(idle, 0.3f);
            idleSeverity.AddChild(idle, 0.55f);
            idleSeverity.AddChild(stumble, 0.7f);
            AssetDatabase.AddObjectToAsset(idleSeverity, controller);

            BlendTree tree = new BlendTree
            {
                name = "Injured Locomotion 2D",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "VelX",
                blendParameterY = "VelZ",
                useAutomaticThresholds = false,
            };

            tree.AddChild(idleSeverity, new Vector2(0f, 0f));
            foreach ((string File, string ClipName, Vector2 Pos) entry in InjuredClips)
            {
                if (entry.Pos == Vector2.zero)
                {
                    continue; // idle replaced by severity blend above
                }

                AnimationClip clip = LoadPackClip($"{InjuredPack}/{entry.File}", entry.ClipName);
                if (clip == null)
                {
                    Debug.LogError($"[HumanoidLocomotionBlendSetup] Missing clip '{entry.ClipName}' in {InjuredPack}/{entry.File}");
                    return null;
                }

                tree.AddChild(clip, entry.Pos);
            }

            AssetDatabase.AddObjectToAsset(tree, controller);
            return tree;
        }

        private static void WireJumpAndTurnOneshots(
            AnimatorController controller,
            AnimatorStateMachine baseMachine,
            AnimatorState peaceful,
            AnimatorState melee,
            AnimatorState ranged,
            AnimatorState injured)
        {
            AnimationClip jumpClip = LoadPackClip($"{LocomotionPack}/jump.fbx", "Mix_Jump");
            if (jumpClip != null)
            {
                AnimatorState jumpState = FindOrCreateState(baseMachine, "Jump", new Vector3(750, 120, 0));
                jumpState.motion = jumpClip;
                EnsureAnyStateTriggerWithLimpGate(baseMachine, jumpState, "Jump", requireLimping: false, canTransitionToSelf: false);
                ClearTransitions(jumpState);
                EnsureExitToStateWithLimpGate(jumpState, peaceful, requireLimping: false, combatStance: 0);
                EnsureExitToStateWithLimpGate(jumpState, melee, requireLimping: false, combatStance: 1);
                EnsureExitToStateWithLimpGate(jumpState, ranged, requireLimping: false, combatStance: 2);
            }

            AnimationClip injuredStandJump = LoadPackClip(
                $"{InjuredPack}/injured standing jump.fbx", "Mix_InjuredStandingJump");
            AnimationClip injuredRunJump = LoadPackClip(
                $"{InjuredPack}/injured run jump.fbx", "Mix_InjuredRunJump");
            if (injuredStandJump != null)
            {
                BlendTree injuredJumpBlend = new BlendTree
                {
                    name = "Injured Jump Blend",
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = "Speed",
                    useAutomaticThresholds = false,
                };
                injuredJumpBlend.AddChild(injuredStandJump, 0f);
                injuredJumpBlend.AddChild(injuredStandJump, 0.4f);
                if (injuredRunJump != null)
                {
                    injuredJumpBlend.AddChild(injuredRunJump, 0.85f);
                }

                AssetDatabase.AddObjectToAsset(injuredJumpBlend, controller);

                AnimatorState injuredJump = FindOrCreateState(baseMachine, "Injured Jump", new Vector3(750, 220, 0));
                injuredJump.motion = injuredJumpBlend;
                EnsureAnyStateTriggerWithLimpGate(baseMachine, injuredJump, "Jump", requireLimping: true, canTransitionToSelf: false);
                ClearTransitions(injuredJump);
                EnsureExitToStateWithLimpGate(injuredJump, injured, requireLimping: true, combatStance: null);
            }

            WireTurn90(
                baseMachine,
                "Turn Left 90",
                "Injured Turn Left 90",
                "TurnLeft90",
                $"{LocomotionPack}/left turn 90.fbx",
                "Mix_LeftTurn90",
                $"{InjuredPack}/injured turn left.fbx",
                "Mix_InjuredTurnLeft",
                new Vector3(750, 0, 0),
                new Vector3(900, 0, 0),
                peaceful,
                melee,
                ranged,
                injured);

            WireTurn90(
                baseMachine,
                "Turn Right 90",
                "Injured Turn Right 90",
                "TurnRight90",
                $"{LocomotionPack}/right turn 90.fbx",
                "Mix_RightTurn90",
                $"{InjuredPack}/injured turn right.fbx",
                "Mix_InjuredTurnRight",
                new Vector3(750, 40, 0),
                new Vector3(900, 40, 0),
                peaceful,
                melee,
                ranged,
                injured);
        }

        private static void WireTurn90(
            AnimatorStateMachine baseMachine,
            string healthyName,
            string injuredName,
            string triggerName,
            string healthyPath,
            string healthyClip,
            string injuredPath,
            string injuredClip,
            Vector3 healthyPos,
            Vector3 injuredPos,
            AnimatorState peaceful,
            AnimatorState melee,
            AnimatorState ranged,
            AnimatorState injured)
        {
            AnimationClip healthyMotion = LoadPackClip(healthyPath, healthyClip);
            if (healthyMotion != null)
            {
                AnimatorState turn = FindOrCreateState(baseMachine, healthyName, healthyPos);
                turn.motion = healthyMotion;
                EnsureAnyStateTriggerWithLimpGate(baseMachine, turn, triggerName, requireLimping: false, canTransitionToSelf: false);
                ClearTransitions(turn);
                EnsureExitToStateWithLimpGate(turn, peaceful, requireLimping: false, combatStance: 0);
                EnsureExitToStateWithLimpGate(turn, melee, requireLimping: false, combatStance: 1);
                EnsureExitToStateWithLimpGate(turn, ranged, requireLimping: false, combatStance: 2);
            }

            AnimationClip injuredMotion = LoadPackClip(injuredPath, injuredClip);
            if (injuredMotion != null)
            {
                AnimatorState turnInjured = FindOrCreateState(baseMachine, injuredName, injuredPos);
                turnInjured.motion = injuredMotion;
                EnsureAnyStateTriggerWithLimpGate(
                    baseMachine, turnInjured, triggerName, requireLimping: true, canTransitionToSelf: false);
                ClearTransitions(turnInjured);
                EnsureExitToStateWithLimpGate(turnInjured, injured, requireLimping: true, combatStance: null);
            }
        }

        private static void WireInjuredWaveEmote(AnimatorStateMachine baseMachine, AnimatorState injured)
        {
            AnimationClip wave = LoadPackClip($"{InjuredPack}/injured wave idle.fbx", "Mix_InjuredWaveIdle");
            if (wave == null)
            {
                return;
            }

            // Base layer — Full Body Override weight is 0 during locomotion, so wave must live here.
            AnimatorState injuredWave = FindOrCreateState(baseMachine, "Injured Wave", new Vector3(900, 120, 0));
            injuredWave.motion = wave;
            injuredWave.writeDefaultValues = true;
            EnsureAnyStateTriggerWithLimpGate(
                baseMachine, injuredWave, "Emote", requireLimping: true, canTransitionToSelf: false);
            ClearTransitions(injuredWave);
            EnsureExitToStateWithLimpGate(injuredWave, injured, requireLimping: true, combatStance: null);
        }

        /// <summary>
        /// World walk/run speeds in combat from the Human prefab's tuning, so calibration follows
        /// <c>_movementSpeed</c> / <c>_combatWalkSpeedFactor</c> / <c>_combatRunSpeedFactor</c>.
        /// Rebuild after changing those. humanScale converts humanoid clip speed to metres.
        /// </summary>
        private static bool TryReadCombatGaitSpeeds(out float walkSpeed, out float runSpeed, out float humanScale, out string error)
        {
            walkSpeed = runSpeed = humanScale = 0f;
            error = null;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
            if (prefab == null)
            {
                error = $"ERROR: Missing {HumanPrefabPath} (needed for gait calibration).";
                return false;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var controllerComponent = instance.GetComponent<SS3D.Systems.Entities.Humanoid.HumanoidLivingController>();
                Animator animator = instance.GetComponent<Animator>();
                if (controllerComponent == null || animator == null)
                {
                    error = "ERROR: Human prefab lacks HumanoidLivingController or Animator.";
                    return false;
                }

                SerializedObject so = new SerializedObject(controllerComponent);
                float speed = so.FindProperty("_movementSpeed").floatValue;
                walkSpeed = speed * controllerComponent.WalkAnimatorValue * so.FindProperty("_combatWalkSpeedFactor").floatValue;
                runSpeed = speed * controllerComponent.RunAnimatorValue * so.FindProperty("_combatRunSpeedFactor").floatValue;

                animator.Rebind();
                animator.Update(0f);
                humanScale = animator.isHuman ? animator.humanScale : 1f;
                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>Blend trees this rebuild recreates. Each rebuild adds fresh sub-assets, so the
        /// previous ones are deleted first or they pile up in the controller as orphans.</summary>
        private static readonly string[] GeneratedBlendTreeNames =
        {
            "Peaceful Locomotion 2D", "Melee Locomotion 2D", "Ranged Locomotion 2D",
            "Injured Locomotion 2D", "Injured Idle Severity", "Injured Jump Blend",
            PistolTreeName, RangedHoldTreeName,
        };

        private static void AddSwingExit(AnimatorState swing, AnimatorState destination, int armHold)
        {
            if (destination == null)
            {
                return;
            }

            AnimatorStateTransition exit = swing.AddTransition(destination);
            exit.hasExitTime = true;
            exit.exitTime = SwingExitTime;
            exit.hasFixedDuration = true;
            exit.duration = SwingExitDuration;
            exit.AddCondition(AnimatorConditionMode.Equals, armHold, "ArmHold");
        }

        /// <summary>
        /// Humanoid import for the melee swing clips from <see cref="MeleeSwingAnimation.Variants"/>: X Bot
        /// avatar, trimmed to the table's frame range, no loop, root baked into the pose. Reimports only on change.
        /// </summary>
        private static void ConfigureMeleeSwingImports()
        {
            ConfigureOneshotImports(
                MeleeSwingAnimation.Variants.Select(s => (s.File, s.ClipName, s.FirstFrame, s.LastFrame)));
        }

        /// <summary>
        /// Get-up states from <see cref="GetUpAnimation"/>: trimmed Mixamo clips at the table's playback rate,
        /// leaving at <see cref="GetUpAnimation.BlendOutStart"/> into the locomotion state for the current
        /// stance and blending over the rest of the clip. <c>Ragdoll</c> plays them by state name.
        /// </summary>
        private static void WireGetUpStates(
            AnimatorStateMachine baseMachine, AnimatorState peaceful, AnimatorState melee, AnimatorState ranged)
        {
            ConfigureOneshotImports(GetUpAnimation.All.Select(c => (c.File, c.ClipName, c.FirstFrame, c.LastFrame)));
            for (int index = 0; index < GetUpAnimation.All.Length; index++)
            {
                GetUpAnimation.Clip getUp = GetUpAnimation.All[index];
                AnimationClip clip = LoadPackClip($"{AnimationsRoot}/{getUp.File}", getUp.ClipName);
                AnimatorState state = FindOrCreateState(baseMachine, getUp.StateName, new Vector3(50, 50 + 60 * index, 0));
                if (clip == null)
                {
                    Debug.LogError($"[HumanoidLocomotionBlendSetup] Missing get-up clip '{getUp.ClipName}' in {getUp.File}");
                    continue;
                }

                state.motion = clip;
                state.speed = getUp.PlaybackRate;
                state.speedParameterActive = false;
                state.writeDefaultValues = true;
                state.iKOnFeet = false;

                ClearTransitions(state);
                AnimatorState[] stanceStates = { peaceful, melee, ranged };
                for (int stance = 0; stance < stanceStates.Length; stance++)
                {
                    AnimatorStateTransition exit = state.AddTransition(stanceStates[stance]);
                    exit.hasExitTime = true;
                    exit.exitTime = GetUpAnimation.BlendOutStart;
                    exit.hasFixedDuration = true;
                    exit.duration = getUp.BlendOutSeconds;
                    exit.AddCondition(AnimatorConditionMode.Equals, stance, "CombatStance");
                }
            }
        }

        /// <summary>
        /// Humanoid import for non-looping Mixamo oneshots: X Bot avatar, trimmed to the given source frames,
        /// root baked into the pose. Reimports only on change.
        /// </summary>
        private static void ConfigureOneshotImports(
            IEnumerable<(string File, string ClipName, int FirstFrame, int LastFrame)> entries)
        {
            Avatar xBot = AssetDatabase.LoadAllAssetsAtPath(XBotAvatarPath).OfType<Avatar>().FirstOrDefault();
            if (xBot == null)
            {
                Debug.LogError($"[HumanoidLocomotionBlendSetup] Missing avatar in {XBotAvatarPath}");
                return;
            }

            foreach ((string file, string clipName, int firstFrame, int lastFrame) in entries)
            {
                string path = $"{AnimationsRoot}/{file}";
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                {
                    Debug.LogError($"[HumanoidLocomotionBlendSetup] Missing FBX {path}");
                    continue;
                }

                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                ModelImporterClipAnimation clip = clips.Length > 0 ? clips[0] : importer.defaultClipAnimations[0];
                bool unchanged = importer.animationType == ModelImporterAnimationType.Human
                    && importer.avatarSetup == ModelImporterAvatarSetup.CopyFromOther
                    && importer.sourceAvatar == xBot
                    && clips.Length == 1
                    && clip.name == clipName
                    && Mathf.Approximately(clip.firstFrame, firstFrame)
                    && Mathf.Approximately(clip.lastFrame, lastFrame)
                    && !clip.loopTime
                    && clip.lockRootRotation && clip.lockRootHeightY && clip.lockRootPositionXZ;
                if (unchanged)
                {
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = xBot;
                importer.importAnimation = true;
                clip.name = clipName;
                clip.takeName = importer.defaultClipAnimations[0].takeName;
                clip.firstFrame = firstFrame;
                clip.lastFrame = lastFrame;
                clip.loopTime = false;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                clip.mirror = false;
                importer.clipAnimations = new[] { clip };
                importer.SaveAndReimport();
                Debug.Log($"[HumanoidLocomotionBlendSetup] Imported {clipName} frames {firstFrame}-{lastFrame}");
            }
        }

        private static void RemoveGeneratedBlendTrees(AnimatorController controller)
        {
            foreach (Object subAsset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
            {
                if (subAsset is BlendTree tree && System.Array.IndexOf(GeneratedBlendTreeNames, tree.name) >= 0)
                {
                    AssetDatabase.RemoveObjectFromAsset(tree);
                    Object.DestroyImmediate(tree, true);
                }
            }
        }

        private static BlendTree BuildBlendTree(
            AnimatorController controller,
            string treeName,
            string packFolder,
            (string File, string ClipName, Vector2 Pos)[] entries,
            BlendTreeType blendType,
            System.Func<Vector2, float> targetWorldSpeed,
            float humanScale)
        {
            BlendTree tree = new BlendTree
            {
                name = treeName,
                blendType = blendType,
                blendParameter = "VelX",
                blendParameterY = "VelZ",
                useAutomaticThresholds = false,
            };

            foreach ((string File, string ClipName, Vector2 Pos) entry in entries)
            {
                AnimationClip clip = LoadPackClip($"{packFolder}/{entry.File}", entry.ClipName);
                if (clip == null)
                {
                    Debug.LogError($"[HumanoidLocomotionBlendSetup] Missing clip '{entry.ClipName}' in {packFolder}/{entry.File}");
                    return null;
                }

                tree.AddChild(clip, entry.Pos);
            }

            if (targetWorldSpeed != null)
            {
                CalibrateChildTimeScales(tree, targetWorldSpeed, humanScale);
            }

            AssetDatabase.AddObjectToAsset(tree, controller);
            return tree;
        }

        /// <summary>
        /// Per-child timeScale so each moving clip's root speed equals the world speed of its gait —
        /// feet neither slide nor moonwalk. Mixamo strafes differ a lot (rifle strafe right covers
        /// half the ground of strafe left per second), which is why this is measured, not hand-set.
        /// </summary>
        private static void CalibrateChildTimeScales(BlendTree tree, System.Func<Vector2, float> targetWorldSpeed, float humanScale)
        {
            ChildMotion[] children = tree.children;
            var log = new System.Text.StringBuilder($"[HumanoidLocomotionBlendSetup] {tree.name} calibration (humanScale {humanScale:F3}):");
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].position.sqrMagnitude < 0.0001f || !(children[i].motion is AnimationClip clip))
                {
                    continue;
                }

                Vector3 average = clip.averageSpeed;
                float clipSpeed = new Vector2(average.x, average.z).magnitude * humanScale;
                if (clipSpeed < 0.05f)
                {
                    continue;
                }

                float target = targetWorldSpeed(children[i].position);
                children[i].timeScale = Mathf.Clamp(target / clipSpeed, MinCalibratedTimeScale, MaxCalibratedTimeScale);
                log.Append($"\n - {clip.name} @ {children[i].position}: clip {clipSpeed:F2} m/s -> target {target:F2} m/s, timeScale {children[i].timeScale:F2}");
            }

            tree.children = children;
            Debug.Log(log.ToString());
        }

        private static void WireStanceTransitions(AnimatorState from, AnimatorState otherA, AnimatorState otherB)
        {
            EnsureStanceTransition(from, otherA, GetStanceForState(otherA));
            EnsureStanceTransition(from, otherB, GetStanceForState(otherB));
        }

        private static int GetStanceForState(AnimatorState state)
        {
            if (state.name.StartsWith("Melee"))
            {
                return 1;
            }

            if (state.name.StartsWith("Ranged"))
            {
                return 2;
            }

            return 0;
        }

        private static void EnsureStanceTransition(AnimatorState from, AnimatorState to, int stanceValue)
        {
            foreach (AnimatorStateTransition transition in from.transitions)
            {
                if (transition.destinationState == to
                    && transition.conditions.Any(c => c.parameter == "CombatStance" && (int)c.threshold == stanceValue))
                {
                    return;
                }
            }

            AnimatorStateTransition created = from.AddTransition(to);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.15f;
            created.AddCondition(AnimatorConditionMode.Equals, stanceValue, "CombatStance");
            // Stay on healthy stance locomotion while not limping.
            created.AddCondition(AnimatorConditionMode.Equals, 0f, "LimpSide");
        }

        private static void WireLimpEntry(AnimatorState from, AnimatorState injured)
        {
            foreach (AnimatorStateTransition transition in from.transitions)
            {
                if (transition.destinationState == injured
                    && transition.conditions.Any(c => c.parameter == "LimpSide"))
                {
                    return;
                }
            }

            AnimatorStateTransition created = from.AddTransition(injured);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.2f;
            // LimpSide: 0 None, 1 Left, 2 Right — any non-zero enters injured gait.
            created.AddCondition(AnimatorConditionMode.Greater, 0f, "LimpSide");
        }

        private static void WireLimpExit(AnimatorState injured, AnimatorState stanceState, int stanceValue)
        {
            foreach (AnimatorStateTransition transition in injured.transitions)
            {
                if (transition.destinationState == stanceState
                    && transition.conditions.Any(c => c.parameter == "CombatStance" && (int)c.threshold == stanceValue))
                {
                    return;
                }
            }

            AnimatorStateTransition created = injured.AddTransition(stanceState);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.2f;
            created.AddCondition(AnimatorConditionMode.Equals, 0f, "LimpSide");
            created.AddCondition(AnimatorConditionMode.Equals, stanceValue, "CombatStance");
        }

        private static void ClearTransitions(AnimatorState state)
        {
            AnimatorStateTransition[] existing = state.transitions;
            for (int i = existing.Length - 1; i >= 0; i--)
            {
                state.RemoveTransition(existing[i]);
            }
        }

        private static void RemoveStateIfPresent(AnimatorStateMachine machine, string stateName)
        {
            AnimatorState state = FindState(machine, stateName);
            if (state != null)
            {
                machine.RemoveState(state);
            }
        }

        private static void RemapStateMotion(AnimatorStateMachine machine, string stateName, string fbxPath, string clipName)
        {
            if (machine == null)
            {
                return;
            }

            AnimatorState state = FindState(machine, stateName);
            if (state == null)
            {
                return;
            }

            AnimationClip clip = LoadPackClip(fbxPath, clipName);
            if (clip != null)
            {
                state.motion = clip;
            }
        }

        private static AnimationClip LoadPackClip(string fbxPath, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && clip.name == clipName && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    Debug.LogWarning($"[HumanoidLocomotionBlendSetup] Using '{clip.name}' instead of '{clipName}' from {fbxPath}");
                    return clip;
                }
            }

            return null;
        }

        private static void EnsureFloatParam(AnimatorController controller, string name)
        {
            if (controller.parameters.Any(p => p.name == name))
            {
                return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Float);
        }

        private static void EnsureIntParam(AnimatorController controller, string name)
        {
            if (controller.parameters.Any(p => p.name == name))
            {
                return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Int);
        }

        private static void EnsureBoolParam(AnimatorController controller, string name)
        {
            if (controller.parameters.Any(p => p.name == name))
            {
                return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Bool);
        }

        private static void EnsureTriggerParam(AnimatorController controller, string name)
        {
            if (controller.parameters.Any(p => p.name == name))
            {
                return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state != null && child.state.name == name)
                {
                    return child.state;
                }
            }

            return null;
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine machine, string name, Vector3 position)
        {
            AnimatorState existing = FindState(machine, name);
            if (existing != null)
            {
                return existing;
            }

            return machine.AddState(name, position);
        }

        private static void EnableMirrorParameter(AnimatorState state, string parameterName)
        {
            if (state == null)
            {
                return;
            }

            state.mirrorParameterActive = true;
            state.mirrorParameter = parameterName;
        }

        private static void EnsureAnyStateTrigger(
            AnimatorStateMachine machine,
            AnimatorState destination,
            string triggerName,
            bool canTransitionToSelf)
        {
            EnsureAnyStateTriggerWithLimpGate(machine, destination, triggerName, requireLimping: null, canTransitionToSelf);
        }

        /// <param name="requireLimping">
        /// null = no limp gate; false = LimpSide == 0; true = LimpSide &gt; 0.
        /// </param>
        private static void EnsureAnyStateTriggerWithLimpGate(
            AnimatorStateMachine machine,
            AnimatorState destination,
            string triggerName,
            bool? requireLimping,
            bool canTransitionToSelf)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.destinationState != destination)
                {
                    continue;
                }

                if (!transition.conditions.Any(c => c.parameter == triggerName))
                {
                    continue;
                }

                transition.mute = false;
                transition.canTransitionToSelf = canTransitionToSelf;
                ApplyLimpGateConditions(transition, triggerName, requireLimping);
                MuteDuplicateAnyStateTriggers(machine, destination, triggerName, transition);
                return;
            }

            AnimatorStateTransition created = machine.AddAnyStateTransition(destination);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.05f;
            created.canTransitionToSelf = canTransitionToSelf;
            created.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
            ApplyLimpGateConditions(created, triggerName, requireLimping);
            MuteDuplicateAnyStateTriggers(machine, destination, triggerName, created);
        }

        private static void MuteDuplicateAnyStateTriggers(
            AnimatorStateMachine machine,
            AnimatorState destination,
            string triggerName,
            AnimatorStateTransition keep)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition == keep || transition.destinationState != destination)
                {
                    continue;
                }

                if (transition.conditions.Any(c => c.parameter == triggerName))
                {
                    transition.mute = true;
                }
            }
        }

        private static void ApplyLimpGateConditions(
            AnimatorStateTransition transition,
            string triggerName,
            bool? requireLimping)
        {
            List<AnimatorCondition> extras = transition.conditions
                .Where(c => c.parameter != triggerName && c.parameter != "LimpSide")
                .ToList();

            while (transition.conditions.Length > 0)
            {
                transition.RemoveCondition(transition.conditions[0]);
            }

            transition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
            if (requireLimping == true)
            {
                transition.AddCondition(AnimatorConditionMode.Greater, 0f, "LimpSide");
            }
            else if (requireLimping == false)
            {
                transition.AddCondition(AnimatorConditionMode.Equals, 0f, "LimpSide");
            }

            foreach (AnimatorCondition c in extras)
            {
                transition.AddCondition(c.mode, c.threshold, c.parameter);
            }
        }

        private static void EnsureExitToStateWithLimpGate(
            AnimatorState from,
            AnimatorState destination,
            bool requireLimping,
            int? combatStance)
        {
            foreach (AnimatorStateTransition transition in from.transitions)
            {
                if (transition.destinationState != destination)
                {
                    continue;
                }

                bool limpOk = requireLimping
                    ? transition.conditions.Any(c => c.parameter == "LimpSide" && c.mode == AnimatorConditionMode.Greater)
                    : transition.conditions.Any(c => c.parameter == "LimpSide" && c.mode == AnimatorConditionMode.Equals);
                bool stanceOk = combatStance == null
                    || transition.conditions.Any(
                        c => c.parameter == "CombatStance" && (int)c.threshold == combatStance.Value);
                if (limpOk && stanceOk)
                {
                    return;
                }
            }

            AnimatorStateTransition created = from.AddTransition(destination);
            created.hasExitTime = true;
            created.exitTime = 0.85f;
            created.hasFixedDuration = true;
            created.duration = 0.1f;
            if (requireLimping)
            {
                created.AddCondition(AnimatorConditionMode.Greater, 0f, "LimpSide");
            }
            else
            {
                created.AddCondition(AnimatorConditionMode.Equals, 0f, "LimpSide");
            }

            if (combatStance.HasValue)
            {
                created.AddCondition(AnimatorConditionMode.Equals, combatStance.Value, "CombatStance");
            }
        }

        private static void EnsureAnyStateTriggerWithInt(
            AnimatorStateMachine machine,
            AnimatorState destination,
            string triggerName,
            string intParamName,
            int intValue,
            bool canTransitionToSelf)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.destinationState != destination)
                {
                    continue;
                }

                bool hasTrigger = transition.conditions.Any(c => c.parameter == triggerName);
                bool hasVariant = transition.conditions.Any(
                    c => c.parameter == intParamName && (int)c.threshold == intValue);
                if (hasTrigger && hasVariant)
                {
                    transition.mute = false;
                    transition.canTransitionToSelf = canTransitionToSelf;
                    return;
                }
            }

            AnimatorStateTransition created = machine.AddAnyStateTransition(destination);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.05f;
            created.canTransitionToSelf = canTransitionToSelf;
            created.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
            created.AddCondition(AnimatorConditionMode.Equals, intValue, intParamName);
        }

        private static void MuteAnyStateTrigger(AnimatorStateMachine machine, string triggerName)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.conditions.Any(c => c.parameter == triggerName))
                {
                    transition.mute = true;
                }
            }
        }

        private static void UnmuteAnyStateTrigger(AnimatorStateMachine machine, string triggerName)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.conditions.Any(c => c.parameter == triggerName))
                {
                    transition.mute = false;
                }
            }
        }

        private static void EnsureExitToState(
            AnimatorState from,
            AnimatorState destination,
            bool hasExitTime,
            float exitTime,
            float duration)
        {
            foreach (AnimatorStateTransition transition in from.transitions)
            {
                if (transition.destinationState == destination)
                {
                    return;
                }
            }

            // Prefer rewriting Exit-node transitions so the clip cannot dump out of the layer SM.
            foreach (AnimatorStateTransition transition in from.transitions)
            {
                if (transition.isExit)
                {
                    transition.isExit = false;
                    transition.destinationState = destination;
                    transition.hasExitTime = hasExitTime;
                    transition.exitTime = exitTime;
                    transition.hasFixedDuration = true;
                    transition.duration = duration;
                    return;
                }
            }

            AnimatorStateTransition created = from.AddTransition(destination);
            created.hasExitTime = hasExitTime;
            created.exitTime = exitTime;
            created.hasFixedDuration = true;
            created.duration = duration;
        }
    }
}
