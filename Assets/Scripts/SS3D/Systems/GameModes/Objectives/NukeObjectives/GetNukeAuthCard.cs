using Coimbra.Services.Events;
using SS3D.Systems.GameModes.Events;
using System;
using UnityEngine;

namespace SS3D.Systems.GameModes.Objectives
{
    public class GetNukeAuthCard : IObjective
    {
        public string Title { get; set; }
        public ObjectiveStatus Status { get; set; }
        public IEvent SuccessEvent { get; set; }
        public IEvent FailEvent { get; set; }

        public void InitializeObjective()
        {
            ItemPickedUpEvent.AddListener(HandleItemPickedUpEvent);
        }

        private void HandleItemPickedUpEvent(ref EventContext context, in ItemPickedUpEvent itemPickedUpEvent)
        {
            string ownerName = itemPickedUpEvent.OwnerName;
            string itemName = itemPickedUpEvent.ItemName;

            Debug.Log("HandleItemPickedUpEvent with values: " + ownerName + "/" + itemName);
        }

        public bool Fail()
        {
            return false;
        }

        public bool Success()
        {
            throw new System.NotImplementedException();
        }
    }
}
