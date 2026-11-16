using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Interactions;
using SS3D.Systems.Selection;
using UnityEngine;

/// <summary>
/// Put this script on things that can be dragged by a player, such as unbolted furnitures.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class Draggable : NetworkActor, IInteractionTarget, IGameObjectProvider
{
    [SerializeField]
    private Transform _dragger;

    private Vector3 _draggerToDragged;

    [SyncVar]
    private bool _dragged;

    public bool Dragged => _dragged;

    /// <summary>
    /// How far (horizontally) the server lets a client's body be from this object when it starts a drag. Generous,
    /// because the object's pivot can sit well past the edge the player clicked, and the server's copy of the body lags.
    /// </summary>
    public const float MaxDragStartDistance = 3f;

    public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        if (!_dragged && IsLocked)
            return System.Array.Empty<IInteraction>();

        DragInteraction dragInteraction = new DragInteraction();
        return new IInteraction[] { dragInteraction };
    }

    /// <summary>True when a sibling <see cref="IDragLock"/> forbids starting a drag.</summary>
    public bool IsLocked => TryGetComponent(out IDragLock dragLock) && dragLock.DragLocked;

    protected override void OnStart()
    {
        AddHandle(UpdateEvent.AddListener(HandleUpdate));
    }

    private void HandleUpdate(ref EventContext context, in UpdateEvent e)
    {
        if (!IsServer || !_dragged || _dragger == null)
            return;

        transform.position =
            new Vector3(_dragger.position.x, transform.position.y, _dragger.position.z) + _draggerToDragged;
    }

    public void SetDrag(bool drag, Transform dragger)
    {
        if (IsServer)
        {
            if (drag && !_dragged && IsLocked)
                return;

            ApplyDrag(drag, dragger);
            return;
        }

        CmdSetDrag(drag);
    }

    /// <summary>
    /// The dragger is always the caller's own body, resolved on the server (networking §2). Starting needs that body
    /// within <see cref="MaxDragStartDistance"/>; only the body dragging the object may let go of it.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void CmdSetDrag(bool drag, NetworkConnection conn = null)
    {
        if (!SubSystems.TryGet(out EntitySubSystem entities) || !entities.TryGetOwnedEntity(conn, out Entity entity))
            return;

        if (!MayChangeDrag(drag, entity.transform, _dragged ? _dragger : null, transform.position))
            return;

        if (drag && !_dragged && IsLocked)
            return;

        ApplyDrag(drag, entity.transform);
    }

    /// <summary>Server rule for a client's drag request; pure, so EditMode can test it.</summary>
    public static bool MayChangeDrag(bool drag, Transform caller, Transform currentDragger, Vector3 draggedPosition)
    {
        if (caller == null)
            return false;

        if (!drag)
            return currentDragger == null || currentDragger == caller;

        if (currentDragger != null && currentDragger != caller)
            return false;

        Vector3 offset = draggedPosition - caller.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= MaxDragStartDistance * MaxDragStartDistance;
    }

    private void ApplyDrag(bool drag, Transform dragger)
    {
        _dragged = drag;
        _dragger = dragger;

        if (_dragger == null)
            return;

        _draggerToDragged = new Vector3(
            transform.position.x - _dragger.position.x,
            0f,
            transform.position.z - _dragger.position.z);
    }
}
