using SS3D.Systems.Rounds;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The lobby's Last Round tab (effort <c>2026-11_round-end-loop</c> Decision 6): the last outcome, kept until the
    /// next round ends, so lobby players and latecomers can read it.
    /// </summary>
    public sealed class LobbyLastRoundView
    {
        private RoundSummaryModel _model;
        private StyleSheet _style;

        public VisualElement Root { get; }

        public RoundSummaryModel Model => _model;

        public LobbyLastRoundView()
        {
            Root = new VisualElement { name = "last-round" };
            Root.AddToClassList(RoundSummaryView.RootClass);
            Root.AddToClassList("round-end--tab");
            Rebuild();
        }

        /// <summary>Shows <paramref name="model"/>; a different outcome rebuilds the tab.</summary>
        public void Set(RoundSummaryModel model, StyleSheet style)
        {
            if (style != null && style != _style)
            {
                if (_style != null) Root.styleSheets.Remove(_style);
                _style = style;
                Root.styleSheets.Add(style);
            }

            if (ReferenceEquals(model?.Outcome, _model?.Outcome)) return;
            _model = model;
            Rebuild();
        }

        private void Rebuild()
        {
            Root.Clear();
            RoundSummaryView.ApplyWinner(Root, RoundWinner.None);
            if (_model == null)
            {
                VisualElement empty = new();
                empty.AddToClassList("round-end__tab-empty");
                empty.Add(RoundSummaryView.Titling("No round has ended on this server yet.", "round-end__tab-note"));
                Root.Add(empty);
                return;
            }

            RoundOutcome outcome = _model.Outcome;
            RoundSummaryView.ApplyWinner(Root, outcome.Winner);

            VisualElement header = new();
            header.AddToClassList("round-end__header");
            header.Add(RoundSummaryView.Arcade("LAST ROUND", "round-end__header-round"));
            header.Add(RoundSummaryView.Terminal($"Round {outcome.RoundId} · {RoundOutcomeText.Duration(outcome.DurationSeconds)}"));
            VisualElement spacer = new() { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("round-end__spacer");
            header.Add(spacer);
            header.Add(RoundSummaryView.Titling("Kept until the next round ends.", "round-end__tab-note"));
            Root.Add(header);

            RoundSummaryView.BuildBody(Root, _model);
        }
    }
}
