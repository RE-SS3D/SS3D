using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using Cysharp.Threading.Tasks;
using UnityEditor;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Health;
using System.Linq;

/// <summary>
/// Class to handle all networking stuff related to a body part, there should be only one on a given game object.
/// There should always be a network object component everywhere this component is.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class BodyPartBehaviour : InteractionTargetNetworkBehaviour
{


    [SerializeField] 
    private bool isHuman;

    [SyncVar]
    private BodyPartBehaviour _parentBodyPartBehaviour;

    [SyncObject, SerializeField]
    private readonly SyncList<BodyPartBehaviour> _childBodyPartsBehaviour = new SyncList<BodyPartBehaviour>();

    /// <summary>
    /// The parent bodypart is the body part attached to this body part, closest from the brain. 
    /// For lower left arm, it's higher left arm. For neck, it's head.
    /// Be careful, it doesn't necessarily match the game object hierarchy
    /// </summary>
    public BodyPartBehaviour ParentBodyPartBehaviour => _parentBodyPartBehaviour;

    /// <summary>
    /// The parent bodypart is the body part attached to this body part, furthest from the brain. 
    /// For higer left arm, it's lower left arm. For head, it's neck.
    /// </summary>
    public List<BodyPartBehaviour> ChildBodyPartsBehaviour => (List<BodyPartBehaviour>) _childBodyPartsBehaviour.Collection;

    public BodyPart BodyPart;

    public void Awake()
    {
        if (isHuman)
        {
            BodyPart = new HumanBodypart(this);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        SetUpChild();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }


    [Server]
    private void SetUpChild()
    {   
        if(_parentBodyPartBehaviour != null)
        {
            _parentBodyPartBehaviour._childBodyPartsBehaviour.Add(this);
        }
    }

    [Server]
    public void AddBodyLayer(BodyLayer layer)
    {
        BodyPart.AddBodyLayer(layer);
    }

    [Server]
    public void RemoveBodyLayer(BodyLayer layer)
    {
        BodyPart.RemoveBodyLayer(layer);
    }

    [Server]
    public void InflictDamage(DamageTypeQuantity damageTypeQuantity)
    {
        BodyPart.InflictDamage(damageTypeQuantity);
    }

    [Server]
    public bool ContainsLayer(BodyLayerType layerType)
    {
        return BodyPart.ContainsLayer(layerType);
    }

    
    [ServerRpc]
    public void ServerRpcAddChildBodyPart(NetworkConnection conn, NetworkObject bodyPart)
    {
        PermissionSystem permissionSystem = SystemLocator.Get<PermissionSystem>();
        if (!permissionSystem.HasAdminPermission(conn))
        {
            return;
        }
        ObserverRpcAddChildBodyPart(bodyPart);
    }

    [ObserversRpc(RunLocally = true)]
    public void ObserverRpcAddChildBodyPart(NetworkObject bodyPart)
    {
        BodyPart.AddChildBodyPart(bodyPart.GetComponent<BodyPartBehaviour>().BodyPart);
    }

    [ServerRpc]
    public void ServerRpcAddNerveLayer(NerveLayer nerveLayer)
    {
        ObserversRpcAddNerveLayer(nerveLayer);
    }

    [ObserversRpc(RunLocally = true)]
    public void ObserversRpcAddNerveLayer(NerveLayer nerveLayer)
    {
       BodyPart.AddBodyLayer(nerveLayer);
    }



    /// <summary>
    /// The body part is not destroyed, it's simply detached from the entity.
    /// </summary>
    public void DetachBodyPart()
    {
        //Spawn a detached body part from the entity, and destroy this one with all childs.
        // Maybe better in body part controller.
        //throw new NotImplementedException();
        _parentBodyPartBehaviour.DetachBodyPart();
        Despawn();
    }

    /// <summary>
    /// The body part took so much damages that it's simply destroyed.
    /// Think complete crushing, burning to dust kind of stuff.
    /// All child body parts are detached.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void DestroyBodyPart()
    {
        // destroy this body part with all childs on the entity, detach all childs.
        // Maybe better in body part controller.
        //throw new NotImplementedException();
    }


    [Server]
    public string DescribeBodyPart()
    {
        return BodyPart.Describe();
    }

    public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        return new IInteraction[] { new KillInteraction() };
    }


}
