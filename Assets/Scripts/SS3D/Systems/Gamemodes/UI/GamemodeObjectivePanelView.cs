using System.Collections.Generic;
using SS3D.Core.Behaviours;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.GameModes.Events;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.GameModes.UI
{
    public class GamemodeObjectivePanelView : Actor
    {
        [SerializeField] private UiFade _fade;

        [SerializeField] private GamemodeObjectiveItemView _itemViewPrefab;
        [SerializeField] private GameObject _content;

        private Dictionary<int, GamemodeObjectiveItemView> _gamemodeObjectiveItems;

        protected override void OnStart()
        {
            base.OnStart();

            _gamemodeObjectiveItems = new Dictionary<int, GamemodeObjectiveItemView>();
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
    }
}