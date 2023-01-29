using Coimbra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public abstract class BodyPart
{

    /// <summary>
    /// The list of body parts this body part is directly connected to. 
    /// </summary>
    private List<BodyPart> ChildConnectedBodyParts;

    private List<BodyPart> ParentConnectedBodyParts;

    /// <summary>
    /// The list of body layers constituting this body part.
    /// </summary>
    public List<BodyLayer> BodyLayers { get; protected set; }

    public BodyPartBehaviour BodyPartBehaviour { get; protected set; }

    /// <summary>
    /// Constructor to allow testing without mono/network behaviour script.
    /// </summary>
    public BodyPart()
    {

        if(BodyPartBehaviour != null)
        {
            BodyLayers = new List<BodyLayer>();
            ChildConnectedBodyParts = new List<BodyPart>();
            ParentConnectedBodyParts = new List<BodyPart>();
        }
        else
        {
            
        }
    }

    public BodyPart(BodyPartBehaviour bodyPartBehaviour) : this()
    {
        BodyPartBehaviour = bodyPartBehaviour;
    }

    public virtual void AddBodyLayer(BodyLayer layer)
    {
        BodyLayers.Add(layer);
    }

    public virtual void RemoveBodyLayer(BodyLayer layer) 
    {
        BodyLayers.Remove(layer);
    }


    public virtual void AddConnectedBodyPart(BodyPart bodyPart, bool isChild)
    {
        if (isChild)
        {
            ChildConnectedBodyParts.Add(bodyPart);
        }
        else
        {
            ParentConnectedBodyParts.Add(bodyPart);
        }
    }

    public virtual void InflictDamage(DamageTypeQuantity damageTypeQuantity)
    {
        foreach (var layer in BodyLayers)
        {
            layer.InflictDamage(damageTypeQuantity);
        }
    }


    public bool CanTransmitNerveSignals()
    {
        foreach(var layer in BodyLayers)
        {
            if (layer is INerveSignalTransmitter) return true;
        }
        return false;
    }


    /// <summary>
    /// Check if this body part contains a given layer type.
    /// </summary>
    public bool ContainsLayer(BodyLayerType layerType)
    {
        return BodyLayers.Any(x => x.LayerType == layerType);
    }

    public BodyLayer GetBodyLayer<T>()
    {
        foreach (var layer in BodyLayers)
        {
            if(layer is T)
            {
                return layer;
            }
        }

        return null;
    }

    public string Describe()
    {
        var description = "";
        foreach(var layer in BodyLayers)
        {
            description += "Layer " + layer.GetType().ToString() + "\n";
        }
        description += "Child connected body parts : \n";
        foreach(var part in ChildConnectedBodyParts)
        {
            description += part.BodyPartBehaviour.gameObject.name + "\n";
        }
        description += "Parent connected body parts : \n";
        foreach (var part in ParentConnectedBodyParts)
        {
            description += part.BodyPartBehaviour.gameObject.name + "\n";
        }
        return description;
    }
}
