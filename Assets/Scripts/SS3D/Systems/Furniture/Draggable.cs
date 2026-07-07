using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
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
        if (!IsServer || !_dragged || _dragger == null)
            return;

        transform.position =
            new Vector3(_dragger.position.x, transform.position.y, _dragger.position.z) + _draggerToDragged;
    }

    public void SetDrag(bool drag, Transform dragger)
    {
        if (IsServer)
        {
            ApplyDrag(drag, dragger);
            return;
        }

        NetworkObject draggerObject = dragger != null
            ? dragger.GetComponentInParent<NetworkObject>()
            : null;
        SetDragServer(drag, draggerObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetDragServer(bool drag, NetworkObject draggerObject)
    {
        ApplyDrag(drag, draggerObject != null ? draggerObject.transform : null);
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
