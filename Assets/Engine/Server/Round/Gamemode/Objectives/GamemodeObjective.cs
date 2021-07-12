using SS3D.Content;
using UnityEngine;

namespace SS3D.Engine.Server.Round
{
    public class GamemodeObjective : ScriptableObject, IGamemodeObjectiveHandler
    {
        public Entity owner;
        public bool completed;

        public virtual bool ValidateObjectiveCompletion()
        {
            return completed;
        }

        public virtual Entity TryToAssignObjectiveOwner(Entity owner)
        {
            // TODO: Make the checks
            this.owner = owner;
            return owner;
        }

        public virtual Entity ForceAssignObjectiveOwner(Entity owner)
        {
            this.owner = owner;
            return owner;
        }

        public virtual void UpdateCompletionStatus(Entity entity, bool state)
        {
            if (entity == owner)
            {
                completed = state;
                Debug.Log("GamemodeObjective: Objective completed!");
            }
        }
    }
}