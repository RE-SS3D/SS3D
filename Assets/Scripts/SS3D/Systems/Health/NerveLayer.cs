using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FishNet.Object;
using System.Linq;

public class NerveLayer : BiologicalLayer, INerveSignalTransmitter
{
    
    public override float OxygenConsumptionRate { get => 0.2f; }

    public bool IsCentralNervousSystem { get; private set; }

    public NerveSignalTransmitterType TransmitterId
    {
        get => NerveSignalTransmitterType.Nerve;
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


    public NetworkBehaviour GetNetworkBehaviour
    {
        get
        {
            return BodyPart.BodyPartBehaviour;
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

    public bool IsConnectedToCentralNervousSystem{
        get 
        {
            if (IsCentralNervousSystem) return true;

            var parent = ParentSignalTransmitter();
            if (parent == null && !IsCentralNervousSystem)
            {
                return false;
            }

            return parent.CanTransmitSignal() ? parent.IsConnectedToCentralNervousSystem : false;
        }
    }

    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Nerve; }
        protected set { LayerType = value; }
    }

    public NerveLayer(BodyPart bodyPart, bool isCentralNervousSystem) : base(bodyPart)
    {
        IsCentralNervousSystem = IsCentralNervousSystem;
    }

    protected override void SetSuceptibilities()
    {
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Slash, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Pressure, 0.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Shock, 2f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Rad, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Toxic, 1.2f));
    }

    public INerveSignalTransmitter ParentSignalTransmitter()
    {
        return BodyPart.ParentBodyPart.CanTransmitNerveSignals() ? (INerveSignalTransmitter) BodyPart.ParentBodyPart.GetBodyLayer<INerveSignalTransmitter>() : null;
    }

    public List<INerveSignalTransmitter> ChildSignalTransmitters()
    {
        var res = new List<INerveSignalTransmitter>();
        foreach (var bodypart in BodyPart.ChildBodyParts)
        {
            if (bodypart.CanTransmitNerveSignals())
            {
                res.Add((INerveSignalTransmitter) bodypart.GetBodyLayer<INerveSignalTransmitter>());
            }
        }
        return res;
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
    }

    public bool CanTransmitSignal()
    {
        if(IsDestroyed()) return false;
        else return true;
    }
}
