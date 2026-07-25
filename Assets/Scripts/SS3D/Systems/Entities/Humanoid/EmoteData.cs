using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Data-driven definition of a single emote: which animator emote index it plays under, and
    /// which postures disallow it (e.g. can't wave while prone). Create instances via
    /// Assets > Create > SS3D > Animations > Emote Data, and assign them to a
    /// HumanoidAnimatorController's emote list.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEmoteData", menuName = "SS3D/Animations/Emote Data")]
    public class EmoteData : ScriptableObject
    {
        [SerializeField] private string _emoteName;

        [Tooltip("Must match the Emote_<index> state's EmoteIndex condition on the Animator's Emote layer.")]
        [SerializeField] private int _emoteIndex;

        [Tooltip("Reference only, for bookkeeping/preview in the inspector. The actual clip lives on the Animator's Emote layer state, not here.")]
        [SerializeField] private AnimationClip _clip;

        [SerializeField] private List<Posture> _disallowedPostures = new();

        public string EmoteName => _emoteName;

        public int EmoteIndex => _emoteIndex;

        public AnimationClip Clip => _clip;

        /// <summary>
        /// Settable (not just inspector-serialized) so tests can configure it directly on a
        /// ScriptableObject.CreateInstance instance, matching this codebase's convention for small
        /// data-holder ScriptableObjects (e.g. Trait, Filter).
        /// </summary>
        public List<Posture> DisallowedPostures
        {
            get => _disallowedPostures;
            set => _disallowedPostures = value;
        }

        public bool IsAllowedInPosture(Posture posture)
        {
            return !_disallowedPostures.Contains(posture);
        }
    }
}
