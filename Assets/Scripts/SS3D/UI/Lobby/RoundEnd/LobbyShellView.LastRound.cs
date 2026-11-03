using SS3D.Systems.Rounds;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The lobby's Last Round tab (effort <c>2026-11_round-end-loop</c> Decision 6): the last outcome, kept
    /// until the next round ends, so lobby players and latecomers can read it. It sits in the tab area
    /// rather than the design's sidebar card because the real sidebar is the character preview column.
    /// </summary>
    public sealed partial class LobbyShellView
    {
        private const string TabLastRound = "last-round";

        private RoundSummaryModel _lastRound;
        private StyleSheet _roundEndStyle;

        /// <summary>The tab exists (a round has ended this session).</summary>
        public bool HasLastRound => _lastRound != null;

        /// <summary>
        /// Supplies the last outcome as this viewer sees it; null hides the tab. A different round rebuilds an
        /// open tab. <paramref name="select"/> switches to it (once per round, chosen by the host).
        /// </summary>
        public void SetLastRound(RoundSummaryModel model, StyleSheet style, bool select)
        {
            bool changed = !ReferenceEquals(_lastRound?.Outcome, model?.Outcome);
            // Never pull an admin out of the settings form or a player out of a chat line; the tab still appears.
            select &= _activeTab is not (TabServerSettings or TabChat);
            _lastRound = model;
            _roundEndStyle = style;
            if (!changed && !select)
                return;

            if (model == null && _activeTab == TabLastRound)
                _activeTab = TabServerInfo;
            if (select && model != null)
            {
                SelectTab(TabLastRound);
                return;
            }

            RefreshTabs();
            if (_activeTab == TabLastRound)
                ShowTab(TabLastRound);
        }

        private void BuildLastRoundTab(VisualElement parent)
        {
            VisualElement root = new() { name = "last-round" };
            root.AddToClassList(RoundSummaryView.RootClass);
            root.AddToClassList("round-end--tab");
            if (_roundEndStyle != null)
                root.styleSheets.Add(_roundEndStyle);
            parent.Add(root);

            if (_lastRound == null)
            {
                VisualElement empty = new();
                empty.AddToClassList("round-end__tab-empty");
                empty.Add(RoundSummaryView.Titling("No round has ended on this server yet.", "round-end__tab-note"));
                root.Add(empty);
                return;
            }

            RoundOutcome outcome = _lastRound.Outcome;
            RoundSummaryView.ApplyWinner(root, outcome.Winner);

            VisualElement header = new();
            header.AddToClassList("round-end__header");
            header.Add(RoundSummaryView.Arcade("LAST ROUND", "round-end__header-round"));
            header.Add(RoundSummaryView.Terminal($"Round {outcome.RoundId} · {RoundOutcomeText.Duration(outcome.DurationSeconds)}"));
            VisualElement spacer = new() { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("round-end__spacer");
            header.Add(spacer);
            header.Add(RoundSummaryView.Titling("Kept until the next round ends.", "round-end__tab-note"));
            root.Add(header);

            RoundSummaryView.BuildBody(root, _lastRound);
        }
    }
}
