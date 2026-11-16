using System;
using SS3D.Systems.Rounds;
using SS3D.UI.Shell;
using SS3D.UI.Shell.RoundEnd;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The in-round summary (<c>round-end.md</c> §5, effort Decision 4) on the UiShell Overlay layer: a solid,
    /// centred panel that only takes the pointer over itself, so movement and combat stay live. It collapses
    /// to a top banner (chevron or Tab). Presentation only; <see cref="RoundEndUiSubSystem"/> drives it.
    /// </summary>
    public sealed class RoundEndOverlay : IUiSurface
    {
        private readonly RoundEndAssetCatalog _catalog;

        private VisualElement _root;
        private VisualElement _panel;
        private VisualElement _banner;
        private int _shownRoundId = -1;
        private int _secondsLeft;
        private bool _held;

        public RoundEndOverlay(RoundEndAssetCatalog catalog)
        {
            _catalog = catalog;
        }

        /// <summary>The chevron was clicked.</summary>
        public event Action CollapseToggleRequested;

        public bool IsAttached => _root != null;

        public bool IsVisible => _root != null && _root.style.display == DisplayStyle.Flex;

        public bool IsCollapsed { get; private set; }

        /// <summary>The overlay's root, for automation.</summary>
        public VisualElement Root => _root;

        public void Attach(VisualElement layerRoot)
        {
            if (_root != null)
                return;

            _root = new VisualElement { name = "round-end-overlay", pickingMode = PickingMode.Ignore };
            _root.AddToClassList(RoundSummaryView.RootClass);
            if (_catalog != null && _catalog.Style != null)
                _root.styleSheets.Add(_catalog.Style);
            _root.style.display = DisplayStyle.None;
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            layerRoot.Add(_root);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            _root.EnableInClassList("round-end--compact", evt.newRect.height <= 720f);
        }

        public void Detach()
        {
            _root?.RemoveFromHierarchy();
            _root = null;
            _panel = null;
            _banner = null;
            _shownRoundId = -1;
        }

        /// <summary>Shows <paramref name="model"/>, rebuilding only when it is a different round.</summary>
        public void Show(RoundSummaryModel model, int secondsLeft, bool held)
        {
            if (_root == null)
                return;

            if (_shownRoundId != model.Outcome.RoundId || _panel == null)
            {
                Build(model);
                _shownRoundId = model.Outcome.RoundId;
            }

            _root.style.display = DisplayStyle.Flex;
            SetCountdown(secondsLeft, held);
            ApplyCollapsed();
        }

        public void Hide()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.None;
            IsCollapsed = false;
        }

        public void SetCollapsed(bool collapsed)
        {
            IsCollapsed = collapsed;
            ApplyCollapsed();
        }

        public void SetCountdown(int secondsLeft, bool held)
        {
            _secondsLeft = secondsLeft;
            _held = held;
            if (_root == null)
                return;

            foreach (Label label in _root.Query<Label>("round-end-countdown").ToList())
            {
                label.text = RoundSummaryModel.Countdown(secondsLeft, held, label.ClassListContains("round-end__countdown--short"));
                label.EnableInClassList("round-end__countdown--held", held);
            }

            foreach (VisualElement fill in _root.Query("round-end-progress-fill").ToList())
            {
                fill.style.width = Length.Percent(RoundSummaryModel.Progress(secondsLeft, held) * 100f);
                fill.EnableInClassList("round-end__progress-fill--held", held);
            }

            Label paused = _root.Q<Label>("round-end-paused");
            if (paused != null)
                paused.style.display = held ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyCollapsed()
        {
            if (_panel == null)
                return;

            _panel.style.display = IsCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
            _banner.style.display = IsCollapsed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Build(RoundSummaryModel model)
        {
            _root.Clear();
            RoundSummaryView.ApplyWinner(_root, model.Outcome.Winner);
            _panel = BuildPanel(model);
            _banner = BuildBanner(model);
            _root.Add(_panel);
            _root.Add(_banner);
            SetCountdown(_secondsLeft, _held);
        }

        private VisualElement BuildPanel(RoundSummaryModel model)
        {
            VisualElement panel = new() { name = "round-end-panel" };
            panel.AddToClassList("round-end__panel");

            VisualElement header = new();
            header.AddToClassList("round-end__header");
            header.Add(RoundSummaryView.Arcade($"ROUND {model.Outcome.RoundId}", "round-end__header-round"));
            header.Add(RoundSummaryView.Terminal(RoundOutcomeText.Duration(model.Outcome.DurationSeconds)));
            header.Add(Spacer());
            Label paused = RoundSummaryView.Arcade("PAUSED", "round-end__paused");
            paused.name = "round-end-paused";
            header.Add(paused);
            header.Add(Countdown(shortForm: false));
            header.Add(Chevron("round-end-collapse", up: true, "Collapse (Tab)"));
            panel.Add(header);
            panel.Add(Progress());

            RoundSummaryView.BuildBody(panel, model);

            VisualElement footer = new();
            footer.AddToClassList("round-end__footer");
            footer.Add(RoundSummaryView.Arcade("TAB", "round-end__key"));
            footer.Add(RoundSummaryView.Titling("Collapse · movement and combat stay active", "round-end__hint"));
            panel.Add(footer);
            return panel;
        }

        private VisualElement BuildBanner(RoundSummaryModel model)
        {
            VisualElement banner = new() { name = "round-end-banner" };
            banner.AddToClassList("round-end__banner");

            VisualElement row = new();
            row.AddToClassList("round-end__banner-row");

            VisualElement result = new();
            result.AddToClassList("round-end__banner-result");
            result.Add(RoundSummaryView.Terminal(model.Title.ToUpperInvariant(), "round-end__title", "round-end__banner-title"));
            row.Add(result);

            VisualElement body = new();
            body.AddToClassList("round-end__banner-body");
            if (model.Outcome.StationDestroyed)
                body.Add(RoundSummaryView.DestroyedBadge());
            body.Add(RoundSummaryView.Titling(model.Outcome.Mode, "round-end__banner-text", "round-end__ellipsis"));
            body.Add(RoundSummaryView.Titling("·", "round-end__banner-dot"));
            body.Add(RoundSummaryView.Titling("You:", "round-end__hint"));
            body.Add(RoundSummaryView.Terminal(model.FateText, "round-end__fate", RoundSummaryView.FateClass(model.Fate)));
            row.Add(body);

            VisualElement end = new();
            end.AddToClassList("round-end__banner-end");
            end.Add(Countdown(shortForm: true));
            end.Add(Chevron("round-end-expand", up: false, "Expand (Tab)"));
            row.Add(end);

            banner.Add(row);
            banner.Add(Progress());
            return banner;
        }

        private static VisualElement Spacer()
        {
            VisualElement spacer = new() { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("round-end__spacer");
            return spacer;
        }

        private static Label Countdown(bool shortForm)
        {
            Label label = RoundSummaryView.Terminal("", "round-end__countdown");
            label.name = "round-end-countdown";
            label.EnableInClassList("round-end__countdown--short", shortForm);
            return label;
        }

        private static VisualElement Progress()
        {
            VisualElement track = new() { pickingMode = PickingMode.Ignore };
            track.AddToClassList("round-end__progress");
            VisualElement fill = new() { name = "round-end-progress-fill", pickingMode = PickingMode.Ignore };
            fill.AddToClassList("round-end__progress-fill");
            track.Add(fill);
            return track;
        }

        /// <summary>A drawn chevron (two borders of a rotated square); the game fonts have no arrow glyphs.</summary>
        private Button Chevron(string name, bool up, string tooltip)
        {
            Button button = new(() => CollapseToggleRequested?.Invoke()) { name = name, text = "", tooltip = tooltip };
            button.AddToClassList("round-end__chevron");
            button.focusable = false;
            VisualElement glyph = new() { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("round-end__chevron-glyph");
            glyph.EnableInClassList("round-end__chevron-glyph--down", !up);
            button.Add(glyph);
            return button;
        }
    }
}
