using Coimbra.Services.Events;
using FishNet;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using SS3D.UI.Buttons;
using UnityEngine;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Controls a button that starts or stops a round
    /// </summary>
    public class ChangeRoundStateView : Actor
    {
        [SerializeField] private ToggleLabelButton _startRoundButton;

        protected override void OnAwake()
        {
            base.OnAwake();

            AddEventListeners();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            _startRoundButton.OnPressedDown -= HandleEmbarkButtonPress;
        }

        private void AddEventListeners()
        {
            _startRoundButton.OnPressedDown += HandleEmbarkButtonPress;

            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            switch (e.RoundState)
            {
                case RoundState.Stopped:
                    _startRoundButton.Pressed = false;
                    _startRoundButton.Disabled = false;
                    break;
                case RoundState.WarmingUp:
                case RoundState.Ongoing:
                    _startRoundButton.Pressed = true;
                    _startRoundButton.Disabled = false;
                    break;
                case RoundState.Preparing:
                case RoundState.Ending:
                    _startRoundButton.Disabled = true;
                    break;
                default:
                    Log.Error(this, $"Unhandled round state: {e.RoundState}", Logs.UI);
                    break;
            }
        }

        private void HandleEmbarkButtonPress(bool state)
        {
            string ckey = Core.Settings.LocalPlayer.Ckey;
            if (string.IsNullOrEmpty(ckey))
            {
                Log.Error(this, "Local player ckey is null or empty, cannot change round state", Logs.UI);
                return;
            }

            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();
            if (permissionSystem == null || !permissionSystem.HasLoadedPermissions)
            {
                Log.Error(this, "Permissions not loaded, cannot change round state", Logs.UI);
                return;
            }

            if (!permissionSystem.IsAtLeast(ckey, ServerRoleTypes.Administrator))
            {
                Log.Error(this, "User {ckey} lacks permission to change round state", Logs.UI, ckey);
                return;
            }

            ChangeRoundState(state);
        }

        private void ChangeRoundState(bool state)
        {
            ChangeRoundStateMessage changeRoundStateMessage = new(state);
            InstanceFinder.ClientManager.Broadcast(changeRoundStateMessage);
        }
    }
}