using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// What one kind of place sounds like (audio.md §2). A department's profile has the department's
    /// name in lower case as its id, and a room can name any other profile as its override.
    /// </summary>
    [CreateAssetMenu(fileName = "AmbienceProfile", menuName = "SS3D/Audio/Ambience Profile")]
    public sealed class AmbienceProfile : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private AudioClip _bed;
        [SerializeField, Range(0f, 1f)] private float _bedVolume = 1f;

        [Tooltip("The bed implies a running machine, so it fades when the area's Equipment channel goes off.")]
        [SerializeField] private bool _powerGated;

        [SerializeField] private AudioClip[] _occasional = System.Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float _occasionalVolume = 0.5f;

        public string Id => _id;
        public AudioClip Bed => _bed;
        public float BedVolume => _bedVolume;
        public bool PowerGated => _powerGated;
        public AudioClip[] Occasional => _occasional;
        public float OccasionalVolume => _occasionalVolume;
    }
}
