using Codice.CM.SEIDInfo;
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

    private BodyPart _parentBodyPart;

    /// <summary>
    /// Return the parent of this BodyPart if it exists.
    /// </summary>
    public BodyPart ParentBodyPart
    {
        get { return _parentBodyPart; }
        set
        {
            if (value == null)
                return;

            if (ChildBodyParts.Contains(value))
            {
                Punpun.Error(this, "trying to set up {bodypart} bodypart as both child and" +
                    " parent of {bodypart} bodypart.", Logs.Generic, value, this);
                return;
            }

            Punpun.Debug(this, "value of parent body part {bodypart}", Logs.Generic, value);
            _parentBodyPart = value;
            _parentBodyPart.ChildBodyParts.Add(this);
        }
    }

    public string Name;

    /// <summary>
    /// The list of body layers constituting this body part.
    /// </summary>
    public List<BodyLayer> BodyLayers { get; protected set; }

    /// <summary>
    /// The list of body layers constituting this body part.
    /// </summary>
    public List<INerveSignalTransmitter> NerveSignalTransmitters
    {
        get
        {
            var transmitters = new List<INerveSignalTransmitter>();
            foreach(var layer in BodyLayers)
            {
                if(layer is INerveSignalTransmitter)
                {
                    transmitters.Add(layer as INerveSignalTransmitter);
                }
            }
            return transmitters;
        }
    }

    public BodyPartBehaviour BodyPartBehaviour { get; protected set; }

    /// <summary>
    /// Constructor to allow testing without mono/network behaviour script.
    /// </summary>
    public BodyPart(string name = "")
    {
        Name = name;
        ChildBodyParts= new List<BodyPart>();
        BodyLayers= new List<BodyLayer>();
    }

    public BodyPart(BodyPart parent, string name = "")
    {
        Name = name;
        ChildBodyParts = new List<BodyPart>();
        BodyLayers = new List<BodyLayer>();
        ParentBodyPart = parent;
    }

    public BodyPart(BodyPartBehaviour bodyPartBehaviour, string name = "")
    {
        Name = name;
        BodyPartBehaviour = bodyPartBehaviour;
        ChildBodyParts =  BodyPartBehaviour.ChildBodyPartsBehaviour.Collection.Select(x=> x.BodyPart).ToList();
        BodyLayers = (List<BodyLayer>) BodyPartBehaviour.BodyLayers.Collection;
        if (BodyPartBehaviour.ParentBodyPartBehaviour != null)
            ParentBodyPart = BodyPartBehaviour.ParentBodyPartBehaviour.BodyPart;
    }

    public BodyPart(BodyPart parentBodyPart, List<BodyPart> childBodyParts, List<BodyLayer> bodyLayers, string name = "")
    {
        Name = name;
        ParentBodyPart = parentBodyPart;
        ChildBodyParts = childBodyParts;
        BodyLayers = bodyLayers;
        foreach(var bodylayer in BodyLayers)
        {
            bodylayer.BodyPart = this;
        }
    }

    /// <summary>
    /// Add a body layer if none of the same type are already present on this body part.
    /// TODO : use generic to check type, actually check if only one of each kind.
    /// </summary>
    /// <returns> The body layer was added.</returns>
    public virtual bool TryAddBodyLayer(BodyLayer layer)
    {
        // Make sure only one nerve signal layer can exist at a time on a bodypart.
        if (layer is INerveSignalTransmitter && CanTransmitNerveSignals())
        {
            Punpun.Warning(this, "Can't have more than one nerve signal transmitter on a bodypart.");
            return false;
        }

        if (BodyPartBehaviour == null)
            BodyLayers.Add(layer);

        BodyPartBehaviour.BodyLayers.Add(layer);
        layer.BodyPart = this;
        return true;

    }

    /// <summary>
    /// Remove a body layer from the body part.
    /// TODO : check if it exists first.
    /// </summary>
    /// <param name="layer"></param>
    public virtual void RemoveBodyLayer(BodyLayer layer) 
    {
        if(BodyPartBehaviour == null)
        BodyLayers.Remove(layer);
    }

    /// <summary>
    /// Add a new body part as a child of this one. 
    /// </summary>
    /// <param name="bodyPart"></param>
    public virtual void AddChildBodyPart(BodyPart bodyPart)
    {
        if (BodyPartBehaviour == null)
            ChildBodyParts.Add(bodyPart);

        if(bodyPart.BodyPartBehaviour == null)
        {
            Punpun.Error(this, "This body part has a reference to a body part behaviour, it should only add to its child bodyParts with references to body part behaviour ");
            return;
        }
        
        BodyPartBehaviour.ChildBodyPartsBehaviour.Add(bodyPart.BodyPartBehaviour);
    }

    /// <summary>
    /// Inflic damages of a certain kind on a certain body layer type if the layer is present.
    /// </summary>
    /// <returns>True if the damage could be inflicted</returns>
    public virtual bool TryInflictDamage<T>(DamageTypeQuantity damageTypeQuantity)
    {
        var layer = GetBodyLayer<T>();
        if (!BodyLayers.Contains(layer)) return false ;
        layer.InflictDamage(damageTypeQuantity);
        return true;
    }

    /// <summary>
    /// inflict same type damages to all layers present on this body part.
    /// </summary>
    public virtual void InflictDamageToAllLayer(DamageTypeQuantity damageTypeQuantity)
    {
        foreach(var layer in BodyLayers)
        {
            layer.InflictDamage(damageTypeQuantity);
        }
    }

    /// <summary>
    /// inflict same type damages to all layers present on this body part.
    /// </summary>
    public virtual void InflictDamageToAllLayerButOne<T>(DamageTypeQuantity damageTypeQuantity)
    {
        foreach (var layer in BodyLayers)
        {
            if(!(layer is T))
                layer.InflictDamage(damageTypeQuantity);
        }
    }


    /// <summary>
    /// Check if a nerveSignalTransmitter is present on this bodypart.
    /// TODO : check if nerve signal layer is destroyed too.
    /// </summary>
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

    /// <summary>
    /// GetBodyLayer of type T on this bodypart.
    /// Todo : change that with TryGetBody.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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

    /// <summary>
    /// Describe extensively the bodypart.
    /// </summary>
    /// <returns></returns>
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

    public override string ToString()
    {
        return Name;
    }
}
