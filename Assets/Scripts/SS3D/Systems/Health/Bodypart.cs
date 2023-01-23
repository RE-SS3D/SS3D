using Coimbra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BodyPart
{

    /// <summary>
    /// The list of body parts this body part is directly connected to. 
    /// </summary>
    public List<BodyPart> ConnectedBodyParts;

    /// <summary>
    /// The list of body layers constituting this body part.
    /// </summary>
    public List<BodyLayer> BodyLayers { get; protected set; }


    public virtual void AddBodyLayer(BodyLayer layer)
    {
        BodyLayers.Add(layer);
    }

    public virtual void RemoveBodyLayer(BodyLayer layer) 
    {
        BodyLayers.Remove(layer);
    }

    public virtual void InflictDamage(DamageTypeQuantity damageTypeQuantity)
    {
        foreach (var layer in BodyLayers)
        {
            layer.InflictDamage(damageTypeQuantity);
        }
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
}
