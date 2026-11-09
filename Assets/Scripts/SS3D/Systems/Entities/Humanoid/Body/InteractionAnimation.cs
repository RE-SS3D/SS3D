using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Interaction clips (animation pass Phase 4). A clip with a contact frame (pickup, drop, lockers) holds its
    /// interaction's effect until the hand gets there: the owner plays the clip at the click and the server runs
    /// the interaction <see cref="Clip.ContactSeconds"/> later. Clips without one (doors) are decoration over an
    /// instant effect. The editor rebuild imports and trims the clips from this table and wires one animator
    /// state per entry: reaches on the Upper Body over locomotion, the floor bends and the kneel on the Base
    /// Layer. <see cref="ChooseClip"/> maps an interaction to its clip.
    /// </summary>
    public static class InteractionAnimation
    {
        /// <summary>Mixamo exports at 30 fps; frame numbers below are source FBX frames.</summary>
        public const float ClipFps = 30f;

        /// <summary>No clip for this interaction.</summary>
        public const int None = -1;

        public const int Reach = 0;
        public const int DoorReach = 1;
        public const int LockerOpen = 2;
        public const int LockerClose = 3;
        public const int LockerTap = 4;
        public const int FloorPickup = 5;
        public const int FloorPutDown = 6;
        public const int Kneel = 7;
        public const int LightSwitch = 8;

        /// <summary>
        /// <see cref="ChooseClip"/> name for a light switch's toggle. Its generic name is the shared "Toggle"
        /// (boombox, valves), so the dispatch passes this when the target is a light switch.
        /// </summary>
        public const string LightSwitchName = "LightSwitch";

        /// <summary>Items and drop points lower than this above the feet use the floor clips.</summary>
        public const float FloorHeight = 0.5f;

        /// <summary>Normalized time the floor bends leave for the stance pose, and the blend length.</summary>
        public const float ExitTime = 0.85f;
        public const float ExitSeconds = 0.3f;

        /// <summary>
        /// Normalized time a reach's last segment (its return, or the clip itself) leaves for the hold pose. Every
        /// reach ends with the arm back at the side, so the exit only matters in combat, where the hold pose is the
        /// guard; in Peaceful the layer has faded out by then.
        /// </summary>
        public const float UpperExitTime = 0.95f;

        /// <summary>The Upper Body layer fades out over this, ending as a reach's arm reaches the side.</summary>
        public const float LayerFadeOutSeconds = 1f / 3f;

        /// <summary>Blend into a clip; a new clip during another restarts it over this crossfade.</summary>
        public const float EntrySeconds = 0.15f;

        /// <summary>Blend from a clip's end into its return segment (the poses there nearly match).</summary>
        public const float ReturnBlendSeconds = 0.15f;

        /// <summary>Blends in and out of the kneel loop.</summary>
        public const float KneelBlendSeconds = 0.35f;

        /// <summary>A full-body oneshot leaves for locomotion at once when the body moves faster than this.</summary>
        public const float MoveExitSpeed = 0.1f;

        /// <summary>Seconds the owner's body holds its facing toward the target (after contact, for a waiting clip).</summary>
        public const float FacingHoldSeconds = 0.3f;

        /// <summary>No waiting clip makes its effect wait longer than this (revised Decision 15).</summary>
        public const float MaxContactSeconds = 0.45f;

        /// <summary>Animator parameters the rebuild and the orchestrator share.</summary>
        public const string TriggerParameter = "Interact";
        public const string ClipParameter = "InteractClip";
        public const string KneelParameter = "InteractKneel";

        public readonly struct Clip
        {
            public readonly string StateName;
            /// <summary>Path relative to Assets/Art/Animations.</summary>
            public readonly string File;
            public readonly string ClipName;
            public readonly int FirstFrame;
            public readonly int LastFrame;
            public readonly float PlaybackRate;
            /// <summary>Base Layer (legs bend too) rather than Upper Body over locomotion.</summary>
            public readonly bool FullBody;
            public readonly bool Loop;
            /// <summary>Source frame where the hand reaches the item, surface or handle; 0 for an instant effect.</summary>
            public readonly int ContactFrame;
            /// <summary>
            /// Optional later segment of the same take (the arm coming down), played after the clip so a take
            /// with a long hold in the middle still lowers the arm on its own motion; 0 when there is none.
            /// </summary>
            public readonly int ReturnFirstFrame;
            public readonly int ReturnLastFrame;

            public Clip(string stateName, string file, string clipName, int firstFrame, int lastFrame,
                float playbackRate, int contactFrame = 0, bool fullBody = false, bool loop = false,
                int returnFirstFrame = 0, int returnLastFrame = 0)
            {
                ContactFrame = contactFrame;
                ReturnFirstFrame = returnFirstFrame;
                ReturnLastFrame = returnLastFrame;
                StateName = stateName;
                File = file;
                ClipName = clipName;
                FirstFrame = firstFrame;
                LastFrame = lastFrame;
                PlaybackRate = playbackRate;
                FullBody = fullBody;
                Loop = loop;
            }

            /// <summary>Seconds of the trimmed clip at its playback rate.</summary>
            public float LengthSeconds => (LastFrame - FirstFrame) / ClipFps / PlaybackRate;

            /// <summary>True when the interaction's effect waits for <see cref="ContactSeconds"/>.</summary>
            public bool WaitsForContact => ContactFrame > FirstFrame;

            /// <summary>Seconds from the clip starting until the hand makes contact; 0 for an instant effect.</summary>
            public float ContactSeconds => WaitsForContact ? (ContactFrame - FirstFrame) / ClipFps / PlaybackRate : 0f;

            public bool HasReturn => ReturnLastFrame > ReturnFirstFrame;
            public string ReturnStateName => StateName + " Return";
            public string ReturnClipName => ClipName + "Return";

            /// <summary>Seconds of the return segment at the clip's playback rate; 0 when there is none.</summary>
            public float ReturnSeconds => HasReturn ? (ReturnLastFrame - ReturnFirstFrame) / ClipFps / PlaybackRate : 0f;

            /// <summary>
            /// Seconds the clip holds its layer up. A floor bend holds the Base Layer until its exit blend ends; a
            /// reach holds the Upper Body until <see cref="LayerFadeOutSeconds"/> before its arm is back at the side,
            /// so the fade rides the clip's own lowering instead of a blend toward the hold pose.
            /// </summary>
            public float PlaySeconds => FullBody
                ? LengthSeconds * ExitTime + ExitSeconds
                : Mathf.Max(ContactSeconds, LengthSeconds + ReturnSeconds * UpperExitTime - LayerFadeOutSeconds);
        }

        private const string Door = "Interact/X Bot@Attempting To Open A Door.fbx";
        private const string Opening = "Interact/X Bot@Opening.fbx";

        /// <summary>Indexed by the constants above. Right-handed; the Upper Body states mirror for the left hand.</summary>
        public static readonly Clip[] All =
        {
            // Door reach, short: from the hand leaving the side (12) to forward at table height (24), then the take's
            // own lowering (108–132, from where 44 leaves the hand). Table pickup and drop; contact 0.23 s.
            new("Interact Reach", Door, "Mix_InteractReach", 12, 44, 1.75f, contactFrame: 24,
                returnFirstFrame: 108, returnLastFrame: 132),
            // Door reach, held: arm out at chest height (24–108 in the source); cut after a second, then the same
            // lowering. Instant.
            new("Interact Door Reach", Door, "Mix_InteractDoorReach", 10, 66, 1.5f,
                returnFirstFrame: 108, returnLastFrame: 132),
            // Owner's Opening, first take, from the hand rising (24): on the handle 42–54, the sweep aside starts at
            // 54 and ends by 96, arm down by 126. Contact (the door starts to swing) 0.44 s.
            new("Interact Locker Open", Opening, "Mix_InteractLockerOpen", 24, 126, 2.25f, contactFrame: 54),
            // Second take, from the reach across (162): on the open door by 192, swings it shut 198–234, arm down by
            // 294. Contact 0.44 s.
            new("Interact Locker Close", Opening, "Mix_InteractLockerClose", 162, 294, 2.25f, contactFrame: 192),
            // The open's reach alone: an ID held to the lock by 40, then the second take's lowering (264–306, the
            // hand where 60 leaves it). Contact 0.34 s.
            new("Interact Locker Tap", Opening, "Mix_InteractLockerTap", 22, 60, 1.75f, contactFrame: 40,
                returnFirstFrame: 264, returnLastFrame: 306),
            // Owner's Picking Up (the same take as Mixamo "Picking Up An Object"), from the hand already going down
            // (36): one-handed, at its lowest 66–72, back up at the chest by 186. Contact 0.40 s.
            new("Interact Floor Pickup", "Interact/X Bot@Picking Up.fbx", "Mix_InteractFloorPickup", 36, 186, 2.5f,
                contactFrame: 66, fullBody: true),
            // Mixamo "Putting Down An Object" from the hand going down (114): at the floor 150–180, up by 246.
            // Contact 0.44 s.
            new("Interact Floor Put Down", "Interact/X Bot@Putting Down An Object.fbx", "Mix_InteractFloorPutDown", 114, 246, 2.75f,
                contactFrame: 150, fullBody: true),
            // Mixamo "Man Working On Device": on one knee with both hands low; 60–132 loops.
            new("Interact Kneel", "Interact/X Bot@Man Working On Device.fbx", "Mix_InteractKneel", 60, 132, 1f,
                fullBody: true, loop: true),
            // Owner's Pulling Lever, from the hand leaving the side (27): up at the switch by 42, pressed slightly
            // down to 66. The source then holds for two seconds; its own lowering (105–132, the hand where 66 left
            // it) follows as the return. Contact 0.29 s.
            new("Interact Light Switch", "Interact/X Bot@Pulling Lever.fbx", "Mix_InteractLightSwitch", 27, 66, 1.75f,
                contactFrame: 42, returnFirstFrame: 105, returnLastFrame: 132),
        };

        public static bool TryGetClip(int index, out Clip clip)
        {
            if (index >= 0 && index < All.Length)
            {
                clip = All[index];
                return true;
            }

            clip = default;
            return false;
        }

        public static bool IsFullBody(int index) => TryGetClip(index, out Clip clip) && clip.FullBody;

        /// <summary>Where a drop with no chosen point (the drop hotkey) puts the hand: just in front of the feet.</summary>
        public static Vector3 DropPointAhead(Transform body) => body.position + body.forward * 0.4f;

        /// <summary>Seconds the effect of an interaction playing <paramref name="index"/> waits; 0 when instant.</summary>
        public static float ContactSeconds(int index) => TryGetClip(index, out Clip clip) ? clip.ContactSeconds : 0f;

        /// <summary>
        /// The clip for an interaction, by its generic name. <paramref name="heightAboveFeet"/> is the item's
        /// (pickup) or drop point's (drop) height; <paramref name="closesLocker"/> is true when a locker door
        /// interaction shuts the door.
        /// </summary>
        public static int ChooseClip(string genericName, float heightAboveFeet = 1f, bool closesLocker = false)
        {
            switch (genericName)
            {
                case "Pickup":
                    return heightAboveFeet < FloorHeight ? FloorPickup : Reach;
                case "Drop":
                    return heightAboveFeet < FloorHeight ? FloorPutDown : Reach;
                case "OpenLocker":
                    return closesLocker ? LockerClose : LockerOpen;
                case "LockLocker":
                case "UnlockLocker":
                    return LockerTap;
                case LightSwitchName:
                    return LightSwitch;
                case "OpenAirlock":
                case "AttachCharge":
                    return DoorReach;
                case "DefuseC4":
                case "Perform CPR":
                    return Kneel;
                default:
                    return None;
            }
        }

        /// <summary>
        /// What a peer actually plays for <paramref name="clip"/>: a full-body bend becomes the standing reach
        /// when the body is moving or not on its feet (Decision 22). The kneel is state, not a oneshot.
        /// </summary>
        public static int ResolvePlayback(int clip, bool moving, bool standing)
        {
            if (clip == Kneel || !TryGetClip(clip, out _))
            {
                return None;
            }

            if (IsFullBody(clip) && (moving || !standing))
            {
                return Reach;
            }

            return clip;
        }
    }
}
