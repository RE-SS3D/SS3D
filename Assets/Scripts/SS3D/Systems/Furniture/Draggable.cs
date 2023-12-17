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

    /// <summary>
    /// The distance at which the dragged item should stay from dragger
    /// </summary>
    [SerializeField]
    private float _distanceFromDragger = 1f;

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

        gameObject.transform.position = new Vector3(_dragger.transform.position.x, transform.position.y, _dragger.transform.position.z)
            + _dragger.transform.forward * _distanceFromDragger;
    }

    public void SetDrag(bool drag, Transform dragger)
    {
        _dragged= drag;
        _dragger = dragger;
    }
}
