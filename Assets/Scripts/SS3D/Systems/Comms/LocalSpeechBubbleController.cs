using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.ActionMessages;
using SS3D.Systems.Comms.UI;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// Drives the local speech subtitle overlay and T-compose draft on the UiShell Overlay layer
    /// (same attachment pattern as radial / armed — never share MachineInterfaceHost's hub UIDocument).
    /// </summary>
    public sealed class LocalSpeechBubbleController : Actor
    {
        private const float BubbleWorldHeightOffset = 0.15f;

        [SerializeField] private StyleSheet _bubbleStyleSheet;
        [SerializeField] private LocalSpeechConfig _config;

        private struct ActiveSpeech
        {
            public string Text;
            public SpeechMode Mode;
            public float StartTime;
            public float ExpiryTime;
            public int GarbleSeed;
            public AudibilityTier CurrentTier;

            /// <summary><see cref="CrowdCapRanker"/> tier; speech entries are 0.</summary>
            public int Tier;
        }

        private readonly Dictionary<Entity, List<ActiveSpeech>> _activeSpeeches = new();
        private readonly List<Entity> _shownSpeakers = new();
        private readonly List<Entity> _staleSpeakers = new();
        private readonly Dictionary<Entity, int> _speakerTiers = new();
        private readonly InputTextEntryScope _composeEntry = new(InputContext.TextEntry);

        private LocalSpeechBubbleView _view;
        private VisualElement _layerHost;
        private LocalSpeechListener _listener;
        private CrowdCapRanker _ranker;
        private Entity _localViewer;
        private CommsSubSystem _commsSubSystem;
        private ActionMessageSubSystem _actionMessages;
        private InputSubSystem _inputSubSystem;
        private float _nextRerankTime;
        private int _overflowCount;
        private bool _overlayReady;
        private bool _isComposing;
        private SpeechMode _composeMode = SpeechMode.Speak;
        private int _composeChannelIndex;
        private int _lastTabCycleFrame = -1;
        private bool _tabHeld;
        private readonly List<CommsChannel> _composeRadioChannels = new();

        protected override void OnAwake()
        {
            base.OnAwake();

#if UNITY_EDITOR
            EnsureEditorAssets();
#endif

            _listener = new LocalSpeechListener(_config);
            _ranker = new CrowdCapRanker();

            AddHandle(LocalPlayerObjectChanged.AddListener(HandlePlayerObjectChanged));
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            _commsSubSystem = SubSystems.Get<CommsSubSystem>();
            if (_commsSubSystem != null)
            {
                _commsSubSystem.OnLocalSpeechReceived += HandleSpeechReceived;
            }

            _inputSubSystem = SubSystems.Get<InputSubSystem>();
            if (_inputSubSystem != null)
            {
                _inputSubSystem.OpenLocalSpeechCompose.performed += HandleOpenCompose;
            }

            EnsureOverlay();
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            if (_commsSubSystem != null)
            {
                _commsSubSystem.OnLocalSpeechReceived -= HandleSpeechReceived;
            }

            if (_inputSubSystem != null)
            {
                _inputSubSystem.OpenLocalSpeechCompose.performed -= HandleOpenCompose;
            }

            if (_actionMessages != null)
            {
                _actionMessages.ClientLineReceived -= HandleActionLine;
                _actionMessages = null;
            }

            EndCompose(clearText: true);
            TearDownOverlay();
        }

        protected override void OnDestroyed()
        {
            EndCompose(clearText: true);
            TearDownOverlay();
            base.OnDestroyed();
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            _localViewer = e.PlayerHasObject ? e.PlayerObject.GetComponent<Entity>() : null;
            if (_localViewer == null)
            {
                EndCompose(clearText: true);
            }
        }

        private void HandleOpenCompose(InputAction.CallbackContext context)
        {
            if (!context.performed || _isComposing)
            {
                return;
            }

            BeginCompose();
        }

        private void BeginCompose()
        {
            if (_localViewer == null || !_localViewer.TryGetComponent(out LocalSpeechEmitter _))
            {
                return;
            }

            if (!EnsureOverlay() || _view?.DraftField == null)
            {
                return;
            }

            _isComposing = true;
            _composeMode = SpeechMode.Speak;
            _composeChannelIndex = 0;
            _tabHeld = false;
            RebuildComposeChannelList();
            _composeEntry.Enter();
            _view.SetRetainDraftFocus(true);
            _view.DraftField.value = string.Empty;
            // Enter/Escape via UITK; Tab is polled from Keyboard in LateUpdate because UITK
            // focus navigation swallows Tab before InputActions / field KeyDown see it.
            _view.DraftField.RegisterCallback<KeyDownEvent>(HandleDraftKeyDown, TrickleDown.TrickleDown);
            _view.DraftField.RegisterCallback<NavigationMoveEvent>(HandleDraftNavigationMove, TrickleDown.TrickleDown);
            if (_layerHost != null)
            {
                _layerHost.RegisterCallback<KeyDownEvent>(HandleDraftKeyDown, TrickleDown.TrickleDown);
                _layerHost.RegisterCallback<NavigationMoveEvent>(HandleDraftNavigationMove, TrickleDown.TrickleDown);
            }

            _view.FocusDraft();
        }

        private void RebuildComposeChannelList()
        {
            _composeRadioChannels.Clear();
            CommsSubSystem comms = _commsSubSystem;
            if (comms == null)
            {
                comms = SubSystems.Get<CommsSubSystem>();
            }

            if (comms == null)
            {
                return;
            }

            _composeRadioChannels.AddRange(comms.GetWritableRadioChannels());
        }

        private bool IsComposeOnRadio => _composeChannelIndex > 0
            && _composeChannelIndex <= _composeRadioChannels.Count;

        private CommsChannel CurrentComposeRadioChannel =>
            IsComposeOnRadio ? _composeRadioChannels[_composeChannelIndex - 1] : null;

        /// <summary>
        /// Prefix overrides Tab selection for draft chrome (and commit).
        /// </summary>
        private void ResolveComposeDraftChrome(out SpeechMode mode, out string channelHeader, out bool isAnnouncement)
        {
            mode = _composeMode;
            channelHeader = null;
            isAnnouncement = false;

            string draftText = _view?.DraftField?.value;
            CommsSubSystem comms = _commsSubSystem != null ? _commsSubSystem : SubSystems.Get<CommsSubSystem>();
            if (CommsComposePrefix.TrySplit(draftText, out string token, out _)
                && comms != null
                && comms.TryResolveComposePrefix(token, out CommsChannel prefixChannel))
            {
                if (prefixChannel.Kind == CommsChannelKind.Announcement)
                {
                    isAnnouncement = true;
                    mode = SpeechMode.Announcement;
                    channelHeader = prefixChannel.ResolveAnnouncementTitle();
                    return;
                }

                mode = SpeechMode.Speak;
                channelHeader = prefixChannel.ResolveRadioHeader();
                return;
            }

            CommsChannel radio = CurrentComposeRadioChannel;
            if (radio != null)
            {
                mode = SpeechMode.Speak;
                channelHeader = radio.ResolveRadioHeader();
            }
        }

        private void CycleComposeChannel(int delta)
        {
            // Rebuild in case channel settings recovered after compose opened with an empty list.
            if (_composeRadioChannels.Count == 0)
            {
                RebuildComposeChannelList();
            }

            int count = 1 + _composeRadioChannels.Count;
            if (count <= 1)
            {
                return;
            }

            // Debounce: Keyboard poll + UITK NavigationMove can both fire in one frame.
            if (_lastTabCycleFrame == Time.frameCount)
            {
                return;
            }

            _lastTabCycleFrame = Time.frameCount;
            _composeChannelIndex = (_composeChannelIndex + delta) % count;
            if (_composeChannelIndex < 0)
            {
                _composeChannelIndex += count;
            }
        }

        private void CommitCompose()
        {
            if (!_isComposing || _view?.DraftField == null)
            {
                return;
            }

            string text = _view.DraftField.value?.Replace("\r", string.Empty).Replace("\n", " ").Trim()
                ?? string.Empty;

            if (string.IsNullOrEmpty(text))
            {
                EndCompose(clearText: true);
                return;
            }

            if (_localViewer == null || !_localViewer.TryGetComponent(out LocalSpeechEmitter emitter))
            {
                EndCompose(clearText: true);
                return;
            }

            CommsSubSystem comms = _commsSubSystem != null ? _commsSubSystem : SubSystems.Get<CommsSubSystem>();
            // /bug is a chat command handled with OOC, never spoken aloud.
            if (comms != null && Rounds.BugCommand.TryParse(text, out _))
            {
                comms.SendOoc(OocChannel.Ooc, text);
                EndCompose(clearText: true);
                return;
            }

            if (CommsComposePrefix.TrySplit(text, out string token, out string body)
                && comms != null
                && comms.TryResolveComposePrefix(token, out CommsChannel prefixChannel))
            {
                body = body.Trim();
                if (string.IsNullOrEmpty(body))
                {
                    EndCompose(clearText: true);
                    return;
                }

                if (prefixChannel.Kind == CommsChannelKind.Announcement)
                {
                    emitter.CmdSendAnnouncement(body);
                }
                else
                {
                    emitter.CmdSendRadio(prefixChannel.name, body);
                }

                EndCompose(clearText: true);
                return;
            }

            CommsChannel radioChannel = CurrentComposeRadioChannel;
            if (radioChannel != null)
            {
                emitter.CmdSendRadio(radioChannel.name, text);
            }
            else
            {
                SpeechMode mode = ResolveComposeModeFromModifiers();
                emitter.CmdSpeak(text, mode);
            }

            EndCompose(clearText: true);
        }

        private void EndCompose(bool clearText)
        {
            if (_view != null)
            {
                _view.SetRetainDraftFocus(false);
            }

            if (_view?.DraftField != null)
            {
                _view.DraftField.UnregisterCallback<KeyDownEvent>(HandleDraftKeyDown, TrickleDown.TrickleDown);
                _view.DraftField.UnregisterCallback<NavigationMoveEvent>(HandleDraftNavigationMove, TrickleDown.TrickleDown);
                if (clearText)
                {
                    _view.DraftField.value = string.Empty;
                }
            }

            if (_layerHost != null)
            {
                _layerHost.UnregisterCallback<KeyDownEvent>(HandleDraftKeyDown, TrickleDown.TrickleDown);
                _layerHost.UnregisterCallback<NavigationMoveEvent>(HandleDraftNavigationMove, TrickleDown.TrickleDown);
            }

            _view?.HideDraft();
            _composeEntry.Exit();
            _isComposing = false;
            _composeMode = SpeechMode.Speak;
            _composeChannelIndex = 0;
            _tabHeld = false;
        }

        private void HandleDraftNavigationMove(NavigationMoveEvent evt)
        {
            if (!_isComposing)
            {
                return;
            }

            // UITK routes Tab / Shift+Tab as focus navigation, not always as KeyDownEvent.
            if (evt.direction == NavigationMoveEvent.Direction.Next)
            {
                SuppressUiToolkitDefault(evt);
                CycleComposeChannel(1);
                return;
            }

            if (evt.direction == NavigationMoveEvent.Direction.Previous)
            {
                SuppressUiToolkitDefault(evt);
                CycleComposeChannel(-1);
            }
        }

        private void HandleDraftKeyDown(KeyDownEvent evt)
        {
            if (!_isComposing)
            {
                return;
            }

            if (evt.keyCode == KeyCode.Tab || evt.character == '\t')
            {
                SuppressUiToolkitDefault(evt);
                bool reverse = evt.shiftKey
                    || (Keyboard.current != null
                        && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed));
                CycleComposeChannel(reverse ? -1 : 1);
                return;
            }

            bool isSubmit = evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter
                || evt.character is '\n' or '\r';
            if (isSubmit)
            {
                SuppressUiToolkitDefault(evt);
                CommitCompose();
                return;
            }

            if (evt.keyCode == KeyCode.Escape)
            {
                SuppressUiToolkitDefault(evt);
                EndCompose(clearText: true);
            }
        }

        private static void SuppressUiToolkitDefault(EventBase evt)
        {
            evt.StopImmediatePropagation();
            evt.PreventDefault();
            if (evt.currentTarget is VisualElement element)
            {
                element.focusController?.IgnoreEvent(evt);
            }
            else if (evt.target is VisualElement target)
            {
                target.focusController?.IgnoreEvent(evt);
            }
        }

        private static SpeechMode ResolveComposeModeFromModifiers()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return SpeechMode.Speak;
            }

            // Ctrl+Enter = shout takes priority over Shift+Enter = whisper (comms.md §5).
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            {
                return SpeechMode.Shout;
            }

            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                return SpeechMode.Whisper;
            }

            return SpeechMode.Speak;
        }

        private void HandleSpeechReceived(Entity speaker, SpeechEvent speechEvent)
        {
            if (speaker == null)
            {
                return;
            }

            float now = Time.time;
            float duration = _config.GetFadeDuration(speechEvent.Text.Length);

            if (!_activeSpeeches.TryGetValue(speaker, out List<ActiveSpeech> stack))
            {
                stack = new List<ActiveSpeech>(_config.MaxStackedMessagesPerSpeaker);
                _activeSpeeches[speaker] = stack;
            }

            stack.Add(new ActiveSpeech
            {
                Text = speechEvent.Text,
                Mode = speechEvent.Mode,
                StartTime = now,
                ExpiryTime = now + duration,
                GarbleSeed = speaker.GetInstanceID() ^ speechEvent.ServerTimestamp.GetHashCode(),
                CurrentTier = AudibilityTier.Inaudible,
            });

            int maxStack = Mathf.Max(1, _config.MaxStackedMessagesPerSpeaker);
            while (stack.Count > maxStack)
            {
                stack.RemoveAt(0);
            }
        }

        /// <summary>
        /// An action line is a chip on the actor in the speech stack (one crowd cap, action-messages.md
        /// §5). A line about you with no actor sits on your own head.
        /// </summary>
        private void HandleActionLine(ActionLine line)
        {
            Entity anchor = line.Actor != null ? line.Actor : line.AimedAtViewer ? _localViewer : null;

            if (anchor == null || string.IsNullOrEmpty(line.Text))
            {
                return;
            }

            if (!_activeSpeeches.TryGetValue(anchor, out List<ActiveSpeech> stack))
            {
                stack = new List<ActiveSpeech>(_config.MaxStackedMessagesPerSpeaker);
                _activeSpeeches[anchor] = stack;
            }

            float now = Time.time;
            int tier = line.Hostile && line.AimedAtViewer ? CrowdCapRanker.HostileActionTier
                : line.AimedAtViewer ? CrowdCapRanker.ActionOnViewerTier
                : CrowdCapRanker.RoutineActionTier;
            stack.Add(new ActiveSpeech
            {
                Text = line.Text,
                Mode = SpeechMode.Action,
                StartTime = now,
                ExpiryTime = now + _config.ActionFadeDuration,
                CurrentTier = AudibilityTier.Clear,
                Tier = tier,
            });

            while (stack.Count > Mathf.Max(1, _config.MaxStackedMessagesPerSpeaker))
            {
                stack.RemoveAt(0);
            }

            _nextRerankTime = 0f;
        }

        private void TryBindActionMessages()
        {
            if (_actionMessages != null || !SubSystems.TryGet(out ActionMessageSubSystem actions))
            {
                return;
            }

            _actionMessages = actions;
            _actionMessages.ClientLineReceived += HandleActionLine;
        }

        private void Update()
        {
            TryBindActionMessages();
            if (_isComposing)
            {
                _composeMode = PeekComposeModeFromModifiers();
            }

            if (_localViewer == null || (_activeSpeeches.Count == 0 && !_isComposing))
            {
                RenderFrame();
                return;
            }

            RemoveExpiredEntries();

            if (Time.time >= _nextRerankTime)
            {
                RecomputeVisibleSet();
                _nextRerankTime = Time.time + _config.RerankIntervalSeconds;
            }

            RenderFrame();
        }

        /// <summary>
        /// Tab must be read from the Keyboard device after UITK has processed the frame.
        /// Focused TextFields swallow Tab as focus navigation; InputActions stay silent too.
        /// </summary>
        private void LateUpdate()
        {
            if (!_isComposing)
            {
                _tabHeld = false;
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                _tabHeld = false;
                return;
            }

            bool tabDown = keyboard.tabKey.isPressed;
            if (tabDown && !_tabHeld)
            {
                bool reverse = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                CycleComposeChannel(reverse ? -1 : 1);
            }

            _tabHeld = tabDown;
        }

        private static SpeechMode PeekComposeModeFromModifiers()
        {
            // Live preview while holding modifiers (same priority as commit).
            return ResolveComposeModeFromModifiers();
        }

        private void RemoveExpiredEntries()
        {
            _staleSpeakers.Clear();
            float now = Time.time;

            foreach (KeyValuePair<Entity, List<ActiveSpeech>> pair in _activeSpeeches)
            {
                if (pair.Key == null)
                {
                    _staleSpeakers.Add(pair.Key);
                    continue;
                }

                List<ActiveSpeech> stack = pair.Value;
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    if (now >= stack[i].ExpiryTime)
                    {
                        stack.RemoveAt(i);
                    }
                }

                if (stack.Count == 0)
                {
                    _staleSpeakers.Add(pair.Key);
                }
            }

            foreach (Entity speaker in _staleSpeakers)
            {
                _activeSpeeches.Remove(speaker);
            }
        }

        private void RecomputeVisibleSet()
        {
            List<Entity> audibleSpeakers = new(_activeSpeeches.Count);

            foreach (KeyValuePair<Entity, List<ActiveSpeech>> pair in _activeSpeeches)
            {
                Entity speaker = pair.Key;
                if (speaker == null)
                {
                    continue;
                }

                AudibilityTier tier = _listener.ComputeTier(_localViewer, speaker);
                List<ActiveSpeech> stack = pair.Value;

                bool shown = false;
                int speakerTier = int.MaxValue;
                for (int i = 0; i < stack.Count; i++)
                {
                    ActiveSpeech entry = stack[i];
                    if (entry.Mode == SpeechMode.Action)
                    {
                        // Already filtered by who could perceive it; never garbled by distance.
                        shown = true;
                        speakerTier = Mathf.Min(speakerTier, entry.Tier);
                        continue;
                    }

                    entry.CurrentTier = tier;
                    stack[i] = entry;
                    if (tier != AudibilityTier.Inaudible)
                    {
                        shown = true;
                        speakerTier = CrowdCapRanker.SpeechTier;
                    }
                }

                if (shown)
                {
                    audibleSpeakers.Add(speaker);
                    _speakerTiers[speaker] = speakerTier;
                }
            }

            CrowdCapRanker.Result result = _ranker.Rank(_localViewer, audibleSpeakers, _config.MaxVisibleBubbles,
                speaker => _speakerTiers.TryGetValue(speaker, out int t) ? t : CrowdCapRanker.SpeechTier);
            _speakerTiers.Clear();

            _shownSpeakers.Clear();
            _shownSpeakers.AddRange(result.Shown);
            _overflowCount = result.OverflowCount;
        }

        private void RenderFrame()
        {
            if (!EnsureOverlay())
            {
                return;
            }

            Camera camera = Camera.main;
            int shownCount = 0;
            float now = Time.time;
            Vector3? localDraftScreen = null;

            if (camera != null && _localViewer != null && _localViewer.ViewPoint != null)
            {
                Vector3 localAnchor = _localViewer.ViewPoint.transform.position + Vector3.up * BubbleWorldHeightOffset;
                Vector3 localScreen = camera.WorldToScreenPoint(localAnchor);
                bool localOnScreen = localScreen.z > 0f
                    && localScreen.x >= 0f && localScreen.x <= Screen.width
                    && localScreen.y >= 0f && localScreen.y <= Screen.height;
                if (localOnScreen)
                {
                    localDraftScreen = localScreen;
                }
            }

            if (camera != null)
            {
                for (int i = 0; i < _shownSpeakers.Count; i++)
                {
                    Entity speaker = _shownSpeakers[i];
                    if (speaker == null || speaker.ViewPoint == null || !_activeSpeeches.TryGetValue(speaker, out List<ActiveSpeech> stack))
                    {
                        continue;
                    }

                    Vector3 anchor = speaker.ViewPoint.transform.position + Vector3.up * BubbleWorldHeightOffset;
                    Vector3 screenPoint = camera.WorldToScreenPoint(anchor);

                    bool onScreen = screenPoint.z > 0f
                        && screenPoint.x >= 0f && screenPoint.x <= Screen.width
                        && screenPoint.y >= 0f && screenPoint.y <= Screen.height;

                    if (!onScreen)
                    {
                        continue;
                    }

                    string speakerName = LocalSpeechChipText.ResolveSpeakerName(speaker);

                    // While the local player is drafting, reserve the head slot so existing
                    // lines lift the same way they will after commit — nothing jumps.
                    int stackBias = _isComposing && speaker == _localViewer ? 1 : 0;

                    // Newest nearest the head; older lines stack upward with per-line drift.
                    for (int fromNewest = 0; fromNewest < stack.Count; fromNewest++)
                    {
                        int index = stack.Count - 1 - fromNewest;
                        ActiveSpeech entry = stack[index];
                        bool isNewest = fromNewest == 0 && stackBias == 0;
                        int visualAge = fromNewest + stackBias;

                        string displayText = LocalSpeechChipText.FormatDisplayText(entry.Text, entry.Mode, entry.GarbleSeed, entry.CurrentTier, speakerName, isNewest);
                        float age = now - entry.StartTime;
                        float drift = age * _config.DriftPixelsPerSecond;
                        float stackLift = visualAge * _config.StackSpacingPixels;
                        float opacity = LocalSpeechChipText.ComputeFadeOpacity(entry.ExpiryTime) * LocalSpeechChipText.StackOpacity(visualAge);
                        string nameForChip = LocalSpeechChipText.ResolveNameLabel(entry.Mode, speakerName, isNewest);

                        _view.ShowBubble(
                            shownCount,
                            screenPoint.x,
                            screenPoint.y + stackLift + drift,
                            nameForChip,
                            displayText,
                            entry.Mode,
                            entry.CurrentTier,
                            opacity,
                            visualAge);
                        shownCount++;
                    }
                }
            }

            _view.HideBubblesFrom(shownCount);

            if (_isComposing && localDraftScreen.HasValue)
            {
                ResolveComposeDraftChrome(out SpeechMode draftMode, out string channelHeader, out bool isAnnouncement);
                _view.ShowDraft(
                    localDraftScreen.Value.x,
                    localDraftScreen.Value.y,
                    speakerName: null,
                    draftMode,
                    channelHeader,
                    isAnnouncement);
            }
            else if (!_isComposing)
            {
                _view.HideDraft();
            }

            if (_overflowCount > 0)
            {
                _view.ShowOverflowChip(_overflowCount);
            }
            else
            {
                _view.HideOverflowChip();
            }
        }

        private bool EnsureOverlay()
        {
            if (_overlayReady && _view != null && _layerHost != null && _layerHost.panel != null)
            {
                return true;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem uiShell)
                || !uiShell.TryGetLayer(UiLayer.Overlay, out VisualElement layerRoot)
                || layerRoot.panel == null)
            {
                return false;
            }

            // Shared shell document — register is idempotent; do not unregister on teardown
            // (same rule as RadialInteractionSubSystem).
            InputInterface.RegisterDocument(uiShell.Document);

            TearDownOverlay();

            _layerHost = new VisualElement { name = "local-speech-overlay" };
            _layerHost.pickingMode = PickingMode.Ignore;
            _layerHost.style.position = UnityEngine.UIElements.Position.Absolute;
            _layerHost.style.left = 0;
            _layerHost.style.top = 0;
            _layerHost.style.right = 0;
            _layerHost.style.bottom = 0;
            layerRoot.Add(_layerHost);

            _view = new LocalSpeechBubbleView(_bubbleStyleSheet);
            _view.Attach(_layerHost);
            _overlayReady = true;
            return true;
        }

        private void TearDownOverlay()
        {
            _view?.Detach();
            _view = null;
            _layerHost?.RemoveFromHierarchy();
            _layerHost = null;
            _overlayReady = false;
        }

#if UNITY_EDITOR
        private void EnsureEditorAssets()
        {
            if (_bubbleStyleSheet == null)
            {
                _bubbleStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/Comms/LocalSpeechBubbles/LocalSpeechBubble.uss");
            }

            if (_config == null)
            {
                _config = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalSpeechConfig>(
                    "Assets/Content/Systems/UI/Comms/LocalSpeechBubbles/LocalSpeechConfig.asset");
            }
        }
#endif
    }
}
