﻿using Coimbra.Services.Events;
using DG.Tweening;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Screens.Events;
using UnityEngine;
using SS3D.Core;

namespace SS3D.Systems.Screens
{
    /// <summary>
    /// Sets the camera follow for the local player.
    /// </summary>
    public class PlayerCameraSystem  : Core.Behaviours.System
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private CameraFollow _cameraFollow;

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
            GameObject target = e.Target;

            _fovSequence?.Kill();
            _fovSequence = DOTween.Sequence();

            _fovSequence.Append(_camera.DOFieldOfView(75, 0.1f));
            _fovSequence.Append(_camera.DOFieldOfView(65, .7F));

            Punpun.Say(this, $"setting new camera target {target.name}");
            _cameraFollow.SetTarget(target);

            new CameraTargetChanged(GameObjectCache).Invoke(this);
        }
    }
}