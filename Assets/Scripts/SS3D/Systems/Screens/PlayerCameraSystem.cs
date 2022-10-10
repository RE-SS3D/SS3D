using Coimbra.Services.Events;
using DG.Tweening;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Screens.Events;
using UnityEngine;

namespace SS3D.Systems.Screens
{
    public class PlayerCameraSystem  : SpessSystem
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private CameraFollow _cameraFollow;

        protected override void OnStart()
        {
            base.OnStart();

            ChangeCameraEvent.AddListener(HandleChangeCameraEvent);
        }

        private void HandleChangeCameraEvent(ref EventContext context, in ChangeCameraEvent e)
        {
            GameObject target = e.Target;

            _camera.DOFieldOfView(75, 0);
            _camera.DOFieldOfView(65, .7F);

            Punpun.Say(this, $"setting new camera target {target.name}", Logs.Generic);
            _cameraFollow.SetTarget(target);
        }
    }
}