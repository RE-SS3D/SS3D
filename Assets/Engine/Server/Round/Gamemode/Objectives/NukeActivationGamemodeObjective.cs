using System;
using SS3D.Content;
using SS3D.Content.Furniture.Special;
using UnityEngine;

namespace SS3D.Engine.Server.Round
{
    [CreateAssetMenu(fileName = "GamemodeObjective", menuName = "Gamemode/Nuke/Nuke activation objective", order = 1)]
    public class NukeActivationGamemodeObjective : GamemodeObjective
    {
        private void Awake()
        {
            Nuke.NukeStateChanged += (entity, state) => UpdateCompletionStatus(entity, state);
        }

        public virtual void UpdateCompletionStatus(Entity entity, bool state)
        {
            if (entity == owner)
            {
                completed = true;
                Debug.Log("Nuke objective complete!");
            }
        }
    }
}