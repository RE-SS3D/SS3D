using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Permissions;
using Cysharp.Threading.Tasks;
using UnityEditor;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Health;
using System.Linq;
using System.Collections.ObjectModel;

/// <summary>
/// Class to handle all networking stuff related to a body part, there should be only one on a given game object.
/// There should always be a network object component everywhere this component is.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class BodyPart : InteractionTargetNetworkBehaviour
{


    [SerializeField] 
    private bool isHuman;

    [SyncVar]
    private BodyPart _parentBodyPart;

    [SyncObject]
    private readonly SyncList<BodyPart> _childBodyParts = new SyncList<BodyPart>();

    [SyncObject]
    public readonly SyncList<BodyLayer> _bodyLayers = new SyncList<BodyLayer>();

    public string Name;

    public ReadOnlyCollection<BodyLayer> BodyLayers
    {
        get { return ((List<BodyLayer>)_bodyLayers.Collection).AsReadOnly(); }
    }

    public ReadOnlyCollection<BodyPart> ChildBodyParts
    {
        get { return ((List<BodyPart>)_childBodyParts.Collection).AsReadOnly(); }
    }


    public void Start()
    {
        SetUpChild();
    }

    /// <summary>
    /// The list of body layers constituting this body part.
    /// </summary>
    public INerveSignalTransmitter NerveSignalTransmitter
    {
        get;
        private set;
    }


    /// <summary>
    /// The parent bodypart is the body part attached to this body part, closest from the brain. 
    /// For lower left arm, it's higher left arm. For neck, it's head.
    /// Be careful, it doesn't necessarily match the game object hierarchy
    /// </summary>
    public BodyPart ParentBodyPart
    {
        get { return _parentBodyPart; }
        set
        {
            if (value == null)
                return;

            if (_childBodyParts.Contains(value))
            {
                Punpun.Error(this, "trying to set up {bodypart} bodypart as both child and" +
                    " parent of {bodypart} bodypart.", Logs.Generic, value, this);
                return;
            }

            Punpun.Debug(this, "value of parent body part {bodypart}", Logs.Generic, value);
            _parentBodyPart = value;
            _parentBodyPart._childBodyParts.Add(this);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    /// <summary>
    /// Constructor to allow testing without mono/network behaviour script.
    /// </summary>
    public virtual void Init(string name = "")
    {
        Name = name; 
    }

    public virtual void Init(BodyPart parent, string name = "")
    {
        Name = name;
        ParentBodyPart = parent;
    }


    public virtual void Init(BodyPart parentBodyPart, List<BodyPart> childBodyParts, List<BodyLayer> bodyLayers, string name = "")
    {
        Name = name;
        ParentBodyPart = parentBodyPart;
        _childBodyParts.AddRange(childBodyParts);
        _bodyLayers.AddRange(bodyLayers);
        foreach (var bodylayer in BodyLayers)
        {
            bodylayer.BodyPart = this;
        }
    }

    [Server]
    private void SetUpChild()
    {   
        if(_parentBodyPart != null)
        {
            _parentBodyPart._childBodyParts.Add(this);
        }
    }

    
    [ServerRpc]
    public void ServerRpcAddChildBodyPart(NetworkConnection conn, NetworkObject bodyPart)
    {
        PermissionSystem permissionSystem = Subsystems.Get<PermissionSystem>();
        if (!permissionSystem.HasAdminPermission(conn))
        {
            return;
        }
        ObserverRpcAddChildBodyPart(bodyPart);
    }

    [ObserversRpc(RunLocally = true)]
    public void ObserverRpcAddChildBodyPart(NetworkObject bodyPart)
    {
        AddChildBodyPart(bodyPart.GetComponent<BodyPart>());
    }

    [ServerRpc]
    public void ServerRpcAddNerveLayer(NerveLayer nerveLayer)
    {
        ObserversRpcAddNerveLayer(nerveLayer);
    }

    [ObserversRpc(RunLocally = true)]
    public void ObserversRpcAddNerveLayer(NerveLayer nerveLayer)
    {
       TryAddBodyLayer(nerveLayer);
    }



    /// <summary>
    /// The body part is not destroyed, it's simply detached from the entity.
    /// </summary>
    public void DetachBodyPart()
    {
        //Spawn a detached body part from the entity, and destroy this one with all childs.
        // Maybe better in body part controller.
        //throw new NotImplementedException();
        _parentBodyPart.DetachBodyPart();
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



    public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        return new IInteraction[] { new KillInteraction() };
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

        if (layer is INerveSignalTransmitter signalTransmitter)
        {
            NerveSignalTransmitter = signalTransmitter;
        }


        _bodyLayers.Add(layer);
        layer.BodyPart = this;
        return true;

    }

    public float TotalDamage => _bodyLayers.Sum(layer => layer.TotalDamage);
    public float MaxDamage => _bodyLayers.Sum(layer => layer.MaxDamage);


    /// <summary>
    /// Remove a body layer from the body part.
    /// TODO : check if it exists first.
    /// </summary>
    /// <param name="layer"></param>
    public virtual void RemoveBodyLayer(BodyLayer layer)
    {
         _bodyLayers.Remove(layer);
    }

    /// <summary>
    /// Add a new body part as a child of this one. 
    /// </summary>
    /// <param name="bodyPart"></param>
    public virtual void AddChildBodyPart(BodyPart bodyPart)
    {
        _childBodyParts.Add(bodyPart);
    }

    /// <summary>
    /// Inflic damages of a certain kind on a certain body layer type if the layer is present.
    /// </summary>
    /// <returns>True if the damage could be inflicted</returns>
    public virtual bool TryInflictDamage<T>(DamageTypeQuantity damageTypeQuantity)
    {
        var layer = GetBodyLayer<T>();
        if (!BodyLayers.Contains(layer)) return false;
        layer.InflictDamage(damageTypeQuantity);
        return true;
    }

    /// <summary>
    /// inflict same type damages to all layers present on this body part.
    /// </summary>
    public virtual void InflictDamageToAllLayer(DamageTypeQuantity damageTypeQuantity)
    {
        foreach (var layer in BodyLayers)
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
            if (!(layer is T))
                layer.InflictDamage(damageTypeQuantity);
        }
    }


    /// <summary>
    /// Check if a nerveSignalTransmitter is present on this bodypart.
    /// TODO : check if nerve signal layer is destroyed too.
    /// </summary>
    public bool CanTransmitNerveSignals()
    {
        foreach (var layer in BodyLayers)
        {
            if (layer is INerveSignalTransmitter) return true;
        }
        return false;
    }

    public float ProducePain()
    {
        return NerveSignalTransmitter != null ? NerveSignalTransmitter.ProducePain() : 0f;
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
            if (layer is T)
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
        foreach (var layer in BodyLayers)
        {
            description += "Layer " + layer.GetType().ToString() + "\n";
        }
        description += "Child connected body parts : \n";
        foreach (var part in _childBodyParts)
        {
            description += part.gameObject.name + "\n";
        }
        description += "Parent body part : \n";
        description += ParentBodyPart.name;
        return description;
    }

    public override string ToString()
    {
        return Name;
    }


}
