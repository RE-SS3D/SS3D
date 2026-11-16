using Coimbra.Services.Events;
using DG.Tweening;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Screens.Events;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Screens
{
    /// <summary>
    /// Sets the camera follow for the local player.
    /// </summary>
    public class PlayerCameraSubSystem : SubSystem
    {
        [SerializeField]
        private Camera _camera;
        [SerializeField]
        private CameraFollow _cameraFollow;
        [SerializeField]
        private Volume _volume;
        [SerializeField]
        private VolumeProfile _lobbyVolumeProfile;
        [SerializeField]
        private VolumeProfile _gameplayVolumeProfile;

        private Sequence _fovSequence;

        protected override void OnAwake()
        {
            base.OnAwake();

            if (_volume == null)
            {
                TryGetComponent(out _volume);
            }

            ApplyVolumeProfileForCurrentRoundState();
            AddHandle(LocalPlayerObjectChanged.AddListener(HandlePlayerObjectChanged));
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
        }

        /// <summary>
        /// Called when the player object is changed.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="e"></param>
        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            GameObject target = e.PlayerObject;

#if !UNITY_SERVER
            _fovSequence?.Kill();
            _fovSequence = DOTween.Sequence();

            _fovSequence.Append(_camera.DOFieldOfView(75, 0.1f));
            _fovSequence.Append(_camera.DOFieldOfView(65, .7F));
#endif

            Log.Information(this, "setting new camera target {gameObject}", Logs.Generic, target.name);
            _cameraFollow.SetTarget(target);
            ApplyGameplayVolumeProfile();

            new CameraTargetChanged(GameObject).Invoke(this);
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            ApplyVolumeProfile(e.RoundState);
        }

        private void ApplyVolumeProfileForCurrentRoundState()
        {
            if (SubSystems.TryGet(out RoundSubSystem roundSubSystem))
            {
                ApplyVolumeProfile(roundSubSystem.CurrentRoundState);
                return;
            }

            ApplyLobbyVolumeProfile();
        }

        private void ApplyVolumeProfile(RoundState roundState)
        {
            if (IsGameplayRoundState(roundState))
            {
                ApplyGameplayVolumeProfile();
            }
            else
            {
                ApplyLobbyVolumeProfile();
            }
        }

        private static bool IsGameplayRoundState(RoundState roundState)
        {
            return roundState is RoundState.Preparing or RoundState.WarmingUp or RoundState.Ongoing;
        }

        private void ApplyLobbyVolumeProfile()
        {
            if (_volume == null || _lobbyVolumeProfile == null)
            {
                return;
            }

            _volume.sharedProfile = _lobbyVolumeProfile;
        }

        private void ApplyGameplayVolumeProfile()
        {
            if (_volume == null || _gameplayVolumeProfile == null)
            {
                return;
            }

            _volume.sharedProfile = _gameplayVolumeProfile;
        }
    }
}