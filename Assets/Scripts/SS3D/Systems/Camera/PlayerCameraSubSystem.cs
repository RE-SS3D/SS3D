using Coimbra.Services.Events;
using DG.Tweening;
using SS3D.Logging;
using SS3D.Systems.Camera.Events;
using SS3D.Systems.Entities.Events;
using UnityEngine;

namespace SS3D.Systems.Camera
{
    /// <summary>
    /// Sets the camera follow for the local player.
    /// </summary>
    public class PlayerCameraSubSystem : Core.Behaviours.SubSystem
    {
        [SerializeField]
        private CameraActor _camera;

        [SerializeField]
        private CameraFollow _cameraFollow;

        private Sequence _fovSequence;

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(LocalPlayerObjectChanged.AddListener(HandlePlayerObjectChanged));
        }

        /// <summary>
        /// Called when the player object is changed.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="e"></param>
        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            GameObject target = e.PlayerObject;

            _fovSequence?.Kill();
            _fovSequence = DOTween.Sequence();

            _fovSequence.Append(_camera.Camera.DOFieldOfView(75, 0.1f));
            _fovSequence.Append(_camera.Camera.DOFieldOfView(65, .7F));

            Log.Information(this, "setting new camera target {gameObject}", Logs.Generic, target.name);
            _cameraFollow.SetTarget(target);

            new CameraTargetChanged(GameObject).Invoke(this);
        }
    }
}
