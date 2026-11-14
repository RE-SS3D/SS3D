using SS3D.Core;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using SS3D.Permissions;
using SS3D.Systems.Audio;
using SS3D.Systems.Comms;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Session;
using SS3D.Systems.Roles;
using SS3D.Systems.Roles.Messages;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using SS3D.Systems.ScreenEffects;
using SS3D.Systems.Settings;
using SS3D.UI.SettingsPanel;
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
using SS3D.UI.Shell.RoundEnd;
using SS3D.UI.Shell.Catalog;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// Self-bootstrapping host for the UITK lobby shell and the character window (lobby effort Phases 4–5).
    /// Attaches into <see cref="UiLayer.Modal"/>. Owns the saved characters, the live preview booth, session
    /// visibility, ready/late-join and administrator round requests. While the local player has a body, Escape
    /// (<c>Other/Toggle Menu</c>) opens the shell as an in-round menu.
    /// </summary>
    public sealed class LobbyUiSubSystem : SubSystem
    {
        private LobbyAssetCatalog _catalog;
        private LobbyView _shellView;
        private CharacterWindowView _window;
        private CharacterSwitcher _lobbySwitcher;
        private string _jobsSignature;
        private LobbyCharacterPreview _preview;
        private CharacterCreatorDraft _draft;
        private CharacterProfileStore _profileStore;
        private CharacterRoster _roster;
        private readonly System.Random _random = new();
        private bool _attached;

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
            _settingsPanel?.Dispose();
            _settingsPanel = null;
            if (_shellView != null)
            {
                _shellView.CharacterCreatorRequested -= HandleCharacterCreatorRequested;
                _shellView.Detach();
                _shellView = null;
            }

            _window?.Detach();
            _window = null;

            if (ReferenceEquals(HumanoidStyleSource.Current, _catalog))
            {
                HumanoidStyleSource.Current = null;
            }

            UnbindClientSession();
            _preview?.Dispose();
            _preview = null;
            base.OnDestroyed();
        }

        private void Update()
        {
            if (!_attached) TryAttachShell();
            if (_shellView != null)
            {
                BindClientSession();
                RefreshSession();
            }
            _preview?.Tick();
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

            _profileStore = new CharacterProfileStore(CharacterProfileStore.DefaultPath, CreateRandomCharacter);
            CharacterProfileStore.Profile profile = _profileStore.Load();
            // No saved characters yet: the player starts with a random one, saved at once (Decision 3).
            _roster = new CharacterRoster(profile?.Characters, profile?.SelectedId, CreateRandomCharacter);
            if (profile == null) SaveRoster();
            _draft = _roster.Selected;

            _shellView = new LobbyView(_catalog);
            _shellView.Attach(modalLayer);
            _shellView.CharacterCreatorRequested += HandleCharacterCreatorRequested;
            _preview = new LobbyCharacterPreview(_catalog);
            _preview.PortraitCaptured += RepaintPortraits;
            _lobbySwitcher = new CharacterSwitcher(_roster, _preview.PortraitOf, DepartmentOf);
            BindSwitcher(_lobbySwitcher);
            _shellView.MountSwitcher(_lobbySwitcher);
            _shellView.BackToRoundRequested += HandleBackToRound;
            _shellView.MusicToggleRequested += HandleMusicToggle;
            _shellView.MusicNextRequested += HandleMusicNext;
            _shellView.MusicVolumeChanged += HandleMusicVolume;
            _shellView.ReadyRequested += HandleReadyRequested;
            _shellView.ObserveRequested += HandleObserveRequested;
            _shellView.QuitRequested += HandleQuitRequested;
            _shellView.BugReportRequested += HandleBugReportRequested;
            _shellView.ServerSettingsChanged += (field, settings) =>
            {
                if (CanAdmin() && SubSystems.TryGet(out ServerSettingsService service)) service.CmdUpdateServerSetting(field, settings);
            };
            _shellView.RoundAdminRequested += action =>
            {
                if (CanAdmin() && SubSystems.TryGet(out RoundSubSystem rounds)) rounds.CmdRoundAdminAction(action);
            };
            _shellView.MapStopResolved += (token, choice, name) =>
            {
                if (CanAdmin() && SubSystems.TryGet(out MapEditorStopFlow stopFlow)) stopFlow.CmdResolveMapStop(token, choice, name);
            };
            _shellView.ChatSubmitted += HandleChatSubmitted;
            BindPlayerSettings();
            RefreshCharacterCard();

            _window = new CharacterWindowView(_catalog, _roster, _preview.PortraitOf, DepartmentOf);
            _window.Attach(modalLayer);
            _window.ReturnRequested += HandleReturnToLobbyRequested;
            _window.Saved += HandleCharacterSaved;
            _window.SelectRequested += HandleSelectCharacter;
            _window.NewRequested += HandleNewCharacter;
            _window.DuplicateRequested += HandleDuplicateCharacter;
            _window.DeleteRequested += HandleDeleteCharacter;
            _window.AngleChanged += _preview.SetAngle;
            _window.BodyChanged += _preview.ApplyBody;
            _window.LookChanged += _preview.ApplyLook;
            _window.OutfitChanged += _preview.ApplyOutfit;
            if (!_preview.Ensure())
            {
                Debug.LogError("[Lobby] Could not initialize the live character preview.");
            }
            _preview.ShowCharacter(_draft);
            PushPreviewTexture();

            _shellView.SetVisible(true);
            _window.SetVisible(false);
            Debug.Log("[Lobby] UITK lobby shell and character window attached.");
        }

        private PlayerSettingsPanel _settingsPanel;

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

        /// <summary>
        /// The Settings tab shows the shared Settings screen (also the main menu's), which edits client-local player
        /// settings with no server round trip. The lobby tab bar titles it, so it has no header of its own.
        /// </summary>
        private void BindPlayerSettings()
        {
            if (!SubSystems.TryGet(out PlayerSettingsSubSystem settings)) return;
            _settingsPanel = new PlayerSettingsPanel(_shellView.SettingsHost, _shellView.Root, settings, null, false);
            _shellView.Hidden += _settingsPanel.CancelTransient;
        }

        private float _nextSessionRefresh;
        private bool _inWorld;
        private bool _spawned;
        private bool _menuOpen;
        private IInputHandle _menuHandle;
        private InputSubSystem _inputSystem;
        private InputAction _toggleMenuAction;

        private RoundState _lastRoundSnapshot;
        private RoundEndAssetCatalog _roundEndCatalog;
        private bool _roundEndCatalogFailed;
        private RoundOutcome _lastRoundOutcome;
        private int _lastRoundSelectedId = -1;
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
            bool online = NetworkManager.Instances.Count > 0 && InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started;
            bool connected = online && SubSystems.TryGet(out PlayerSubSystem _) && SubSystems.TryGet(out RoundSubSystem _);
            SubSystems.TryGet(out PlayerSubSystem players);
            SubSystems.TryGet(out ReadyPlayersSubSystem ready);
            SubSystems.TryGet(out EntitySubSystem entities);
            Player local = connected ? players.GetPlayer(InstanceFinder.ClientManager.Connection) : null;
            bool spawned = local != null && entities != null && entities.IsPlayerSpawned(InstanceFinder.ClientManager.Connection);
            _spawned = spawned;
            if (!spawned) _menuOpen = false;
            // Menu music plays on from the main menu into the lobby and fades once the player has a body or a ghost,
            // the in-round Escape menu included (lobby effort Decision 15). No opinion until connected, so a handover
            // from the main menu keeps playing through the session load.
            if (SubSystems.TryGet(out MenuMusicSubSystem music)) music.SetLobby(connected ? !spawned : null);
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
                _window.SetVisible(false);
                _preview?.SetActive(false);
                _inWorld = true;
            }
            else if (_inWorld)
            {
                _inWorld = false;
                HandleReturnToLobbyRequested();
            }

            SubSystems.TryGet(out ServerSettingsService serverSettings);
            if (connected && serverSettings != null)
                _shellView.SetServerInfo(serverSettings.ServerName, serverSettings.Motd, serverSettings.PublicMode,
                    serverSettings.ServerBuild, serverSettings.ServerStarted);
            else
                _shellView.SetServerInfo(null, "", null, "", null);
            RefreshLastRound(connected ? rounds : null, visible);
            _shellView.SetStationLoadState(rounds != null ? rounds.CurrentStationLoadState : StationLoadState.NotLoaded,
                rounds != null ? rounds.StationName : "");
            if (rounds != null) _shellView.SetRoundId(rounds.RoundId);
            bool admin = CanAdmin();
            ShowAdminSettings();
            if (SubSystems.TryGet(out CommsSubSystem comms))
                _shellView.SetChat(comms.OocLog, comms.OocLogVersion, connected && admin, connected);
            MapEditorStopFlow stopFlow = null;
            bool mapPrompt = connected && admin && rounds != null && rounds.CurrentRoundState == RoundState.Ongoing
                && SubSystems.TryGet(out stopFlow);
            _shellView.SetMapStopPrompt(mapPrompt ? stopFlow.MapStopToken : 0,
                mapPrompt ? stopFlow.MapStopEditingMap : "", mapPrompt ? stopFlow.MapStopError : "");

            int onlineCount = 0;
            int readyCount = 0;
            if (connected)
                foreach (Player player in players.OnlinePlayers)
                {
                    if (player == null) continue;
                    onlineCount++;
                    if (ready != null && ready.IsReady(player)) readyCount++;
                }
            _shellView.SetPlayers(onlineCount, readyCount, connected && serverSettings != null ? serverSettings.PublicPlayerCap : 0);
            RefreshCharacterCard();
            RefreshWindowData(serverSettings);
            RefreshMusicRow(spawned);

            RoundState state = rounds != null ? rounds.CurrentRoundState : RoundState.Stopped;
            _shellView.SetSession(new LobbySessionInfo
            {
                State = state,
                TimerSeconds = rounds != null ? rounds.CurrentTimerSeconds : 0,
                AutoStartEnabled = rounds != null && rounds.AutoStartEnabled,
                AutoStartHeld = rounds != null && rounds.AutoStartHeld,
                AutoStartSecondsLeft = rounds != null ? rounds.AutoStartSecondsLeft : 0,
                Connected = onlineCount,
                MinimumConnected = serverSettings != null ? serverSettings.PublicMinimumConnected : 1,
                EndHeld = rounds != null && rounds.EndHeld,
                EndSecondsLeft = rounds != null ? rounds.EndSecondsLeft : 0,
                Ready = local != null && ready != null && ready.IsReady(local),
                Spawned = spawned,
                LateJoinAllowed = serverSettings == null || serverSettings.AllowLateJoin,
                // Observing is immediate (lobby effort Decision 9): no late-join gate, only a loaded station and no body yet.
                CanObserve = local != null && !spawned && rounds != null
                    && ObserveRules.StationLoaded(rounds.CurrentRoundState, rounds.CurrentStationLoadState),
                FirstName = LobbySidebarModel.FirstName(_draft?.Name),
            }, admin);
        }

        /// <summary>The sidebar card: the saved selected character, its High job and that job's department.</summary>
        private void RefreshCharacterCard()
        {
            if (_shellView == null || _draft == null) return;
            string high = _draft.Preferences?.HighJob;
            Department department = Department.None;
            if (high != null && SubSystems.TryGet(out RoleSubSystem roles))
                foreach (JobSlotInfo job in roles.Jobs)
                    if (job.Name == high) department = (Department)job.Department;
            _shellView.SetCharacter(_draft.Name, high, department, _roster?.Count ?? 1, CharacterRoster.MaxCharacters);
        }

        /// <summary>
        /// Feeds the character window the replicated job list (rebuilt only when it changes) and the server's
        /// antagonist default.
        /// </summary>
        private void RefreshWindowData(ServerSettingsService serverSettings)
        {
            if (_window == null) return;
            _window.SetAntagonistDefault(serverSettings != null && serverSettings.AntagonistOptInDefault);
            if (!SubSystems.TryGet(out RoleSubSystem roles)) return;
            System.Text.StringBuilder signature = new();
            foreach (JobSlotInfo job in roles.Jobs) signature.Append(job.Name).Append('/').Append(job.Department).Append('/').Append(job.Slots).Append(';');
            if (signature.ToString() == _jobsSignature) return;
            _jobsSignature = signature.ToString();
            _window.SetJobs(roles.Jobs.ToList(), roles.DescriptionOf, roles.FallbackJob);
            _lobbySwitcher?.Refresh();
            // The booth's outfit follows the High job's loadout, which needs the role assets.
            if (!_window.IsVisible) _preview.ShowCharacter(_draft);
        }

        /// <summary>The music row drives the same menu music the main menu does (Decision 15).</summary>
        private void RefreshMusicRow(bool spawned)
        {
            SubSystems.TryGet(out PlayerSettingsSubSystem settings);
            SubSystems.TryGet(out MenuMusicSubSystem music);
            bool on = settings == null || settings.Settings.Audio.MenuMusic;
            _shellView.SetMusic(!spawned && music != null && music.HasTracks, on, music != null ? music.Title : "",
                settings != null ? settings.Settings.Audio.Music : 0.8f);
        }

        private void HandleMusicToggle()
        {
            if (!SubSystems.TryGet(out PlayerSettingsSubSystem settings)) return;
            settings.SetMenuMusic(!settings.Settings.Audio.MenuMusic);
            _nextSessionRefresh = 0f;
        }

        private void HandleMusicNext()
        {
            if (SubSystems.TryGet(out MenuMusicSubSystem music)) music.Next();
            _nextSessionRefresh = 0f;
        }

        private void HandleMusicVolume(float volume)
        {
            if (!SubSystems.TryGet(out PlayerSettingsSubSystem settings)) return;
            settings.SetVolume(PlayerVolume.Music, volume);
            // Turning the volume up turns the music on, as on the main menu.
            if (volume > 0f && !settings.Settings.Audio.MenuMusic) settings.SetMenuMusic(true);
        }

        /// <summary>"Back to round" closes the in-round Escape menu, the same as Escape.</summary>
        private void HandleBackToRound()
        {
            if (!_spawned) return;
            _menuOpen = false;
            _nextSessionRefresh = 0f;
            RefreshSession();
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
            // The character window takes Escape itself (Return to lobby, or its unsaved-changes prompt).
            if (_window != null && _window.IsVisible) return;
            // A key capture or a pending display change takes Escape first, before or during a round.
            if (_shellView != null && _settingsPanel != null && _settingsPanel.ConsumeEscape()) return;
            // Before spawn the shell is the whole screen; Escape there belongs to other owners.
            if (!_spawned || _shellView == null) return;
            if (SubSystems.TryGet(out ArmedInteractionSubSystem armed) && armed.IsArmed) return;
            if (SubSystems.TryGet(out MapEditorSubSystem mapEditor) && mapEditor.IsActive) return;
            _menuOpen = !_menuOpen;
            _nextSessionRefresh = 0f;
            RefreshSession();
        }

        /// <summary>
        /// The session service sends job preferences, the saved character and the OOC history request once per
        /// connection, and relays the admin settings the server pushes (tech-debt P6.6). The lobby only supplies
        /// the values and shows what arrives.
        /// </summary>
        private bool _sessionBound;

        private void BindClientSession()
        {
            if (_sessionBound || !SubSystems.TryGet(out ClientSessionSubSystem session)) return;
            _sessionBound = true;
            session.JobPreferencesSource = BuildJobPreferences;
            session.AppearanceSource = BuildCharacterAppearance;
            session.AdminSettingsChanged += ShowAdminSettings;
            session.JobPreferencesChanged();
            session.AppearanceChanged();
            ShowAdminSettings();
        }

        private void UnbindClientSession()
        {
            if (!_sessionBound || !SubSystems.TryGet(out ClientSessionSubSystem session)) return;
            _sessionBound = false;
            session.AdminSettingsChanged -= ShowAdminSettings;
            session.JobPreferencesSource = null;
            session.AppearanceSource = null;
        }

        private ServerSettingsService _shownSettingsService;
        private int _shownAdminSettingsVersion;

        /// <summary>
        /// Shows the latest admin snapshot. The session's event calls it; <see cref="RefreshSession"/> calls it too, so a
        /// snapshot that arrived while the view or the admin check wasn't ready yet still shows (a local version check).
        /// </summary>
        private void ShowAdminSettings()
        {
            if (_shellView == null || !CanAdmin() || !SubSystems.TryGet(out ServerSettingsService service) || !service.HasAdminSettings)
                return;
            if (service == _shownSettingsService && service.AdminSettingsVersion == _shownAdminSettingsVersion)
                return;
            _shownSettingsService = service;
            _shownAdminSettingsVersion = service.AdminSettingsVersion;
            _shellView.SetServerSettings(service.AdminSettings, service.AdminMaps, service.AdminSettingsStatus);
        }

        private static void SendJobPreferences()
        {
            if (SubSystems.TryGet(out ClientSessionSubSystem session)) session.JobPreferencesChanged();
        }

        private void SaveRoster() => _profileStore?.Save(_roster.Characters, _roster.SelectedId);

        /// <summary>A random character from the catalog's names and looks (Decision 3).</summary>
        private CharacterCreatorDraft CreateRandomCharacter() =>
            RandomCharacter.Create(_catalog != null ? _catalog.CharacterNames : null, RandomCharacter.LookOptions.From(_catalog), _random);

        private void NotifyAppearanceChanged()
        {
            if (SubSystems.TryGet(out ClientSessionSubSystem session)) session.AppearanceChanged();
        }

        /// <summary>The selected saved character's preferences; the server keeps them per ckey until the next send.</summary>
        private JobPreferencesMessage? BuildJobPreferences() => _draft?.Preferences?.ToMessage();

        /// <summary>The saved character (never unsaved creator edits).</summary>
        private CharacterAppearance? BuildCharacterAppearance()
        {
            if (_draft == null || _catalog == null) return null;
            return new CharacterAppearance
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
                SkinColor = LobbyCharacterPreview.PaletteColor(CharacterCreatorMockData.SkinColors, _draft.SkinColorIndex),
                EyeColor = LobbyCharacterPreview.PaletteColor(CharacterCreatorMockData.EyeColors, _draft.EyeColorIndex),
                EyebrowColor = _catalog.GetHairColor(_draft.EyebrowColorIndex),
                EyebrowStyle = CharacterAppearance.NormalizeEyebrowStyle(_draft.Eyebrows),
            };
        }

        private static bool CanAdmin() => ClientSessionSubSystem.LocalPlayerIsAdmin();

        private void HandleChatSubmitted(OocChannel channel, string text)
        {
            if (SubSystems.TryGet(out CommsSubSystem comms)) comms.SendOoc(channel, text);
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

        /// <summary>Asks for an observer ghost now, before or during the round; the server decides (<c>observer.md</c> §2).</summary>
        private void HandleObserveRequested()
        {
            if (!InstanceFinder.ClientManager.Started || !SubSystems.TryGet(out EntitySubSystem entities)) return;
            entities.CmdObserve();
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
            if (_shellView == null || _window == null)
            {
                return;
            }

            if (!_preview.Ensure())
            {
                Debug.LogWarning("[Lobby] Character window opened without live preview (human prefab missing).");
            }

            PushPreviewTexture();
            _shellView.SetVisible(false);
            _window.Open();
        }

        /// <summary>
        /// Feeds the Last Round tab (effort <c>2026-11_round-end-loop</c> Decision 6) and opens it once per
        /// ended round, the first time this player sees the lobby after it ended.
        /// </summary>
        private void RefreshLastRound(RoundSubSystem rounds, bool visible)
        {
            RoundOutcome outcome = rounds != null ? rounds.LastOutcome : null;
            if (outcome != null && _roundEndCatalog == null && !_roundEndCatalogFailed
                && !UiCatalogRuntimeLoader.TryLoad(RoundEndAssetPaths.ResourcesCatalogName, RoundEndAssetPaths.RebuildMenuPath,
                    this, out _roundEndCatalog))
                _roundEndCatalogFailed = true;

            bool select = visible && outcome != null && outcome.RoundId != _lastRoundSelectedId;
            if (select)
                _lastRoundSelectedId = outcome.RoundId;
            if (ReferenceEquals(outcome, _lastRoundOutcome) && !select)
                return;

            _lastRoundOutcome = outcome;
            RoundSummaryModel model = outcome != null ? new RoundSummaryModel(outcome, SS3D.Core.Settings.LocalPlayer.Ckey) : null;
            _shellView.SetLastRound(model, _roundEndCatalog != null ? _roundEndCatalog.Style : null, select);
        }

        private void HandleReturnToLobbyRequested()
        {
            if (_shellView == null || _window == null)
            {
                return;
            }

            // Keep the booth warm for the lobby card; it shows the saved character, dressed, never unsaved window edits.
            _preview.SetActive(true);
            _preview.SetAngle(0);
            _preview.ShowCharacter(_draft);
            PushPreviewTexture();
            RefreshCharacterCard();
            _window.SetVisible(false);
            _shellView.SetVisible(true);
        }

        private void HandleCharacterSaved(CharacterCreatorDraft saved)
        {
            if (saved == null || !_roster.Replace(saved))
            {
                return;
            }

            _roster.Select(saved.Id);
            CharacterSelectionChanged();
            Debug.Log($"[Lobby] Character saved: {saved.Name}");
        }

        private void BindSwitcher(CharacterSwitcher switcher)
        {
            switcher.SelectRequested += HandleSelectCharacter;
            switcher.NewRequested += HandleNewCharacter;
            switcher.DuplicateRequested += HandleDuplicateCharacter;
            switcher.DeleteRequested += HandleDeleteCharacter;
        }

        private void HandleSelectCharacter(string id)
        {
            if (_roster.Select(id)) CharacterSelectionChanged();
        }

        /// <summary>New makes a random character and selects it (Decision 3); refused at the cap of ten.</summary>
        private void HandleNewCharacter()
        {
            if (_roster.AddRandom() != null) CharacterSelectionChanged();
        }

        /// <summary>
        /// Duplicate places a copy right below the original and selects it. It keeps the name: "Name (2)" on the
        /// canvas breaks the name rule, and two characters may share a name (Decision 4).
        /// </summary>
        private void HandleDuplicateCharacter(string id)
        {
            if (_roster.Duplicate(id) != null) CharacterSelectionChanged();
        }

        private void HandleDeleteCharacter(string id)
        {
            if (!_roster.Delete(id)) return;
            _preview.ForgetPortrait(id);
            CharacterSelectionChanged();
        }

        /// <summary>
        /// The roster changed (saved, selected, added, duplicated or deleted): write it, send the selected character's
        /// look and preferences (Decision 1), and repaint the card, both switchers, the window and the booth.
        /// </summary>
        private void CharacterSelectionChanged()
        {
            _draft = _roster.Selected;
            SaveRoster();
            NotifyAppearanceChanged();
            SendJobPreferences();
            RefreshCharacterCard();
            _lobbySwitcher?.Refresh();
            if (_window != null && _window.IsVisible)
            {
                // After the window's own Save it already edits the stored character.
                if (!ReferenceEquals(_window.Model.Saved, _draft)) _window.Load(_draft);
                _window.RefreshRoster();
            }
            else
            {
                _preview.ShowCharacter(_draft);
                _window?.RefreshRoster();
            }
        }

        private static Department DepartmentOf(string job) =>
            SubSystems.TryGet(out RoleSubSystem roles) ? CharacterWindowModel.DepartmentOf(job, roles.Jobs) : Department.None;

        private void PushPreviewTexture()
        {
            Texture texture = _preview?.Texture;
            _window?.SetPreviewTexture(texture);
            _shellView?.SetPreviewTexture(texture);
        }

        private void RepaintPortraits()
        {
            _window?.RefreshRoster();
            if (_lobbySwitcher != null && _lobbySwitcher.IsOpen) _lobbySwitcher.Refresh();
        }
    }
}
