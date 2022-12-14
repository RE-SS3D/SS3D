using System.Collections.Generic;
using Coimbra;
using Coimbra.Services.Events;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Utils;
using UnityEngine;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Gamemodes.UI
{
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

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            if (roundState == RoundState.Stopped)
            {
                ClearObjectivesList();
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            _fade.SetFade(false);
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            ProcessInput();
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _fade.SetFade(true);
            }
            if (Input.GetKeyUp(KeyCode.Tab))
            {
                _fade.SetFade(false);
            }
        }

        public void ProcessObjectiveUpdated(GamemodeObjective objective)
        {
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
            GamemodeObjectiveItemView itemView = Instantiate(_itemViewPrefab, _content.transform);
            itemView.SetActive(true);

            _gamemodeObjectiveItems.Add(objective.Id, itemView);
            itemView.UpdateObjective(objective);
        }

        private void ClearObjectivesList()
        {
            foreach (KeyValuePair<int,GamemodeObjectiveItemView> view in _gamemodeObjectiveItems)
            {
                view.Value.GameObjectCache.Destroy();
            }

            _gamemodeObjectiveItems.Clear();
        }
    }
}