using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using System.Linq;

public class BodyPartBehaviour : NetworkBehaviour
{


    [SerializeField] 
    private bool isHuman;

    [SerializeField]
    private List<BodyPartBehaviour> InitialConnectedParentBodyPartsBehaviour;

    [SerializeField]
    private List<BodyPartBehaviour> InitialConnectedChildBodyPartsBehaviour;

    [SerializeField]
    private List<BodyPartBehaviour> InitialConnectedParentNerveSignalTransmittersBehaviour;

    [SerializeField]
    private List<BodyPartBehaviour> InitialConnectedChildNerveSignalTransmittersBehaviour;

    private BodyPart BodyPart;

    public void Awake()
    {
        if (isHuman)
        {
            BodyPart = new HumanBodypart(this);
        }
    }

    public void Start()
    {
        foreach (var bodyPart in InitialConnectedParentBodyPartsBehaviour)
        {
            BodyPart.AddConnectedBodyPart(bodyPart.BodyPart, false);
        }

        foreach (var bodyPart in InitialConnectedChildBodyPartsBehaviour)
        {
            BodyPart.AddConnectedBodyPart(bodyPart.BodyPart, true);
        }

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
}
