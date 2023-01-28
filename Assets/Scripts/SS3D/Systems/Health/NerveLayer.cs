using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FishNet.Object;

public class NerveLayer : BiologicalLayer, INerveSignalTransmitter
{
    
    public override float OxygenConsumptionRate { get => 0.2f; }

    [SerializeField]
    protected List<INerveSignalTransmitter> ConnectedParentNerveSignalTransmitters;

    [SerializeField]
    protected List<INerveSignalTransmitter> ConnectedChildNerveSignalTransmitters;

    public NerveSignalTransmitterEnum TransmitterId
    {
        get => NerveSignalTransmitterEnum.Nerve;
        set
        {
            TransmitterId = value;
        }
    }

    public NetworkObject getNetworkedObject
    {
        get
        {
            return BodyPart.BodyPartBehaviour.NetworkObject;
        }
        set
        {

        }
    }

    public GameObject getGameObject
    {
        get
        {
            return BodyPart.BodyPartBehaviour.gameObject;
        }
        set
        {

        }
    }

    private bool _isconnectedToCentralNervousSystem;

    public bool IsConnectedToCentralNervousSystem{
        get { return _isconnectedToCentralNervousSystem; }
        set
        {
            if(IsConnectedToCentralNervousSystem == value) return;
            IsConnectedToCentralNervousSystem = value;
            // add syncing stuff
        } 
    }

    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Nerve; }
        protected set { LayerType = value; }
    }

    public NerveLayer(BodyPart bodyPart) : base(bodyPart)
    {
        ConnectedParentNerveSignalTransmitters = new List<INerveSignalTransmitter>();
        ConnectedChildNerveSignalTransmitters = new List<INerveSignalTransmitter>();

        IsConnectedToCentralNervousSystem = false;
    }

    protected override void SetSuceptibilities()
    {
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Slash, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Pressure, 0.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Shock, 2f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Rad, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Toxic, 1.2f));
    }


    /// <summary>
    /// Disconnect all child nerve signal transmitters as well as this from the CNS.
    /// </summary>
    public void DisconnectFromCentralNervousSystem()
    {
        foreach (INerveSignalTransmitter transmitter in ConnectedChildNerveSignalTransmitters)
        {
            transmitter.DisconnectFromCentralNervousSystem();
        }
        IsConnectedToCentralNervousSystem = false;
    }

    public void RemoveAllNerveSignalTransmitter()
    {
        DisconnectFromCentralNervousSystem();
        foreach (INerveSignalTransmitter transmitter in ConnectedChildNerveSignalTransmitters)
        {
            transmitter.RemoveNerveSignalTransmitter(this);
        }
        foreach (INerveSignalTransmitter transmitter in ConnectedParentNerveSignalTransmitters)
        {
            transmitter.RemoveNerveSignalTransmitter(this);
        }
        ConnectedParentNerveSignalTransmitters.Clear();
        ConnectedChildNerveSignalTransmitters.Clear();
    }

    public List<INerveSignalTransmitter> ParentConnectedSignalTransmitters()
    {
        return ConnectedParentNerveSignalTransmitters;
    }

    public List<INerveSignalTransmitter> ChildConnectedSignalTransmitters()
    {
        return ConnectedParentNerveSignalTransmitters;
    }

    public void RemoveNerveSignalTransmitter(INerveSignalTransmitter transmitter)
    {
        if (transmitter == null) { return; }
        ConnectedChildNerveSignalTransmitters.Remove(transmitter);
        ConnectedParentNerveSignalTransmitters.Remove(transmitter);
        transmitter.RemoveNerveSignalTransmitter(this);
    }

    public void AddNerveSignalTransmitter(INerveSignalTransmitter transmitter, bool isChild)
    {
        if (transmitter == null) { return; }
        if(AlreadyAdded(transmitter)) { return; }

        if (isChild)
        {
            ConnectedChildNerveSignalTransmitters.Add(transmitter);
            if (transmitter.IsConnectedToCentralNervousSystem)
            {
                IsConnectedToCentralNervousSystem = true;
            }
        }
        else
        {
            ConnectedParentNerveSignalTransmitters.Add(transmitter);
        }

        transmitter.AddNerveSignalTransmitter(this, !isChild);

        if(BodyPart.BodyPartBehaviour != null)
        {
            if(BodyPart.BodyPartBehaviour.IsOwner)
                BodyPart.BodyPartBehaviour.ServerRpcAddNerveSignalTransmitter(this, transmitter, isChild);
        }
    }

    public bool AlreadyAdded(INerveSignalTransmitter transmitter)
    {
        return ConnectedChildNerveSignalTransmitters.Contains(transmitter)
            || ConnectedParentNerveSignalTransmitters.Contains(transmitter);
    }

    /// <summary>
    /// Produces a given amount of pain depending on other tissues damages.
    /// It also depends on the state of the never layer.
    /// When nerves are closed to be destroyed, they send less and less pain.
    /// </summary>
    public float ProducePain()
    {
        throw new NotImplementedException();
    }

    public override void OnDamageInflicted(DamageTypeQuantity damageQuantity)
    {
        base.OnDamageInflicted(damageQuantity);
       RemoveAllNerveSignalTransmitter();
    }
}
