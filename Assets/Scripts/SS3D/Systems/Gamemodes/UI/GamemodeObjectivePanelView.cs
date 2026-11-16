using System.Collections.Generic;
using Coimbra;
using Coimbra.Services.Events;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Utils;
using UnityEngine;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Gamemodes.UI
{
    /// <summary>
    /// Legacy uGUI objectives panel — kept for data wiring but hidden on screen.
    /// Design surface is the PDA objectives tab (objectives.md §8).
    /// </summary>
    public class GamemodeObjectivePanelView : Actor
    {
        [SerializeField] private UiFade _fade;

        [SerializeField] private GamemodeObjectiveItemView _itemViewPrefab;
        [SerializeField] private GameObject _content;

        private Dictionary<int, GamemodeObjectiveItemView> _gamemodeObjectiveItems;

        protected override void OnAwake()
        {
            base.OnAwake();

            _gamemodeObjectiveItems = new Dictionary<int, GamemodeObjectiveItemView>();

            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Hidden until the PDA objectives tab ships — no hold-to-show hotkey revival.
            if (_fade != null)
            {
                _fade.SetFade(false);
            }

            if (_content != null)
            {
                _content.SetActive(false);
            }
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            if (roundState == RoundState.Stopped)
            {
                ClearObjectivesList();
            }
        }

        public void ProcessObjectiveUpdated(GamemodeObjective objective)
        {
            // Panel is disabled; still accept updates so state is ready when PDA tab lands.
            bool hasValue = _gamemodeObjectiveItems.TryGetValue(objective.Id, out GamemodeObjectiveItemView view);

            if (hasValue)
            {
                view.UpdateObjective(objective);
            }
            else
            {
                CreateItemView(objective);
            }
        }

        private void CreateItemView(GamemodeObjective objective)
        {
            if (_itemViewPrefab == null || _content == null)
            {
                return;
            }

            GamemodeObjectiveItemView itemView = Instantiate(_itemViewPrefab, _content.transform);
            itemView.SetActive(false);

            _gamemodeObjectiveItems.Add(objective.Id, itemView);
            itemView.UpdateObjective(objective);
        }

        private void ClearObjectivesList()
        {
            foreach (KeyValuePair<int, GamemodeObjectiveItemView> view in _gamemodeObjectiveItems)
            {
                view.Value.GameObject.Dispose(true);
            }

            _gamemodeObjectiveItems.Clear();
        }
    }
}
