using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Interactions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Draggable : NetworkActor, IInteractionTarget, IGameObjectProvider
{
    /// <summary>
    /// The thing that drag this object
    /// </summary>
    [SerializeField]
    private Transform _dragger;

    private FixedJoint joint;

    private Vector3 _draggerToDragged;

    /// <summary>
    /// True if the object is currently being dragged.
    /// </summary>
    [SyncVar]
    private bool _dragged;

    public bool Dragged => _dragged;

    public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        DragInteraction dragInteraction = new DragInteraction();

        return new IInteraction[] { dragInteraction };
    }

    protected override void OnStart()
    {
        AddHandle(UpdateEvent.AddListener(HandleUpdate));
    }

    private void HandleUpdate(ref EventContext context, in UpdateEvent e)
    {

        if (!_dragged) return;

        gameObject.transform.position =  new Vector3(_dragger.transform.position.x, transform.position.y, _dragger.transform.position.z) + _draggerToDragged;

        
    }

    public void SetDrag(bool drag, Transform dragger)
    {
        _dragged= drag;
        _dragger = dragger;
        _draggerToDragged = new Vector3(transform.position.x - _dragger.transform.position.x, 0f, transform.position.z - _dragger.transform.position.z);

        
    }
}
