using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Screens;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Little script to position correctly the audio listener to be on the current player camera target.
    /// </summary>
    public class ListenerPosition : Actor
    {
        [SerializeField]
        private AudioListener _listener;

        [SerializeField]
        private GameObject _listenerTarget;

        protected override void OnAwake()
        {
            AddHandle(LocalPlayerObjectChanged.AddListener(HandlePlayerObjectChanged));
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            _listenerTarget = e.PlayerObject;
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if(_listenerTarget != null)
            {
                _listener.transform.position = _listenerTarget.transform.position;
            }
        }
    }
}
