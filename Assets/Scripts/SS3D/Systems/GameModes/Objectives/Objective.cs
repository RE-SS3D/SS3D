using System.Collections;
using SS3D.Systems.GameModes.GameEvents;
using UnityEngine;

namespace SS3D.Systems.GameModes.Objectives
{
    enum ObjectiveStatus
    {
        InProgress,
        Success,
        Failed
    }

    [CreateAssetMenu(fileName = "Objective", menuName = "GameModes/Objective", order = 1)]
    public class Objective : ScriptableObject
    {
        [SerializeField] private string Title;
        public GameEvent SuccessEvent;
        public GameEvent FailEvent;
        private ObjectiveStatus Status;

        public string GetTitle()
        {
            return Title;
        }

        public bool Done()
        {
            return (Status != ObjectiveStatus.InProgress);
        }

        public void Success()
        {
            Status = ObjectiveStatus.Success;
        }

        public void Fail()
        {
            Status = ObjectiveStatus.Failed;
        }

        private void OnEnable()
        {
            SuccessEvent?.RegisterListener(this);
            FailEvent?.RegisterListener(this);
        }

        private void OnDisable()
        {
            SuccessEvent?.UnregisterListener(this);
            FailEvent?.UnregisterListener(this);
        }

        public void OnGameEvent(Hashtable gameEventData)
        {
            if (object.ReferenceEquals(gameEventData[GameEventDataType.GameEvent], SuccessEvent))
            {
                Debug.Log("Success");
                Success();
            } else
            if (object.ReferenceEquals(gameEventData[GameEventDataType.GameEvent], FailEvent))
            {
                Debug.Log("Failed");
                Fail();
            }
        }
    }
}
