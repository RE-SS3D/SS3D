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
    public List<BodyPart> ChildConnectedBodyParts { get; protected set; }

    public List<BodyPart> ParentConnectedBodyParts { get; protected set; }

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
        BodyLayers= new List<BodyLayer>();
        ChildConnectedBodyParts = new List<BodyPart>();
        ParentConnectedBodyParts = new List<BodyPart>();
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
    /// The body part is not destroyed, it's simply detached from the entity.
    /// </summary>
    public void DetachBodyPart()
    {
        //Spawn a detached body part from the entity, and destroy this one with all childs.
        // Maybe better in body part controller.
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
}
