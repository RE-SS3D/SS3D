using Coimbra;
using SS3D.Logging;
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
    public List<BodyPart> ChildBodyParts;

    public BodyPart ParentBodyPart;

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
        ChildBodyParts= new List<BodyPart>();
        BodyLayers= new List<BodyLayer>();
    }

    public BodyPart(BodyPartBehaviour bodyPartBehaviour) : this()
    {
        BodyPartBehaviour = bodyPartBehaviour;
        ChildBodyParts = (List<BodyPart>) BodyPartBehaviour.ChildBodyPartsBehaviour.Select(x=> x.BodyPart);
        ParentBodyPart = BodyPartBehaviour.ParentBodyPartBehaviour.BodyPart;
    }

    public BodyPart(BodyPart parentBodyPart, List<BodyPart> childBodyParts, List<BodyLayer> bodyLayers)
    {
        ParentBodyPart = parentBodyPart;
        ChildBodyParts = childBodyParts;
        BodyLayers = bodyLayers;
    }

    public virtual void AddBodyLayer(BodyLayer layer)
    {
        // Make sure only one nerve signal layer can exist at a time on a bodypart.
        if (layer is INerveSignalTransmitter && CanTransmitNerveSignals())
        {
            Debug.Log("panic");
            return;
        }
        BodyLayers.Add(layer);
    }

    public virtual void RemoveBodyLayer(BodyLayer layer) 
    {
        BodyLayers.Remove(layer);
    }


    public virtual void AddChildBodyPart(BodyPart bodyPart)
    {
            ChildBodyParts.Add(bodyPart);
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
        foreach(var part in ChildBodyParts)
        {
            description += part.BodyPartBehaviour.gameObject.name + "\n";
        }
        description += "Parent body part : \n";
        description += ParentBodyPart.BodyPartBehaviour.name;
        return description;
    }
}
