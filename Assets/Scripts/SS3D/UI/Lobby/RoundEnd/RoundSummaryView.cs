using SS3D.Systems.Rounds;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// Builds the round-end ledger (<c>round-end.md</c> §5) from a <see cref="RoundSummaryModel"/>: result,
    /// gamemode and the viewer's fate, the antagonist roster with objectives, and everyone else. Shared by
    /// the in-round overlay (<see cref="RoundEndOverlay"/>) and the lobby's Last Round tab. Styles: <c>RoundEnd.uss</c>.
    /// </summary>
    public static class RoundSummaryView
    {
        public const string RootClass = "round-end";

        /// <summary>The winner tint class for <paramref name="winner"/>, set on the summary's root.</summary>
        public static string WinnerClass(RoundWinner winner) => winner switch
        {
            RoundWinner.Operatives or RoundWinner.OperativesMinor => "round-end--ops",
            RoundWinner.Crew => "round-end--crew",
            _ => "round-end--none",
        };

        public static void ApplyWinner(VisualElement root, RoundWinner winner)
        {
            foreach (RoundWinner each in new RoundWinner[] { RoundWinner.None, RoundWinner.Operatives, RoundWinner.Crew })
                root.RemoveFromClassList(WinnerClass(each));
            root.AddToClassList(WinnerClass(winner));
        }

        /// <summary>Result, facts, antagonists and the crew roster; the roster grows and scrolls.</summary>
        public static void BuildBody(VisualElement parent, RoundSummaryModel model)
        {
            parent.Add(BuildResult(model));
            parent.Add(BuildFacts(model));
            if (model.HasAntagonists)
                parent.Add(BuildAntagonists(model));
            parent.Add(BuildCrew(model));
        }

        public static Label Text(string text, string font, params string[] classes)
        {
            Label label = new(text) { enableRichText = false, pickingMode = PickingMode.Ignore };
            label.AddToClassList(font);
            foreach (string cls in classes)
                label.AddToClassList(cls);
            return label;
        }

        public static Label Arcade(string text, params string[] classes) => Text(text, "round-end__arcade", classes);

        public static Label Terminal(string text, params string[] classes) => Text(text, "round-end__terminal", classes);

        public static Label Titling(string text, params string[] classes) => Text(text, "round-end__titling", classes);

        public static Label DestroyedBadge() => Arcade("STATION DESTROYED", "round-end__destroyed");

        public static string FateClass(RoundSummaryModel.FateKind fate) => fate switch
        {
            RoundSummaryModel.FateKind.Alive => "round-end__alive",
            RoundSummaryModel.FateKind.Dead => "round-end__dead",
            _ => "round-end__neutral",
        };

        private static VisualElement BuildResult(RoundSummaryModel model)
        {
            VisualElement result = new() { name = "round-end-result" };
            result.AddToClassList("round-end__result");
            result.Add(Arcade("ROUND RESULT"));
            VisualElement row = new();
            row.AddToClassList("round-end__result-row");
            row.Add(Terminal(model.Title.ToUpperInvariant(), "round-end__title"));
            if (model.Outcome.StationDestroyed)
                row.Add(DestroyedBadge());
            result.Add(row);
            result.Add(Titling(model.Outcome.Reason, "round-end__reason"));
            return result;
        }

        private static VisualElement BuildFacts(RoundSummaryModel model)
        {
            VisualElement facts = new();
            facts.AddToClassList("round-end__facts");

            VisualElement mode = Fact("GAMEMODE", divided: false);
            VisualElement modeValue = mode.Q(className: "round-end__fact-value");
            if (model.ShowConfiguredMode)
            {
                // The admin-configured mode, struck through: the reveal (round-end.md §4).
                Label configured = Terminal($"<s>{model.Outcome.ConfiguredMode}</s>", "round-end__mode-configured");
                configured.enableRichText = true;
                modeValue.Add(configured);
                modeValue.Add(Terminal("→", "round-end__mode-configured"));
            }
            modeValue.Add(Terminal(string.IsNullOrEmpty(model.Outcome.Mode) ? "Unknown" : model.Outcome.Mode,
                "round-end__mode", "round-end__ellipsis"));
            facts.Add(mode);

            VisualElement fate = Fact("YOUR FATE", divided: true);
            VisualElement fateValue = fate.Q(className: "round-end__fact-value");
            fateValue.Add(Terminal(model.FateText, "round-end__fate", FateClass(model.Fate)));
            fateValue.Add(Titling(model.FateWho, "round-end__fate-who", "round-end__ellipsis"));
            facts.Add(fate);
            return facts;
        }

        private static VisualElement Fact(string label, bool divided)
        {
            VisualElement fact = new();
            fact.AddToClassList("round-end__fact");
            fact.EnableInClassList("round-end__fact--divided", divided);
            fact.Add(Arcade(label));
            VisualElement value = new();
            value.AddToClassList("round-end__fact-value");
            fact.Add(value);
            return fact;
        }

        private static VisualElement BuildAntagonists(RoundSummaryModel model)
        {
            VisualElement section = new() { name = "round-end-antagonists" };
            section.AddToClassList("round-end__section");
            section.Add(SectionHead(model.Outcome.AntagonistLabel.ToUpperInvariant(), model.AntagonistCount, antagonist: true));

            foreach (RoundOutcomeObjective objective in model.Outcome.Objectives)
            {
                VisualElement row = new();
                row.AddToClassList("round-end__objective");
                row.Add(Arcade("OBJECTIVE", "round-end__objective-label"));
                row.Add(Titling(objective.Text, "round-end__objective-text"));
                bool met = objective.Result == ObjectiveResult.Met;
                row.Add(Arcade(met ? "MET" : "FAILED", "round-end__objective-result",
                    met ? "round-end__objective-result--met" : "round-end__objective-result--failed"));
                section.Add(row);
            }

            VisualElement grid = new();
            grid.AddToClassList("round-end__ops-grid");
            foreach (RoundSummaryModel.Row row in model.Antagonists)
                grid.Add(PlayerRow(row, half: true, showJob: false));
            section.Add(grid);
            return section;
        }

        private static VisualElement BuildCrew(RoundSummaryModel model)
        {
            VisualElement crew = new() { name = "round-end-crew" };
            crew.AddToClassList("round-end__crew");
            crew.Add(SectionHead(model.CrewLabel, model.CrewCount, antagonist: false));

            ScrollView roster = new(ScrollViewMode.Vertical) { name = "round-end-roster" };
            roster.AddToClassList("round-end__roster");
            roster.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            if (model.Crew.Count == 0)
                roster.Add(Titling("Nobody took a body this round.", "round-end__empty"));
            foreach (RoundSummaryModel.Row row in model.Crew)
                roster.Add(PlayerRow(row, half: false, showJob: true));
            crew.Add(roster);
            return crew;
        }

        private static VisualElement SectionHead(string label, string count, bool antagonist)
        {
            VisualElement head = new();
            head.AddToClassList("round-end__section-head");
            Label title = Arcade(label, "round-end__section-label");
            title.EnableInClassList("round-end__section-label--antag", antagonist);
            head.Add(title);
            head.Add(Terminal(count));
            return head;
        }

        private static VisualElement PlayerRow(RoundSummaryModel.Row row, bool half, bool showJob)
        {
            VisualElement element = new();
            element.AddToClassList("round-end__row");
            element.EnableInClassList("round-end__row--half", half);
            element.EnableInClassList("round-end__row--you", row.IsViewer);
            if (row.IsViewer)
                element.name = "round-end-row-you";

            Label name = Titling(row.Name, "round-end__row-name", "round-end__ellipsis");
            name.EnableInClassList("round-end__row-name--dead", !row.Alive);
            element.Add(name);
            if (row.IsViewer)
                element.Add(Arcade("YOU", "round-end__you"));
            if (showJob)
                element.Add(Titling(row.Job, "round-end__row-job", "round-end__ellipsis"));
            element.Add(Arcade(row.Alive ? "ALIVE" : "DEAD", "round-end__row-status",
                row.Alive ? "round-end__alive" : "round-end__dead"));
            return element;
        }
    }
}
