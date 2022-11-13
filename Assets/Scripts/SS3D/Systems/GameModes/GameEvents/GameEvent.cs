using System.Collections;
using System.Collections.Generic;
using SS3D.Systems.GameModes.Objectives;
using UnityEngine;


namespace SS3D.Systems.GameModes.GameEvents
{
    enum GameEventDataType
    {
        InteractionType,
        InteractionSource,
        InteractionTarget,
        CulpritInfo,
        Timestamp,
        GameEvent
    }

    [CreateAssetMenu(fileName = "GameEvent", menuName = "GameModes/GameEvent", order = 2), System.Serializable]
    public class GameEvent : ScriptableObject
    {
        private readonly List<Objective> listeners = new List<Objective>();

        public virtual void Raise(Hashtable data)
        {
            Debug.Log("Event Called " + this.name);
            // Adds information about this GameEvent to the data
            data.Add(GameEventDataType.GameEvent, this);

            // Calls the responses on all listeners, the last registered listeners are called first
            for (int i = listeners.Count - 1; i > 0; i--)
            {
                listeners[i].OnGameEvent(data);
            }
        }

        public virtual void RegisterListener(Objective listener) => listeners.Add(listener);

        public virtual void UnregisterListener(Objective listener) => listeners.Remove(listener);
    }
}