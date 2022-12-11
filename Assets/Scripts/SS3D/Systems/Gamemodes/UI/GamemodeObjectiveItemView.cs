using SS3D.Core.Behaviours;
using SS3D.Systems.Gamemodes;
using TMPro;

namespace SS3D.Systems.GameModes.UI
{
    public class GamemodeObjectiveItemView : Actor
    {
        public GamemodeObjective Objective;
        public TMP_Text _text;

        public void UpdateObjective(GamemodeObjective objective)
        {
            if (Objective == null)
            {
                Objective = objective;
            }

            if (objective.Id != Objective.Id)
            {
                return;
            }

            _text.SetText($"{objective.Id} - {objective.Status} - {objective.Title}");
        }
    }
}