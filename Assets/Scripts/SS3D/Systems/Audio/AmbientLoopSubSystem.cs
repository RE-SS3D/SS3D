using System.Collections.Generic;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Generated;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Plays machine hums on a fixed set of voices (audio.md §3). A station has far more machines
    /// than a listener can pick out, so only the nearest running ones sound, each voice following
    /// its machine and occluded like any other positional sound. Client-local: nothing here is
    /// networked. Self-bootstrapped by <c>SystemsBootstrap</c>.
    /// </summary>
    public sealed class AmbientLoopSubSystem : SubSystem
    {
        public const int VoiceCount = 12;

        private const float SelectInterval = 0.25f;
        private const float FadePerSecond = 2.5f;

        private readonly Voice[] _voices = new Voice[VoiceCount];
        private readonly List<AmbientLoopCandidate> _candidates = new();
        private readonly HashSet<int> _selected = new();

        private float _nextSelectTime;

        private sealed class Voice
        {
            public AudioSource Source;
            public AudioSourceOcclusion Occlusion;
            public AmbientLoopSource Owner;
            public float Volume;
            public float Fade;
        }

        protected override void OnAwake()
        {
            base.OnAwake();

#if !UNITY_SERVER
            for (int i = 0; i < VoiceCount; i++)
            {
                _voices[i] = BuildVoice(i);
            }

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
#endif
        }

        private Voice BuildVoice(int index)
        {
            GameObject host = new($"AmbientLoopVoice{index}");
            host.transform.SetParent(Transform, false);

            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.outputAudioMixerGroup = MainAudioMixer.FindGroup(MainAudioMixer.SfxGroup);

            return new Voice { Source = source, Occlusion = host.AddComponent<AudioSourceOcclusion>() };
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent e)
        {
            if (Time.time >= _nextSelectTime)
            {
                _nextSelectTime = Time.time + SelectInterval;
                AssignVoices();
            }

            float step = FadePerSecond * Time.deltaTime;
            foreach (Voice voice in _voices)
            {
                DriveVoice(voice, step);
            }
        }

        private void AssignVoices()
        {
            if (!AudioSourceOcclusion.TryGetListenerPosition(out Vector3 listener))
            {
                return;
            }

            IReadOnlyList<AmbientLoopSource> sources = AmbientLoopSource.Sources;
            _candidates.Clear();
            for (int i = 0; i < sources.Count; i++)
            {
                AmbientLoopSource source = sources[i];
                if (source.IsRunning)
                {
                    float maxDistance = AmbientLoopTrackIds.Get(source.Kind).MaxDistance;
                    float sqrDistance = (source.transform.position - listener).sqrMagnitude;
                    _candidates.Add(new AmbientLoopCandidate(i, sqrDistance, maxDistance));
                }
            }

            AmbientLoopSelection.SelectNearest(_candidates, VoiceCount, _selected);

            // Release first, so a voice let go here can't be counted as free until it has faded.
            foreach (Voice voice in _voices)
            {
                if (voice.Owner != null && !IsSelected(voice.Owner, sources))
                {
                    voice.Owner = null;
                }
            }

            foreach (int index in _selected)
            {
                if (!HasVoice(sources[index]))
                {
                    TryStartVoice(sources[index]);
                }
            }
        }

        private bool IsSelected(AmbientLoopSource owner, IReadOnlyList<AmbientLoopSource> sources)
        {
            foreach (int index in _selected)
            {
                if (sources[index] == owner)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasVoice(AmbientLoopSource source)
        {
            foreach (Voice voice in _voices)
            {
                if (voice.Owner == source)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryStartVoice(AmbientLoopSource owner)
        {
            AmbientLoopSettings settings = AmbientLoopTrackIds.Get(owner.Kind);
            if (!Assets.TryGet(AssetDatabases.Sounds, settings.TrackId, out AudioClip clip))
            {
                return;
            }

            foreach (Voice voice in _voices)
            {
                if (voice.Owner == null && !voice.Source.isPlaying)
                {
                    voice.Owner = owner;
                    voice.Volume = settings.Volume;
                    voice.Fade = 0f;
                    voice.Source.transform.position = owner.transform.position;
                    voice.Source.maxDistance = settings.MaxDistance;
                    voice.Source.clip = clip;
                    voice.Occlusion.PrepareForPlayback(0f);

                    // A random start keeps neighbouring machines from phasing against each other.
                    voice.Source.time = Random.value * clip.length * 0.99f;
                    voice.Source.Play();
                    return;
                }
            }
        }

        private static void DriveVoice(Voice voice, float step)
        {
            if (!voice.Source.isPlaying)
            {
                return;
            }

            bool owned = voice.Owner != null;
            if (owned)
            {
                voice.Source.transform.position = voice.Owner.transform.position;
            }

            voice.Fade = Mathf.MoveTowards(voice.Fade, owned ? 1f : 0f, step);
            voice.Occlusion.SetBaseVolume(voice.Volume * voice.Fade);
            if (!owned && voice.Fade <= 0f)
            {
                voice.Source.Stop();
            }
        }
    }
}
