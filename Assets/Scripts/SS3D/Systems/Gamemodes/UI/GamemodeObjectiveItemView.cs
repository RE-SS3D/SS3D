using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Gamemodes;
using SS3D.Utils;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.UI
{
    public class GamemodeObjectiveItemView : Actor
    {
        [SerializeField]
        private GamemodeObjective _objective;

        [SerializeField]
        private TMP_Text _text;

        public void UpdateObjective(GamemodeObjective objective)
        {
            if (_objective == null)
            {
                _objective = objective;
            }

            if (objective.Id != _objective.Id)
            {
                return;
            }

            string type = objective.GetType().ToString();

            string status = objective.Status == ObjectiveStatus.Success ? "<sprite name=\"approve\">" : "<sprite name=\"deny\">";
            _text.SetText($"{objective.Id} - {objective.Title} {status}");

            switch (objective.Status)
            {
                case ObjectiveStatus.Success:
                {
                    _text.color = Color.green;
                    break;
                }

                case ObjectiveStatus.Failed:
                {
                    _text.color = Color.red;
                    break;
                }

                case ObjectiveStatus.Cancelled:
                {
                    _text.color = Color.gray;
                    break;
                }

                case ObjectiveStatus.InProgress:
                {
                    _text.color = Color.yellow;
                    break;
                }
            }
        }
    }
}