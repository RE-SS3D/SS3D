using System;
using System.Collections;
using System.IO;
using System.Linq;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Client;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Data.Persistence;
using SS3D.Systems.Area;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inputs;
using SS3D.Systems.Persistence;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.UI.Shell;
using SS3D.UI.Shell.BugReport;
using SS3D.UI.Shell.Catalog;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.Systems.BugReports
{
    /// <summary>
    /// Client host of the bug report dialog (<c>sandbox-server.md</c> §7). F8, a bare <c>/bug</c> and the
    /// Escape menu's "Report a bug" open it; the round, position and screenshot are frozen at that
    /// moment. A client sends the report to the server (<see cref="BugReportSubmitMessage"/>); a player
    /// who is their own host saves a bundle in <c>Data/Exports/</c> instead.
    /// </summary>
    public sealed class BugReportSubSystem : SubSystem
    {
        private const float ResultTimeoutSeconds = 15f;
        private const int NetworkScreenshotWidth = 960;

        /// <summary>Maps that ship in <c>Builds/Game/Data/StationTemplates/</c>; any other map defaults the map option on.</summary>
        private static readonly string[] ShippedMaps = { "ministation", "classic-ministation" };

        private BugReportAssetCatalog _catalog;
        private bool _catalogFailed;
        private BugReportView _view;
        private InputSubSystem _input;
        private IInputHandle _dialogHandle;
        private ClientManager _clientManager;

        private BugReportClientInfo _info;
        private Texture2D _screenshot;
        private string[] _logTail = Array.Empty<string>();
        private bool _local;
        private bool _capturing;

        private float _fps = 60f;
        private float _nextAllowedAt = -1f;
        private float _resultDueAt = -1f;
        private int _editorUsedInRound = -1;

        private string _lastBundlePath;
        private BugReportBundleReport _lastReport;

        /// <summary>The dialog is open (form or saved screen).</summary>
        public bool IsOpen => _view != null && _view.IsOpen;

        /// <summary>The dialog's view, once built; for automation.</summary>
        public BugReportView View => _view;

        /// <summary>Path of the last local bundle, or null.</summary>
        public string LastBundlePath => _lastBundlePath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (UnityEngine.Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            if (SubSystems.TryGet(out BugReportSubSystem _))
                return;

            GameObject host = new(nameof(BugReportSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<BugReportSubSystem>();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            BugReportRequests.OpenHandler = HandleOpenRequest;
        }

        protected override void OnDestroyed()
        {
            if (BugReportRequests.OpenHandler == HandleOpenRequest)
                BugReportRequests.OpenHandler = null;
            if (_input != null)
                _input.OpenBugReport.performed -= HandleOpenAction;
            if (_clientManager != null)
                _clientManager.UnregisterBroadcast<BugReportResultMessage>(HandleResult);
            ReleaseDialogInput();
            _view?.Detach();
            DestroyScreenshot();
            base.OnDestroyed();
        }

        /// <summary>Opens the dialog, as F8 does. False when there is no UI to open it on.</summary>
        public bool Open(string trigger = "opened this")
        {
            if (IsOpen || _capturing)
                return true;
            if (!EnsureView())
                return false;

            StartCoroutine(CaptureThenOpen(trigger));
            return true;
        }

        /// <summary>Closes the dialog without filing anything.</summary>
        public void Close()
        {
            _view?.Close();
            ReleaseDialogInput();
        }

        private bool HandleOpenRequest() => Open();

        private void HandleOpenAction(InputAction.CallbackContext context) =>
            Open("pressed " + _input.OpenBugReport.GetBindingDisplayString());

        private void Update()
        {
            float now = Time.unscaledTime;
            if (Time.unscaledDeltaTime > 0f)
                _fps = Mathf.Lerp(_fps, 1f / Time.unscaledDeltaTime, 0.05f);

            BindInput();
            BindClient();
            TrackEditorUse();

            if (_resultDueAt >= 0f && now > _resultDueAt)
            {
                _resultDueAt = -1f;
                _view?.EndSending(now);
                _view?.ShowToast("No answer from the server; your report may not have been saved.", false, now);
            }

            if (_view == null)
                return;

            _view.Tick(now);
            if (IsOpen)
                HandleKeys();
            else
                ReleaseDialogInput();
        }

        private void HandleKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            bool ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed
                || keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed;
            if (ctrl && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                _view.TrySubmit();
        }

        private void BindInput()
        {
            if (_input != null || !SubSystems.TryGet(out InputSubSystem input) || input.OpenBugReport == null)
                return;

            _input = input;
            _input.OpenBugReport.performed += HandleOpenAction;
        }

        private void BindClient()
        {
            // InstanceFinder logs and rescans the scenes on every miss, and this runs each frame.
            if (NetworkManager.Instances.Count == 0)
                return;

            ClientManager client = InstanceFinder.ClientManager;
            if (client == null || client == _clientManager)
                return;

            if (_clientManager != null)
                _clientManager.UnregisterBroadcast<BugReportResultMessage>(HandleResult);
            _clientManager = client;
            _clientManager.RegisterBroadcast<BugReportResultMessage>(HandleResult);
        }

        /// <summary>The map option defaults on when the map editor was used this round.</summary>
        private void TrackEditorUse()
        {
            if (SubSystems.TryGet(out MapEditorSubSystem editor) && editor.IsActive
                && SubSystems.TryGet(out RoundSubSystem rounds))
                _editorUsedInRound = rounds.RoundId;
        }

        private bool EnsureView()
        {
            if (_view != null && _view.IsAttached)
                return true;
            if (_catalogFailed)
                return false;

            if (_catalog == null && !UiCatalogRuntimeLoader.TryLoad(BugReportAssetPaths.ResourcesCatalogName,
                    BugReportAssetPaths.RebuildMenuPath, this, out _catalog))
            {
                _catalogFailed = true;
                return false;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem shell) || !shell.TryGetLayer(UiLayer.Modal, out VisualElement layer))
                return false;

            InputInterface.RegisterDocument(shell.Document);
            _view = new BugReportView(_catalog);
            _view.SubmitRequested += HandleSubmit;
            _view.CloseRequested += Close;
            _view.OpenFolderRequested += () => MapFileSharing.Reveal(_lastBundlePath);
            _view.OpenIssueRequested += OpenIssue;
            _view.Attach(layer);
            return true;
        }

        /// <summary>
        /// Freezes the moment, then waits for the frame to finish so the screenshot shows what the player saw
        /// (and not the Escape menu, which the lobby closes before asking).
        /// </summary>
        private IEnumerator CaptureThenOpen(string trigger)
        {
            _capturing = true;
            BugReportClientInfo info = CaptureInfo();
            yield return null;
            yield return new WaitForEndOfFrame();

            Texture2D shot = null;
            try
            {
                shot = ScreenCapture.CaptureScreenshotAsTexture();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BugReport] Screenshot failed: {exception.Message}");
            }

            _capturing = false;
            DestroyScreenshot();
            _screenshot = shot;
            _info = info;
            _logTail = BugReportLogTail.Snapshot();
            _local = IsLocalSession();
            ShowForm(trigger);
        }

        private void ShowForm(string trigger)
        {
            BugReportFormModel model = new()
            {
                Info = _info,
                Screenshot = _screenshot,
                ScreenshotTitle = "Screenshot · taken when you " + trigger,
                ScreenshotFile = _screenshot != null ? $"screenshot.png · {_screenshot.width}×{_screenshot.height}" : "",
                LogLines = _logTail.Length,
                Local = _local,
            };

            if (_local && !string.IsNullOrEmpty(_info.map) && SubSystems.TryGet(out PersistenceSubSystem _))
            {
                string map = _info.map.Length > 32 ? _info.map.Substring(0, 31) + "…" : _info.map;
                model.MapOptionLabel = $"Include current map ({map}, unsaved edits included)";
                model.MapOptionDefault = _info.unsavedEdits || _editorUsedInRound == _info.roundId
                    || !ShippedMaps.Contains(_info.map, StringComparer.OrdinalIgnoreCase);
            }

            float now = Time.unscaledTime;
            _view.ShowForm(model);
            _view.SetRateLimit(_local ? -1f : _nextAllowedAt, now);
            _dialogHandle ??= _input?.PushContext(InputContext.Dialog);
        }

        private void HandleSubmit(BugReportDraft draft)
        {
            if (_local)
                SaveLocal(draft);
            else
                SendToServer(draft);
        }

        private void SendToServer(BugReportDraft draft)
        {
            float now = Time.unscaledTime;
            if (_clientManager == null || !_clientManager.Started)
            {
                _view.EndSending(now);
                _view.ShowToast("Not connected to a server; the report was not sent.", false, now);
                return;
            }

            _clientManager.Broadcast(new BugReportSubmitMessage
            {
                Summary = draft.Summary,
                Details = draft.Details,
                Severity = BugReportText.SeverityKey(draft.Severity),
                Crashed = draft.Crashed,
                Client = _info,
                ScreenshotJpg = draft.IncludeScreenshot ? EncodeReduced(_screenshot) : Array.Empty<byte>(),
                LogTail = BugReportText.ClampLog(_logTail),
            });
            _resultDueAt = now + ResultTimeoutSeconds;
        }

        /// <summary>The dialog stays open until the server answers, so a refused report keeps its text.</summary>
        private void HandleResult(BugReportResultMessage result)
        {
            float now = Time.unscaledTime;
            _resultDueAt = -1f;
            if (result.RetryAfterSeconds > 0f)
                _nextAllowedAt = now + result.RetryAfterSeconds;

            if (_view == null)
                return;

            if (result.Saved)
            {
                Close();
                DestroyScreenshot();
            }
            else
            {
                _view.EndSending(now);
                _view.SetRateLimit(_nextAllowedAt, now);
            }

            _view.ShowToast(result.Message, result.Saved, now);
        }

        private void SaveLocal(BugReportDraft draft)
        {
            float now = Time.unscaledTime;
            string mapJson = null;
            if (draft.IncludeMap && SubSystems.TryGet(out PersistenceSubSystem persistence))
                mapJson = persistence.CaptureStationTemplateJson(_info.map);

            BugReportBundleReport report = new()
            {
                summary = draft.Summary,
                details = draft.Details,
                severity = BugReportText.SeverityKey(draft.Severity),
                crashed = draft.Crashed,
                screenshotIncluded = draft.IncludeScreenshot,
                mapIncluded = mapJson != null,
                client = _info,
                logTail = _logTail,
            };

            byte[] png = draft.IncludeScreenshot && _screenshot != null ? _screenshot.EncodeToPNG() : null;
            if (!BugReportBundle.TryWrite(MapFileSharing.ExportsFolder, BugReportBundle.BaseName(_info, DateTime.Now), report,
                    png, BugReportBundle.DefaultLogFiles(), _info.map, mapJson, out string path, out string error))
            {
                _view.EndSending(now);
                _view.ShowToast($"Could not save the report: {error}", false, now);
                return;
            }

            _lastBundlePath = path;
            _lastReport = report;
            Debug.Log($"[BugReport] Saved {path}");
            _view.ShowSaved(path);
            DestroyScreenshot();
        }

        private void OpenIssue()
        {
            if (_lastReport == null)
                return;

            bool mapEditor = _lastReport.mapIncluded || (_lastReport.client.context ?? "").StartsWith("MAP EDITOR", StringComparison.Ordinal);
            UnityEngine.Application.OpenURL(BugReportIssueLink.Build(_lastReport, Path.GetFileName(_lastBundlePath), mapEditor));
        }

        /// <summary>A player who is their own host (or not connected) has no remote server to receive the report.</summary>
        private static bool IsLocalSession()
        {
            bool client = InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started;
            bool server = InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started;
            return !client || server;
        }

        private BugReportClientInfo CaptureInfo()
        {
            BugReportClientInfo info = new()
            {
                version = BuildInfo.Label,
                os = SystemInfo.operatingSystem,
                gpu = SystemInfo.graphicsDeviceName,
                fps = Mathf.RoundToInt(_fps),
                playerName = LocalPlayer.Ckey ?? "",
                capturedAtUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                roundState = "Lobby",
                context = "LOBBY",
            };

            bool client = InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started;
            bool server = InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started;
            if (SubSystems.TryGet(out PlayerSubSystem players))
                info.players = players.OnlinePlayers.Count();
            info.networkRole = server && client ? (info.players > 1 ? "Host" : "Own host") : client ? "Client" : "Offline";

            if (!SubSystems.TryGet(out RoundSubSystem rounds))
                return info;

            RoundState state = rounds.CurrentRoundState;
            bool inRound = state is RoundState.Ongoing or RoundState.Ending;
            info.roundId = rounds.RoundId;
            info.roundSeconds = rounds.CurrentTimerSeconds;
            info.roundState = inRound ? state.ToString() : "Lobby";
            if (rounds.CurrentStationLoadState == StationLoadState.Ready)
                info.map = rounds.StationName;

            bool editing = SubSystems.TryGet(out MapEditorSubSystem editor) && editor.IsActive;
            if (editor != null)
                info.unsavedEdits = editor.HasUnsavedChanges;

            if (client && inRound && SubSystems.TryGet(out EntitySubSystem entities)
                && entities.TryGetSpawnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity))
            {
                info.context = entity.GetComponent<HumanoidGhostController>() != null ? "GHOST" : "IN ROUND · ALIVE";
                info.hasPosition = true;
                info.position = entity.transform.position;
                info.area = AreaAt(info.position);
            }

            if (editing)
                info.context = $"MAP EDITOR · {editor.CurrentTool.ToString().ToUpperInvariant()} TOOL";

            return info;
        }

        private static string AreaAt(Vector3 position)
        {
            try
            {
                if (SubSystems.TryGet(out AreaSubSystem areas) && SubSystems.TryGet(out TileSubSystem tiles)
                    && tiles.QueryService != null
                    && areas.TryGetAreaForTile(tiles.QueryService.WorldToTile(position), out AreaRecord record))
                    return record.DisplayName ?? "";
            }
            catch (Exception exception)
            {
                // The area map is optional context; a client without it reports coordinates only.
                Debug.LogWarning($"[BugReport] Area lookup failed: {exception.Message}");
            }

            return "";
        }

        /// <summary>A reduced JPEG for the network (<c>sandbox-server.md</c> §7: "screenshots are sent at reduced size").</summary>
        private static byte[] EncodeReduced(Texture2D source)
        {
            if (source == null)
                return Array.Empty<byte>();

            int width = Mathf.Min(NetworkScreenshotWidth, source.width);
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * (width / (float)source.width)));
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0);
            RenderTexture previous = RenderTexture.active;
            Texture2D small = new(width, height, TextureFormat.RGB24, false);
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                small.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                small.Apply();
                foreach (int quality in new[] { 75, 55, 35 })
                {
                    byte[] jpg = small.EncodeToJPG(quality);
                    if (jpg.Length <= BugReportText.MaxScreenshotBytes)
                        return jpg;
                }

                return Array.Empty<byte>();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Destroy(small);
            }
        }

        private void ReleaseDialogInput()
        {
            _dialogHandle?.Dispose();
            _dialogHandle = null;
        }

        private void DestroyScreenshot()
        {
            if (_screenshot == null)
                return;
            // The view may still show it as a thumbnail; only drop it once the form is gone.
            if (IsOpen && !_view.IsSavedScreen)
                return;
            Destroy(_screenshot);
            _screenshot = null;
        }
    }
}
