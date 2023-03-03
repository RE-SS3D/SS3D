using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using SS3D.Systems.Entities.Events;
using Coimbra.Services.Events;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Logging;

namespace SS3D.Systems.Roles
{
    public class RoleSystem : NetworkSystem
    {
        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        [Server]
        private void Setup()
        {
            AddHandle(IndividualPlayerEmbarked.AddListener(HandleIndividualPlayerEmbarked));
            AddHandle(OnlineSoulsChanged.AddListener(HandleOnlineSoulsChanged)); 
        }

        [Server]
        private void HandleOnlineSoulsChanged(ref EventContext context, in OnlineSoulsChanged e)
        {
            if (!e.AsServer)
            {
                return;
            }

            if (e.ChangeType == ChangeType.Addition)
            {
                HandlePlayerJoined(e.ChangedCkey);
            } else 
            if (e.ChangeType == ChangeType.Removal)
            {
                HandlePlayerLeft(e.ChangedCkey);
            }
        }

        [Server]
        private void HandlePlayerJoined(string ckey)
        {
            Punpun.Say(this, ckey + " has joined the server");
        }

        [Server]
        private void HandlePlayerLeft(string ckey)
        {
            Punpun.Say(this, ckey + " has left the server");
        }

        [Server]
        private void HandleIndividualPlayerEmbarked(ref EventContext context, in IndividualPlayerEmbarked e)
        {
            Punpun.Say(this, e.Player.Mind.Soul.Ckey + " has embarked");
        }
    }
}