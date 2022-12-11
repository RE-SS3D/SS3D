using System.Collections.Generic;
using SS3D.Core.Behaviours;
using SS3D.Systems.Gamemodes;
using UnityEngine;

namespace SS3D.Systems.GameModes.UI
{
    public class GamemodeObjectivePanelView : Actor
    {
        private GamemodeObjectiveItemView _itemViewPrefab;
        private GameObject _content;

        private Dictionary<int, GamemodeObjectiveItemView> _gamemodeObjectiveItems;

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

            _gamemodeObjectiveItems.Add(objective.Id, itemView);
            itemView.UpdateObjective(objective);
        }
    }
}