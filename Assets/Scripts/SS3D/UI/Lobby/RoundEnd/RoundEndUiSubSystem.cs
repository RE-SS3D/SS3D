using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using SS3D.Systems.Rounds;
using SS3D.UI.Shell;
using SS3D.UI.Shell.Catalog;
using SS3D.UI.Shell.RoundEnd;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// Client host of the round-end overlay (effort <c>2026-11_round-end-loop</c> Decisions 3–5). While the
    /// round is Ending it paints the outcome over whatever the player sees, living or ghost, with the beat's
    /// countdown; Tab or the chevron collapse it to a banner. Players in the lobby read the same outcome in
    /// the shell's Last Round tab instead (the lobby window sits above this layer).
    /// </summary>
    public sealed class RoundEndUiSubSystem : SubSystem
    {
        private RoundEndAssetCatalog _catalog;
        private bool _catalogFailed;
        private RoundEndOverlay _overlay;
        private InputSubSystem _input;
        private RoundSummaryModel _model;

        /// <summary>The overlay, once built; for automation.</summary>
        public RoundEndOverlay Overlay => _overlay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (UnityEngine.Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            if (SubSystems.TryGet(out RoundEndUiSubSystem _))
                return;

            GameObject host = new(nameof(RoundEndUiSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<RoundEndUiSubSystem>();
        }

        protected override void OnDestroyed()
        {
            if (_input != null)
                _input.ToggleRoundSummary.performed -= HandleToggle;
            _overlay?.Detach();
            base.OnDestroyed();
        }

        private void Update()
        {
            BindInput();
            if (!SubSystems.TryGet(out RoundSubSystem rounds) || !rounds.IsSpawned)
            {
                Hide();
                return;
            }

            RoundOutcome outcome = rounds.LastOutcome;
            bool ending = rounds.CurrentRoundState == RoundState.Ending && outcome != null && outcome.RoundId == rounds.RoundId;
            if (!ending)
            {
                Hide();
                return;
            }

            if (!EnsureOverlay())
                return;

            if (_model == null || _model.Outcome != outcome)
                _model = new RoundSummaryModel(outcome, SS3D.Core.Settings.LocalPlayer.Ckey);
            _overlay.Show(_model, rounds.EndSecondsLeft, rounds.EndHeld);
        }

        private void Hide()
        {
            _model = null;
            _overlay?.Hide();
        }

        private void BindInput()
        {
            if (_input != null || !SubSystems.TryGet(out InputSubSystem input) || input.ToggleRoundSummary == null)
                return;

            _input = input;
            _input.ToggleRoundSummary.performed += HandleToggle;
        }

        private void HandleToggle(InputAction.CallbackContext context) => ToggleCollapsed();

        /// <summary>Collapses or expands the overlay while it is up; for Tab, the chevron and automation.</summary>
        public void ToggleCollapsed()
        {
            if (_overlay != null && _overlay.IsVisible)
                _overlay.SetCollapsed(!_overlay.IsCollapsed);
        }

        private bool EnsureOverlay()
        {
            if (_overlay != null && _overlay.IsAttached)
                return true;
            if (_catalogFailed)
                return false;

            if (_catalog == null && !UiCatalogRuntimeLoader.TryLoad(RoundEndAssetPaths.ResourcesCatalogName,
                    RoundEndAssetPaths.RebuildMenuPath, this, out _catalog))
            {
                _catalogFailed = true;
                return false;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem shell) || !shell.TryGetLayer(UiLayer.Overlay, out VisualElement layer))
                return false;

            // Pointer over the panel must count as UI, so a click on it never reaches the world.
            InputInterface.RegisterDocument(shell.Document);
            _overlay = new RoundEndOverlay(_catalog);
            _overlay.CollapseToggleRequested += ToggleCollapsed;
            _overlay.Attach(layer);
            return true;
        }
    }
}
