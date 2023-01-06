using Coimbra.Services.Events;
using SS3D.Systems.GameModes.Events;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives
{
    /// <summary>
    /// Objective which the goal is to detonate the nuke. 
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/Objectives/DetonateNuke", fileName = "DetonateNuke")]
    public class DetonateNukeObjective : GamemodeObjective
    {
        /// <inheritdoc />
        public override void InitializeObjective()
        {
            base.InitializeObjective();
        }

        /// <inheritdoc />
        public override void AddEventListeners()
        {
            NukeDetonateEvent.AddListener(HandleNukeDetonateEvent);
        }

        /// <inheritdoc />
        public override void FinalizeObjective()
        {
            Succeed();
        }

        public override void CheckCompletion()
        {
            
        }

        private void HandleNukeDetonateEvent(ref EventContext context, in NukeDetonateEvent e)
        {

            if (e.Author == AssigneeCkey)
            {
                FinalizeObjective();
            }
        }
    }
}