using SS3D.Core;
using System.Linq;
using FishNet;
using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Comms;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Roles;
using SS3D.Systems.Roles.Messages;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using SS3D.Systems.ScreenEffects;
using SS3D.Systems.Settings;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions;
using SS3D.Systems.Tile.MapEditor;
using SS3D.UI.Shell;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cloth = SS3D.Systems.Inventory.Containers.Cloth;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// Self-bootstrapping host for the UITK pre-round lobby shell and Character Creator.
    /// Attaches into <see cref="UiLayer.Modal"/>. Phase E1: live booth preview + local draft.
    /// Owns session visibility, ready/late-join and administrator round requests. While the local
    /// player has a body, Escape (<c>Other/Toggle Menu</c>) opens the shell as an in-round menu.
    /// </summary>
    public sealed class LobbyUiSubSystem : SubSystem
    {
        private LobbyAssetCatalog _catalog;
        private LobbyShellView _shellView;
        private CharacterCreatorView _creatorView;
        private CharacterPreviewBooth _booth;
        private CharacterCreatorDraft _draft;
        private CharacterProfileStore _profileStore;
        private bool _attached;
        private readonly Dictionary<string, RenderTexture> _portraits = new();
        private bool _portraitDirty;
        private int _portraitDirtyFrame;
        private const int PortraitSize = 128;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;

            if (SubSystems.TryGet(out LobbyUiSubSystem _))
            {
                return;
            }

            GameObject host = new(nameof(LobbyUiSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<LobbyUiSubSystem>();
        }

        protected override void OnDestroyed()
        {
            BindToggleMenu(null);
            SetMenuBlur(false);
            _menuHandle?.Dispose();
            _menuHandle = null;
            ReleasePortraits();
            if (_playerSettings != null) _playerSettings.Changed -= HandlePlayerSettingsChanged;
            _playerSettings = null;
            if (_shellView != null)
            {
                _shellView.CharacterCreatorRequested -= HandleCharacterCreatorRequested;
                _shellView.Detach();
                _shellView = null;
            }

            if (_creatorView != null)
            {
                _creatorView.ReturnToLobbyRequested -= HandleReturnToLobbyRequested;
                _creatorView.CharacterSaved -= HandleCharacterSaved;
                _creatorView.PreviewAngleChanged -= HandlePreviewAngleChanged;
                _creatorView.OutfitChanged -= HandleOutfitChanged;
            _creatorView.BodyMorphsChanged -= HandleBodyMorphsChanged;
            _creatorView.StyleChanged -= HandleStyleChanged;
            _creatorView.FaceChanged -= HandleFaceChanged;
            _creatorView.Detach();
                _creatorView = null;
            }

            if (ReferenceEquals(HumanoidStyleSource.Current, _catalog))
            {
                HumanoidStyleSource.Current = null;
            }

            DisposeBooth();
            base.OnDestroyed();
        }

        private void Update()
        {
            if (!_attached) TryAttachShell();
            if (_shellView != null) RefreshSession();
            RefreshDisplayConfirmation();
            if (_portraitDirty) TryCapturePortrait();
        }

        private void TryAttachShell()
        {
            if (!TryEnsureCatalog())
            {
                _attached = true;
                return;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem uiShell)
                || !uiShell.TryGetLayer(UiLayer.Modal, out VisualElement modalLayer))
            {
                return;
            }

            _attached = true;
            // The shell's panel settings are shared with the HUD, storage and radial overlays: UI Scale reaches all of them.
            if (SubSystems.TryGet(out PlayerSettingsSubSystem playerSettings)) playerSettings.RegisterUiPanel(uiShell.Document.panelSettings);
            InputInterface.RegisterDocument(uiShell.Document);

            _profileStore = new CharacterProfileStore(CharacterProfileStore.DefaultPath);
            CharacterProfileStore.Profile profile = _profileStore.Load();
            _draft = profile?.Characters.FirstOrDefault(c => c.Id == profile.SelectedId)
                ?? new CharacterCreatorDraft { OutfitIndex = _catalog.ChoosePreviewOutfitIndex() };

            _shellView = new LobbyShellView(_catalog);
            _shellView.Attach(modalLayer);
            _shellView.CharacterCreatorRequested += HandleCharacterCreatorRequested;
            _shellView.ReadyRequested += HandleReadyRequested;
            _shellView.QuitRequested += HandleQuitRequested;
            _shellView.BugReportRequested += HandleBugReportRequested;
            _shellView.RoundChangeRequested += HandleRoundChangeRequested;
            _shellView.ServerSettingsChanged += (field, settings) =>
            {
                if (CanAdmin() && SubSystems.TryGet(out RoundSubSystem rounds)) rounds.CmdUpdateServerSetting(field, settings);
            };
            _shellView.RoundAdminRequested += action =>
            {
                if (CanAdmin() && SubSystems.TryGet(out RoundSubSystem rounds)) rounds.CmdRoundAdminAction(action);
            };
            _shellView.MapStopResolved += (token, choice, name) =>
            {
                if (CanAdmin() && SubSystems.TryGet(out RoundSubSystem rounds)) rounds.CmdResolveMapStop(token, choice, name);
            };
            _shellView.JobPreferencesChanged += SendJobPreferences;
            _shellView.ChatSubmitted += HandleChatSubmitted;
            BindPlayerSettings();
            _shellView.SetCharacterName(_draft.Name);

            _creatorView = new CharacterCreatorView(_catalog, _draft, profile?.Characters);
            _creatorView.Attach(modalLayer);
            _creatorView.ReturnToLobbyRequested += HandleReturnToLobbyRequested;
            _creatorView.CharacterSaved += HandleCharacterSaved;
            _creatorView.PreviewAngleChanged += HandlePreviewAngleChanged;
            _creatorView.OutfitChanged += HandleOutfitChanged;
            _creatorView.BodyMorphsChanged += HandleBodyMorphsChanged;
            _creatorView.StyleChanged += HandleStyleChanged;
            _creatorView.FaceChanged += HandleFaceChanged;
            if (!EnsureBooth())
            {
                Debug.LogError("[Lobby] Could not initialize the live character preview.");
            }
            _creatorView.ApplyDraft(_draft);
            if (_booth != null)
            {
                _booth.SetAngleIndex(0);
            }
            PushPreviewTexture();

            _shellView.SetVisible(true);
            _creatorView.SetVisible(false);
            Debug.Log("[Lobby] UITK Lobby Shell + Character Creator attached (Phase E1 + hair styles).");
        }

        private PlayerSettingsSubSystem _playerSettings;

        /// <summary>Same strength as a diegetic machine panel (<c>MachineInterfaceHost.DiegeticBackdropBlur</c>).</summary>
        private const float MenuBackdropBlur = 0.7f;
        private bool _menuBlurred;

        /// <summary>
        /// Blurs the world behind the lobby, both between rounds and as the in-round (Escape) menu. Writes only on
        /// open/close so it never clears a blur a machine panel set while the menu stayed shut.
        /// </summary>
        private void SetMenuBlur(bool open)
        {
            if (open == _menuBlurred || !SubSystems.TryGet(out ScreenEffectsSubSystem screenEffects)) return;
            _menuBlurred = open;
            screenEffects.SetUiBackdropBlur(open ? MenuBackdropBlur : 0f);
        }

        /// <summary>The Settings tab edits client-local player settings; no server round trip.</summary>
        private void BindPlayerSettings()
        {
            if (!SubSystems.TryGet(out _playerSettings)) return;
            PlayerSettingsSubSystem settings = _playerSettings;
            _shellView.VolumeChanged += settings.SetVolume;
            _shellView.MuteAllChanged += settings.SetMuteAll;
            _shellView.MuteWhenUnfocusedChanged += settings.SetMuteWhenUnfocused;
            _shellView.AudioResetRequested += settings.ResetAudio;
            _shellView.ResolutionChanged += settings.SetResolution;
            _shellView.WindowModeChanged += settings.SetWindowMode;
            _shellView.VSyncChanged += settings.SetVSync;
            _shellView.FrameCapChanged += settings.SetFrameCap;
            _shellView.QualityChanged += settings.SetQuality;
            _shellView.RenderScaleChanged += settings.SetRenderScale;
            _shellView.UpscalerChanged += settings.SetUpscaler;
            _shellView.AntiAliasingChanged += settings.SetAntiAliasing;
            _shellView.UiScaleChanged += settings.SetUiScale;
            _shellView.ShowFpsChanged += settings.SetShowFps;
            _shellView.DisplayResetRequested += settings.ResetDisplay;
            _shellView.DisplayChangeKept += settings.ConfirmDisplayChange;
            _shellView.DisplayChangeReverted += settings.RevertDisplayChange;
            _shellView.KeyCaptureRequested += settings.BeginKeyCapture;
            _shellView.KeyCaptureCancelled += settings.CancelKeyCapture;
            _shellView.ControlsResetRequested += settings.ResetControls;
            settings.Changed += HandlePlayerSettingsChanged;
            HandlePlayerSettingsChanged();
        }

        private void HandlePlayerSettingsChanged()
        {
            if (_shellView == null) return;
            _shellView.SetAudioSettings(_playerSettings.Settings.Audio);
            _shellView.SetDisplaySettings(_playerSettings.Settings.Display, _playerSettings.ResolutionChoices,
                _playerSettings.CurrentResolution, _playerSettings.StpSupported);
            _shellView.SetControls(_playerSettings.KeybindRows,
                _playerSettings.CapturingRowId, _playerSettings.ControlsNotice, _playerSettings.ControlsNoticeVersion);
            RefreshDisplayConfirmation();
        }

        /// <summary>Countdown text for the "Keep these settings?" prompt; cheap enough to run every frame.</summary>
        private void RefreshDisplayConfirmation()
        {
            if (_shellView == null || _playerSettings == null) return;
            _shellView.SetDisplayConfirmation(_playerSettings.DisplayConfirmationPending,
                Mathf.CeilToInt(_playerSettings.DisplayConfirmationSecondsLeft));
        }

        private float _nextSessionRefresh;
        private bool _inWorld;
        private NetworkConnection _preferencesSentTo;
        private NetworkConnection _appearanceSentTo;
        private NetworkConnection _chatHistoryRequestedFrom;
        private bool _spawned;
        private bool _menuOpen;
        private IInputHandle _menuHandle;
        private InputSubSystem _inputSystem;
        private InputAction _toggleMenuAction;

        private RoundState _lastRoundSnapshot;
        private StationLoadState _lastStationSnapshot;
        private string _lastStationName;

        private void RefreshSession()
        {
            SubSystems.TryGet(out RoundSubSystem rounds);
            bool roundChanged = rounds != null && (rounds.CurrentRoundState != _lastRoundSnapshot
                || rounds.CurrentStationLoadState != _lastStationSnapshot || rounds.StationName != _lastStationName);
            if (!roundChanged && Time.unscaledTime < _nextSessionRefresh) return;
            if (rounds != null)
            {
                _lastRoundSnapshot = rounds.CurrentRoundState;
                _lastStationSnapshot = rounds.CurrentStationLoadState;
                _lastStationName = rounds.StationName;
            }
            _nextSessionRefresh = Time.unscaledTime + 0.2f;
            bool online = InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started;
            bool connected = online && SubSystems.TryGet(out PlayerSubSystem _) && SubSystems.TryGet(out RoundSubSystem _);
            SubSystems.TryGet(out PlayerSubSystem players);
            SubSystems.TryGet(out ReadyPlayersSubSystem ready);
            SubSystems.TryGet(out EntitySubSystem entities);
            Player local = connected ? players.GetPlayer(InstanceFinder.ClientManager.Connection) : null;
            bool spawned = local != null && entities != null && entities.IsPlayerSpawned(InstanceFinder.ClientManager.Connection);
            _spawned = spawned;
            if (!spawned) _menuOpen = false;
            BindToggleMenu(SubSystems.TryGet(out InputSubSystem input) ? input : null);
            bool visible = connected && (!spawned || _menuOpen);
            bool holdMenu = visible && spawned;
            // Blur whatever world sits behind the lobby, in a round (Escape menu) or not.
            SetMenuBlur(visible);
            if (holdMenu && _menuHandle == null && _inputSystem != null) _menuHandle = _inputSystem.PushContext(InputContext.Menu);
            else if (!holdMenu && _menuHandle != null)
            {
                _menuHandle.Dispose();
                _menuHandle = null;
            }
            if (!visible)
            {
                _shellView.SetVisible(false);
                _creatorView.SetVisible(false);
                if (_booth != null) _booth.SetActive(false);
                _inWorld = true;
            }
            else if (_inWorld)
            {
                _inWorld = false;
                HandleReturnToLobbyRequested();
            }
            // Wait for the replicated player record: the server drops preferences from unknown senders.
            if (local == null)
            {
                _preferencesSentTo = null;
                _appearanceSentTo = null;
                _chatHistoryRequestedFrom = null;
            }
            else
            {
                if (_preferencesSentTo != InstanceFinder.ClientManager.Connection) SendJobPreferences();
                if (_appearanceSentTo != InstanceFinder.ClientManager.Connection) SendCharacterAppearance();
                if (_chatHistoryRequestedFrom != InstanceFinder.ClientManager.Connection
                    && SubSystems.TryGet(out CommsSubSystem historyComms))
                {
                    historyComms.RequestOocHistory();
                    _chatHistoryRequestedFrom = InstanceFinder.ClientManager.Connection;
                }
            }
            _shellView.SetJobs(connected && SubSystems.TryGet(out RoleSubSystem roles)
                ? roles.Jobs : System.Array.Empty<JobSlotInfo>());
            if (connected && rounds != null)
                _shellView.SetServerInfo(rounds.ServerName, rounds.Motd, rounds.PublicMode, rounds.ServerBuild, rounds.ServerStarted);
            else
                _shellView.SetServerInfo(null, "", null, "", null);
            _shellView.SetStationLoadState(rounds != null ? rounds.CurrentStationLoadState : StationLoadState.NotLoaded,
                rounds != null ? rounds.StationName : "");
            bool admin = CanAdmin();
            if (SubSystems.TryGet(out CommsSubSystem comms))
                _shellView.SetChat(comms.OocLog, comms.OocLogVersion, connected && admin);
            if (admin && rounds != null && connected)
            {
                if (Time.unscaledTime >= _nextAdminRefresh)
                {
                    _nextAdminRefresh = Time.unscaledTime + 1f;
                    rounds.CmdReadServerSettings();
                }
                if (rounds.HasAdminSettings)
                    _shellView.SetServerSettings(rounds.AdminSettings, rounds.AdminMaps, rounds.AdminSettingsStatus);
            }
            bool mapPrompt = connected && admin && rounds != null && rounds.CurrentRoundState == RoundState.Ongoing;
            _shellView.SetMapStopPrompt(mapPrompt ? rounds.MapStopToken : 0,
                mapPrompt ? rounds.MapStopEditingMap : "", mapPrompt ? rounds.MapStopError : "");
            LobbyMockData.PlayerRow[] rows = connected ? players.OnlinePlayers.Where(p => p != null)
                .Select(p => new LobbyMockData.PlayerRow(p.Ckey, p.Ckey, ready != null && ready.IsReady(p),
                    null, "—", "—", "—")).ToArray() : System.Array.Empty<LobbyMockData.PlayerRow>();
            _shellView.SetAutoStart(rounds != null && rounds.AutoStartEnabled, rounds != null && rounds.AutoStartHeld,
                rounds != null ? rounds.AutoStartSecondsLeft : 0);
            _shellView.SetSession(rounds != null ? rounds.CurrentRoundState : RoundState.Stopped,
                rounds != null ? rounds.CurrentTimerSeconds : 0,
                local != null && ready != null && ready.IsReady(local), local != null && (rounds == null || rounds.CurrentRoundState != RoundState.Ongoing || rounds.AllowLateJoin), admin, rows, spawned);
        }

        /// <summary>Follows InputSubSystem across scene reloads (it owns a new Controls each time).</summary>
        private void BindToggleMenu(InputSubSystem input)
        {
            if (input == _inputSystem) return;
            if (_toggleMenuAction != null) _toggleMenuAction.performed -= HandleToggleMenu;
            _menuHandle?.Dispose();
            _menuHandle = null;
            _inputSystem = input;
            _toggleMenuAction = input != null ? input.Inputs?.Other.ToggleMenu : null;
            if (_toggleMenuAction != null) _toggleMenuAction.performed += HandleToggleMenu;
        }

        /// <summary>
        /// Closes the Escape menu first so the report's screenshot shows the round, not the menu; between
        /// rounds the lobby is what the player sees, so it stays.
        /// </summary>
        private void HandleBugReportRequested()
        {
            if (_spawned && _menuOpen)
            {
                _menuOpen = false;
                _nextSessionRefresh = 0f;
                RefreshSession();
            }

            SS3D.Systems.BugReports.BugReportRequests.RequestOpen();
        }

        private void HandleToggleMenu(InputAction.CallbackContext context)
        {
            // Before spawn the shell is the whole screen; Escape there belongs to other owners.
            if (!_spawned || _shellView == null) return;
            if (_shellView.IsCapturingKey) return;
            if (SubSystems.TryGet(out ArmedInteractionSubSystem armed) && armed.IsArmed) return;
            if (SubSystems.TryGet(out MapEditorSubSystem mapEditor) && mapEditor.IsActive) return;
            _menuOpen = !_menuOpen;
            _nextSessionRefresh = 0f;
            RefreshSession();
        }

        /// <summary>
        /// Sends the full preference list; the server keeps it per ckey until the next change, so it
        /// is also resent once per connection.
        /// </summary>
        private void SendJobPreferences()
        {
            if (_shellView == null || InstanceFinder.ClientManager == null || !InstanceFinder.ClientManager.Started) return;
            string[] jobs = _shellView.JobPriorities.Keys.ToArray();
            InstanceFinder.ClientManager.Broadcast(new JobPreferencesMessage
            {
                Jobs = jobs,
                Priorities = jobs.Select(job => (byte)_shellView.JobPriorities[job]).ToArray(),
                AntagonistOptIns = _shellView.AntagonistOptIns.ToArray(),
            });
            _preferencesSentTo = InstanceFinder.ClientManager.Connection;
        }

        /// <summary>
        /// Sends the saved character (never unsaved creator edits); the server applies it at the next
        /// spawn, so it is also resent once per connection.
        /// </summary>
        private void SendCharacterAppearance()
        {
            if (_draft == null || _catalog == null || InstanceFinder.ClientManager == null || !InstanceFinder.ClientManager.Started) return;
            InstanceFinder.ClientManager.Broadcast(new CharacterAppearanceMessage
            {
                Appearance = new CharacterAppearance
                {
                    Name = _draft.Name,
                    Female = _draft.GetMorph("female", 0f),
                    Breasts = _draft.GetMorph("breasts", 0f),
                    Fat = _draft.GetMorph("fat", 0f),
                    Muscle = _draft.GetMorph("muscle", 0f),
                    Jaw = _draft.GetMorph("jaw", 0.5f),
                    Height = _draft.GetMorph("height", 0.5f),
                    HairStyle = _draft.HairStyleIndex,
                    BeardStyle = _draft.BeardStyleIndex,
                    HairColor = _catalog.GetHairColor(_draft.HairColorIndex),
                    BeardColor = _catalog.GetHairColor(_draft.BeardColorIndex),
                    SkinColor = CharacterCreatorView.PaletteColor(CharacterCreatorMockData.SkinColors, _draft.SkinColorIndex),
                    EyeColor = CharacterCreatorView.PaletteColor(CharacterCreatorMockData.EyeColors, _draft.EyeColorIndex),
                    EyebrowColor = _catalog.GetHairColor(_draft.EyebrowColorIndex),
                    EyebrowStyle = CharacterAppearance.NormalizeEyebrowStyle(_draft.Eyebrows),
                },
            });
            _appearanceSentTo = InstanceFinder.ClientManager.Connection;
        }

        private float _nextAdminRefresh;

        private bool CanAdmin() => SubSystems.TryGet(out PermissionSubSystem permissions)
            && permissions.HasLoadedPermissions
            && !string.IsNullOrEmpty(SS3D.Core.Settings.LocalPlayer.Ckey)
            && permissions.IsAtLeast(SS3D.Core.Settings.LocalPlayer.Ckey, ServerRoleTypes.Administrator);

        private void HandleChatSubmitted(OocChannel channel, string text)
        {
            if (SubSystems.TryGet(out CommsSubSystem comms)) comms.SendOoc(channel, text);
        }

        private void HandleRoundChangeRequested(bool start)
        {
            if (CanAdmin() && InstanceFinder.ClientManager.Started)
                InstanceFinder.ClientManager.Broadcast(new ChangeRoundStateMessage(start));
        }

        /// <summary>Leaves the session (ending it when hosting) and returns to the main menu.</summary>
        private static void HandleQuitRequested()
        {
            if (SubSystems.TryGet(out SS3D.Networking.NetworkSessionSubSystem session))
            {
                session.LeaveToMainMenu();
            }
        }

        private void HandleReadyRequested()
        {
            if (!InstanceFinder.ClientManager.Started || !SubSystems.TryGet(out PlayerSubSystem players)
                || !SubSystems.TryGet(out RoundSubSystem rounds) || !SubSystems.TryGet(out ReadyPlayersSubSystem ready)) return;
            Player local = players.GetPlayer(InstanceFinder.ClientManager.Connection);
            if (local == null) return;
            if (rounds.CurrentRoundState == RoundState.Ongoing)
            {
                if (SubSystems.TryGet(out EntitySubSystem entities)) entities.CmdSpawnLatePlayer(local);
            }
            else if (rounds.CurrentRoundState is not RoundState.Ending)
                InstanceFinder.ClientManager.Broadcast(new ChangePlayerReadyMessage(local.Ckey, !ready.IsReady(local)));
        }

        private bool TryEnsureCatalog()
        {
            if (_catalog != null)
            {
                return true;
            }

            _catalog = Resources.Load<LobbyAssetCatalog>(LobbyAssetPaths.ResourcesCatalogName);
            if (_catalog == null)
            {
                Debug.LogError(
                    $"LobbyUiSubSystem could not load Resources/{LobbyAssetPaths.ResourcesCatalogName}. "
                    + "Run SS3D → Data → Rebuild All UI Catalogs and commit the asset.",
                    this);
                return false;
            }

            if (!_catalog.HasRequiredAssets(out string missingField))
            {
                Debug.LogError(
                    $"LobbyAssetCatalog is missing required field '{missingField}'. "
                    + "Run SS3D → Data → Rebuild All UI Catalogs.",
                    this);
                return false;
            }

            HumanoidStyleSource.Current = _catalog;
            return true;
        }

        private void HandleCharacterCreatorRequested()
        {
            if (_shellView == null || _creatorView == null)
            {
                return;
            }

            if (!EnsureBooth())
            {
                Debug.LogWarning("[Lobby] Character Creator opened without live preview (human prefab missing).");
            }
            else
            {
                ApplyBodyMorphsToBooth(_creatorView.CurrentBodyMorphs);
                _creatorView.NotifyStyleChanged();
            }

            PushPreviewTexture();
            _shellView.SetVisible(false);
            _creatorView.Open();
            Debug.Log("[Lobby] Character Creator opened.");
        }

        private void HandleReturnToLobbyRequested()
        {
            if (_shellView == null || _creatorView == null)
            {
                return;
            }

            if (_booth != null)
            {
                // Keep booth warm for lobby sidebar RT; camera stays on.
                _booth.SetActive(true);
            }

            // Return shows the last explicitly saved character, not unsaved preview edits.
            _creatorView.ApplyDraft(_draft);
            PushPreviewTexture();
            _shellView.SetCharacterName(_draft?.Name ?? LobbyMockData.CharacterName);
            _creatorView.SetVisible(false);
            _shellView.SetVisible(true);
            Debug.Log("[Lobby] Returned to Lobby Shell from Character Creator.");
        }

        private void HandleCharacterSaved(CharacterCreatorDraft draft)
        {
            if (draft == null)
            {
                return;
            }

            _draft = draft;
            _profileStore.Save(_creatorView.SavedCharacters, draft.Id);
            _shellView?.SetCharacterName(_draft.Name);
            HandleOutfitChanged(_draft.OutfitIndex);
            ApplyBodyMorphsToBooth(_draft.BodyMorphs);
            ApplyStyleToBooth(_draft.HairStyleIndex, _draft.BeardStyleIndex, _draft.HairColorIndex, _draft.BeardColorIndex);
            HandleFaceChanged(CharacterCreatorView.PaletteColor(CharacterCreatorMockData.SkinColors, _draft.SkinColorIndex),
                CharacterCreatorView.PaletteColor(CharacterCreatorMockData.EyeColors, _draft.EyeColorIndex),
                CharacterAppearance.NormalizeEyebrowStyle(_draft.Eyebrows), _catalog.GetHairColor(_draft.EyebrowColorIndex));
            SendCharacterAppearance();
            Debug.Log($"[Lobby] Character draft saved: {_draft.Name}");
        }

        /// <summary>Recapture the current character's selector portrait once the booth renders the change.</summary>
        private void MarkPortraitDirty()
        {
            _portraitDirty = true;
            _portraitDirtyFrame = Time.frameCount;
        }

        private void TryCapturePortrait()
        {
            // The booth camera renders at end of frame: wait one full frame after the change.
            if (_booth == null || _creatorView == null || Time.frameCount <= _portraitDirtyFrame + 1) return;
            string id = _creatorView.CurrentCharacterId;
            if (string.IsNullOrEmpty(id)) return;
            if (!_portraits.TryGetValue(id, out RenderTexture portrait) || portrait == null)
            {
                portrait = new RenderTexture(PortraitSize, PortraitSize, 0, RenderTextureFormat.ARGB32)
                {
                    name = "CharacterPortraitRT",
                    filterMode = FilterMode.Bilinear,
                };
                portrait.Create();
                _portraits[id] = portrait;
            }

            // Side/Back angles keep the flag set until the preview returns to Front.
            if (!_booth.TryCapturePortrait(portrait)) return;
            _portraitDirty = false;
            _creatorView.SetPortrait(id, portrait);
        }

        private void ReleasePortraits()
        {
            foreach (RenderTexture portrait in _portraits.Values)
            {
                if (portrait == null) continue;
                portrait.Release();
                Destroy(portrait);
            }

            _portraits.Clear();
        }

        private void HandleOutfitChanged(int outfitIndex)
        {
            MarkPortraitDirty();
            if (_booth == null || _catalog == null)
            {
                return;
            }
            LobbyPreviewOutfit outfit = _catalog.GetPreviewOutfit(outfitIndex);
            _booth.ApplyOutfit(outfit.Jumpsuit.GetComponent<Cloth>(), outfit.Shoes.GetComponent<Cloth>());
        }

        private void HandlePreviewAngleChanged(int angleIndex)
        {
            MarkPortraitDirty();
            if (_booth != null)
            {
                _booth.SetAngleIndex(angleIndex);
            }
        }

        private void HandleBodyMorphsChanged(IReadOnlyDictionary<string, float> morphs)
        {
            MarkPortraitDirty();
            ApplyBodyMorphsToBooth(morphs);
        }

        private void HandleFaceChanged(Color skin, Color eyes, string eyebrows, Color browColor)
        {
            MarkPortraitDirty();
            if (_booth != null) _booth.ApplyFace(skin, eyes, _catalog.GetEyebrowStyle(eyebrows), browColor);
        }

        private void HandleStyleChanged(int hairIndex, int beardIndex, int colorIndex, int beardColorIndex)
        {
            MarkPortraitDirty();
            ApplyStyleToBooth(hairIndex, beardIndex, colorIndex, beardColorIndex);
        }

        private void ApplyStyleToBooth(int hairIndex, int beardIndex, int colorIndex, int beardColorIndex)
        {
            if (_booth == null || _catalog == null)
            {
                return;
            }

            GameObject hairPrefab = _catalog.GetHairStyle(hairIndex);
            GameObject beardPrefab = _catalog.GetBeardStyle(beardIndex);
            Color hairColor = _catalog.GetHairColor(colorIndex);
            _booth.ApplyStyle(hairPrefab, beardPrefab, hairColor, _catalog.GetHairColor(beardColorIndex));
        }

        private void ApplyBodyMorphsToBooth(IReadOnlyDictionary<string, float> morphs)
        {
            if (_booth == null || morphs == null)
            {
                return;
            }

            float Get(string key, float fallback) =>
                morphs.TryGetValue(key, out float value) ? value : fallback;

            _booth.ApplyBodyMorphs(
                female: Get("female", 0f),
                breasts: Get("breasts", 0f),
                fat: Get("fat", 0f),
                muscle: Get("muscle", 0f),
                jaw: Get("jaw", 0.5f),
                height: Get("height", 0.5f));
        }

        private bool EnsureBooth()
        {
            if (_booth != null)
            {
                _booth.SetActive(true);
                return _booth.PreviewTexture != null;
            }

            GameObject prefab = _catalog != null ? _catalog.PreviewHumanPrefab : null;
            if (prefab == null)
            {
                return false;
            }

            GameObject host = new("CharacterPreviewBooth");
            DontDestroyOnLoad(host);
            _booth = host.AddComponent<CharacterPreviewBooth>();
            _booth.Initialize(prefab);
            return _booth.PreviewTexture != null;
        }

        private void PushPreviewTexture()
        {
            Texture texture = _booth != null ? _booth.PreviewTexture : null;
            _creatorView?.SetPreviewTexture(texture);
            _shellView?.SetPreviewTexture(texture);
        }

        private void DisposeBooth()
        {
            if (_booth == null)
            {
                return;
            }

            _booth.DisposeBooth();
            _booth = null;
        }
    }
}
