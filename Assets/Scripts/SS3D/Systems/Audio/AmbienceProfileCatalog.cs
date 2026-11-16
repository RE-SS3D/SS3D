using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>Every <see cref="AmbienceProfile"/>, loaded from Resources.</summary>
    public sealed class AmbienceProfileCatalog : ScriptableObject
    {
        /// <summary>Plays for a room with no department, or one whose department has no profile yet.</summary>
        public const string GenericProfileId = "station";

        [SerializeField] private AmbienceProfile[] _profiles = Array.Empty<AmbienceProfile>();
        [SerializeField] private AudioClip _airBed;
        [SerializeField, Range(0f, 1f)] private float _airBedVolume = 0.15f;

        [Tooltip("Light, heavy and violent, in that order. AudioAirState.WindGain crossfades them by wind speed.")]
        [SerializeField] private AudioClip[] _windLayers = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float _windVolume = 0.7f;

        [SerializeField] private AudioClip _spaceBed;
        [SerializeField, Range(0f, 1f)] private float _spaceBedVolume = 0.08f;

        private static AmbienceProfileCatalog _instance;

        public static AmbienceProfileCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AmbienceProfileCatalog>(nameof(AmbienceProfileCatalog));
                }

                return _instance;
            }
        }

        /// <summary>Plays under every room, so a dark room sounds dead rather than broken (audio.md §2).</summary>
        public AudioClip AirBed => _airBed;
        public float AirBedVolume => _airBedVolume;

        /// <summary>The sound of moving air, one clip per band of wind speed (audio.md §2).</summary>
        public AudioClip[] WindLayers => _windLayers;
        public float WindVolume => _windVolume;

        /// <summary>Very faint, so open space doesn't read as broken audio (audio.md §2).</summary>
        public AudioClip SpaceBed => _spaceBed;
        public float SpaceBedVolume => _spaceBedVolume;

        public IEnumerable<string> ProfileIds => _profiles.Select(profile => profile.Id);

        public bool TryGet(string id, out AmbienceProfile profile)
        {
            profile = _profiles.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
            return profile != null;
        }

        /// <summary>The profile named <paramref name="id"/>, or the generic one when there's no such profile.</summary>
        public AmbienceProfile Resolve(string id)
        {
            if (TryGet(id, out AmbienceProfile profile) || TryGet(GenericProfileId, out profile))
            {
                return profile;
            }

            return null;
        }
    }
}
