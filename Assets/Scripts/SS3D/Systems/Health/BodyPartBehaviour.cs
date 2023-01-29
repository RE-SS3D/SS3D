using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;



/// <summary>
/// Class to handle all networking stuff related to a body part, there should be only one on a given game object.
/// There should always be a network object component everywhere this component is.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class BodyPartBehaviour : NetworkBehaviour
{


    [SerializeField] 
    private bool isHuman;

    [SerializeField]
    [Tooltip("Add all parent body part directly connected to this one (e.g. head for the neck of humans)")]
    private List<BodyPartBehaviour> InitialConnectedParentBodyPartsBehaviour;

    [SerializeField]
    [Tooltip("Add all child body part directly connected to this one (e.g. neck for the head of humans)")]
    private List<BodyPartBehaviour> InitialConnectedChildBodyPartsBehaviour;

    [SerializeField]
    private List<BodyPartBehaviour> InitialConnectedParentNerveSignalTransmittersBehaviour;

    [SerializeField]
    private List<BodyPartBehaviour> InitialConnectedChildNerveSignalTransmittersBehaviour;

    [SyncObject]
    private readonly SyncList<BodyPartBehaviour> ParentConnectedBodyPartsBehaviour;

    [SyncObject]
    private readonly SyncList<BodyPartBehaviour> ChildConnectedBodyPartsBehaviour;


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

        foreach (var bodyPart in InitialConnectedParentBodyPartsBehaviour)
        {
            //ObserverRpcAddConnectedBodyPart(bodyPart.NetworkObject, false);
            ParentConnectedBodyPartsBehaviour.Add(bodyPart);
        }

        foreach (var bodyPart in InitialConnectedChildBodyPartsBehaviour)
        {
            //ObserverRpcAddConnectedBodyPart(bodyPart.NetworkObject, true);
            ChildConnectedBodyPartsBehaviour.Add(bodyPart);
        }

    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        /*
        foreach (var connectedBodyPart in InitialConnectedParentNerveSignalTransmittersBehaviour)
        {
            var transmitterBodyLayer = BodyPart.GetBodyLayer<INerveSignalTransmitter>();

            var connectedBodyLayer = connectedBodyPart.BodyPart.GetBodyLayer<INerveSignalTransmitter>();
            if (connectedBodyLayer != null && transmitterBodyLayer != null)
            {
                ((INerveSignalTransmitter)transmitterBodyLayer).
                    AddNerveSignalTransmitter((INerveSignalTransmitter)connectedBodyLayer, false);
            }
        }

        foreach (var connectedBodyPart in InitialConnectedChildNerveSignalTransmittersBehaviour)
        {
            var transmitterBodyLayer = BodyPart.GetBodyLayer<INerveSignalTransmitter>();

            var connectedBodyLayer = connectedBodyPart.BodyPart.GetBodyLayer<INerveSignalTransmitter>();
            if (connectedBodyLayer != null && transmitterBodyLayer != null)
            {
                ((INerveSignalTransmitter)transmitterBodyLayer).
                    AddNerveSignalTransmitter((INerveSignalTransmitter)connectedBodyLayer, true);
            }
        }*/

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
    public void ServerRpcAddConnectedBodyPart(NetworkConnection conn, NetworkObject bodyPart, bool isChild)
    {
        PermissionSystem permissionSystem = SystemLocator.Get<PermissionSystem>();
        if (!permissionSystem.HasAdminPermission(conn))
        {
            return;
        }
        ObserverRpcAddConnectedBodyPart(bodyPart, isChild);
    }

    [ObserversRpc(RunLocally = true)]
    public void ObserverRpcAddConnectedBodyPart(NetworkObject bodyPart, bool isChild)
    {
        BodyPart.AddConnectedBodyPart(bodyPart.GetComponent<BodyPartBehaviour>().BodyPart, isChild);
    }


    //TODO remove that shit before any merge
    [ServerRpc]
    public void ServerRpcAddNerveSignalTransmitter(INerveSignalTransmitter transmitter,
    INerveSignalTransmitter transmitterToAdd, bool isChild)
    {
        ObserverRpcAddNerveSignalTransmitter(transmitter, transmitterToAdd, isChild);
    }

    [ObserversRpc]
    public void ObserverRpcAddNerveSignalTransmitter(INerveSignalTransmitter transmitter,
        INerveSignalTransmitter transmitterToAdd, bool isChild)
    {
        transmitter.AddNerveSignalTransmitter(transmitterToAdd, isChild);
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
}
