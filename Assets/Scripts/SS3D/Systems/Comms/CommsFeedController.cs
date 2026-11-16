using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Systems.Comms.UI;
using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
using System.Collections;
using System.Collections.Generic;
using SS3D.Systems.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// Drives the non-diegetic radio feed and announcement banner on the UiShell HUD layer.
    /// Announcements always play <see cref="CommsAudioTrackIds.StationAnnounce"/> first; when that
    /// ends, the banner reveals and any optional follow-up clip (<see cref="CommsMessage.SoundId"/>,
    /// e.g. welcome.ogg) starts in parallel.
    /// </summary>
    public sealed class CommsFeedController : SubSystem
    {
        private const string StyleSheetPath = "Assets/Content/Systems/UI/Comms/Feed/CommsFeed.uss";
        private const string RadioIconPath = "Assets/Art/Icons/External/delapouite/radio-tower.svg";
        private const float AnnounceCueVolume = 0.9f;

        [SerializeField] private StyleSheet _styleSheet;
        [SerializeField] private VectorImage _radioIcon;

        private CommsFeedView _view;
        private CommsSubSystem _comms;
        private bool _attached;
        private AudioSource _announceSource;
        private readonly Queue<(string Title, string Body, string FollowUpSoundId)> _pendingAnnouncements = new();
        private bool _announceSequenceRunning;

        protected override void OnAwake()
        {
            base.OnAwake();

#if UNITY_EDITOR
            EnsureEditorAssets();
#endif
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            _comms = SubSystems.Get<CommsSubSystem>();
            if (_comms != null)
            {
                _comms.OnCommsMessageReceived += HandleCommsMessage;
                _comms.StationCueReceived += HandleStationCue;
            }
        }

        protected override void OnDisabled()
        {
            if (_comms != null)
            {
                _comms.OnCommsMessageReceived -= HandleCommsMessage;
                _comms.StationCueReceived -= HandleStationCue;
                _comms = null;
            }

            StopAllCoroutines();
            _pendingAnnouncements.Clear();
            _announceSequenceRunning = false;
            if (_announceSource != null)
            {
                _announceSource.Stop();
            }

            DetachView();
            base.OnDisabled();
        }

        private void Update()
        {
            if (!EnsureView())
            {
                return;
            }

            _view.Tick();
        }

        private void HandleCommsMessage(CommsMessage message)
        {
            if (!EnsureView())
            {
                return;
            }

            CommsChannel channel = null;
            _comms?.TryGetChannel(message.ChannelId, out channel);

            if (message.Kind == CommsChannelKind.Announcement
                || (channel != null && channel.Kind == CommsChannelKind.Announcement))
            {
                string title = channel != null ? channel.ResolveAnnouncementTitle() : "ALL-STATION";
                EnqueueAnnouncement(title, message.Text, message.SoundId);
                return;
            }

            if (message.Kind != CommsChannelKind.Radio)
            {
                return;
            }

            string header = channel != null
                ? channel.ResolveRadioHeader()
                : (message.ChannelId ?? "RADIO").ToUpperInvariant();
            Color accent = channel != null ? channel.Color : new Color(0.37f, 0.53f, 0.7f);
            _view.PushRadio(header, message.Sender, message.Text, accent);
            PlayAnnounceCue(message.SoundId);
        }

        /// <summary>A station cue plays at once on the announcement source, with no banner.</summary>
        private void HandleStationCue(string soundId) => PlayAnnounceCue(soundId);

        private void EnqueueAnnouncement(string title, string body, string followUpSoundId)
        {
            _pendingAnnouncements.Enqueue((title, body ?? string.Empty, followUpSoundId ?? string.Empty));
            if (!_announceSequenceRunning)
            {
                StartCoroutine(PlayAnnouncementSequence());
            }
        }

        private IEnumerator PlayAnnouncementSequence()
        {
            _announceSequenceRunning = true;

            while (_pendingAnnouncements.Count > 0)
            {
                (string title, string body, string followUpSoundId) = _pendingAnnouncements.Dequeue();

                float delay = PlayAnnounceCue(CommsAudioTrackIds.StationAnnounce);
                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }

                // Reveal + optional follow-up (welcome.ogg) start together after the announce chime.
                if (!string.IsNullOrEmpty(followUpSoundId)
                    && followUpSoundId != CommsAudioTrackIds.StationAnnounce)
                {
                    PlayAnnounceCue(followUpSoundId);
                }

                if (_view != null)
                {
                    _view.ShowAnnouncement(title, body);
                }
            }

            _announceSequenceRunning = false;
        }

        /// <summary>
        /// Plays the given announcement chime; returns clip length in seconds (0 if unavailable).
        /// </summary>
        private float PlayAnnounceCue(string soundId)
        {
            if (string.IsNullOrEmpty(soundId)
                || !Assets.TryGet(AssetDatabases.Sounds, soundId, out AudioClip clip)
                || clip == null)
            {
                return 0f;
            }

            EnsureAnnounceSource();
            _announceSource.PlayOneShot(clip, AnnounceCueVolume);
            return Mathf.Max(0.01f, clip.length);
        }

        private void EnsureAnnounceSource()
        {
            if (_announceSource != null)
            {
                return;
            }

            GameObject host = new("StationAnnounceSource");
            host.transform.SetParent(Transform, false);
            _announceSource = host.AddComponent<AudioSource>();
            _announceSource.playOnAwake = false;
            _announceSource.loop = false;
            _announceSource.spatialBlend = 0f; // Non-diegetic UI chime — not positional.
            _announceSource.volume = 1f;
            // Non-diegetic cue heard by this player only, like the alert ding: Personal, not SFX.
            _announceSource.outputAudioMixerGroup = MainAudioMixer.FindGroup(MainAudioMixer.PersonalGroup);
        }

        private bool EnsureView()
        {
            if (_attached && _view != null)
            {
                return true;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem uiShell)
                || !uiShell.TryGetLayer(UiLayer.Hud, out VisualElement layerRoot))
            {
                return false;
            }

            InputInterface.RegisterDocument(uiShell.Document);

            _view = new CommsFeedView(_styleSheet, _radioIcon);
            _view.Attach(layerRoot);
            _attached = true;
            return true;
        }

        private void DetachView()
        {
            _view?.Detach();
            _view = null;
            _attached = false;
        }

#if UNITY_EDITOR
        private void EnsureEditorAssets()
        {
            if (_styleSheet == null)
            {
                _styleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            }

            if (_radioIcon == null)
            {
                _radioIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<VectorImage>(RadioIconPath);
            }
        }
#endif
    }
}
