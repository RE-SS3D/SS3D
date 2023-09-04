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
using FishNet;
using Coimbra;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using SS3D.Systems;

/// <summary>
/// Class to handle all networking stuff related to a body part, there should be only one on a given game object.
/// </summary>
public abstract class BodyPart : InteractionTargetNetworkBehaviour
{
    /// <summary>
    /// Body part to which this body part is attached, from an anatomy perspective. (left hand is attached to left arm, attached to torso...)
    /// Can be null (Human torso are the root of the tree of attached body parts)
    /// </summary>
    [SyncVar]
    protected BodyPart _parentBodyPart;

    /// <summary>
    /// When a body part is attached, its shown through its skinnedMeshrenderer on the player. When its detached, it's important to hide this.
    /// </summary>
    [SerializeField]
    private SkinnedMeshRenderer _skinnedMeshRenderer;

    /// <summary>
    /// The game object spawned upon detaching the bodypart.
    /// </summary>
    [SerializeField]
    protected GameObject _bodyPartItem;

    /// <summary>
    /// List of body parts child of this one. 
    /// </summary>
    protected readonly List<BodyPart> _childBodyParts = new List<BodyPart>();

    /// <summary>
    /// List of body layers composing a body part.
    /// </summary>
    protected readonly List<BodyLayer> _bodyLayers = new List<BodyLayer>();


	private BodyPart _externalBodyPart;

	public BodyPart ExternalBodyPart => _externalBodyPart;

	public bool IsInsideBodyPart => _externalBodyPart != null;

    /// <summary>
    /// A container containing all internal body parts. The head has a brain for an internal body part. Internal body parts should be destroyed
    /// </summary>
    [SerializeField]
    private AttachedContainer _internalBodyParts;

    /// <summary>
    /// Collider registering hits on this bodypart. It should usually be on the armature of the Entity, so it follows animations.
    /// </summary>
    [SerializeField]
    protected Collider _bodyCollider;

    public Collider BodyCollider => _bodyCollider;

    public string Name => gameObject.name;

    /// <summary>
    /// Check if this bodypart has been detached. Should always be true for all bodyparts spawned on detach.
    /// </summary>
    protected bool _isDetached;

	public HealthController HealthController;


    public ReadOnlyCollection<BodyLayer> BodyLayers
    {
        get { return _bodyLayers.AsReadOnly(); }
    }


    public ReadOnlyCollection<BodyPart> ChildBodyParts
    {
        get { return _childBodyParts.AsReadOnly(); }
    }

    public IEnumerable<BodyPart> InternalBodyParts
    {
        get { return _internalBodyParts?.Items.Select(x => x.GetComponent<BodyPart>()); }
    }

    public bool HasInternalBodyPart => _internalBodyParts != null && _internalBodyParts.Items.Count() != 0;

    /// <summary>
    /// The volume in mililiters of a given bodypart
    /// </summary>
    [SerializeField] protected double _bodyPartVolume;

    public double Volume => _bodyPartVolume;

    /// <summary>
    /// A bodypart is considered destroyed when The total amount of damages it sustained is above a maximum.
    /// </summary>
    public bool IsDestroyed => TotalDamage >= MaxDamage;

    /// <summary>
    /// A bodypart is considered severed when the total amount of damages it sustained on the bone layer is above a maximum.
    /// </summary>
    public bool IsSevered => GetBodyLayer<BoneLayer>() == null ? false : GetBodyLayer<BoneLayer>().IsDestroyed();

    public float TotalDamage => _bodyLayers.Sum(layer => layer.TotalDamage);
    public float MaxDamage => 0.5f*_bodyLayers.Sum(layer => layer.MaxDamage);

    public float RelativeDamage => TotalDamage/ MaxDamage;

    public event EventHandler OnDamageInflicted;
    public event EventHandler OnBodyPartDestroyed;
    public event EventHandler OnBodyPartDetached;
    public event EventHandler OnBodyPartLayerAdded;

    /// <summary>
    /// The parent bodypart is the body part attached to this body part, closest from the brain. 
    /// For lower left arm, it's higher left arm. For neck, it's head.
    /// Be careful, it doesn't necessarily match the game object hierarchy
    /// </summary>
    public BodyPart ParentBodyPart
    {
        get { return _parentBodyPart; }
        set => SetParentBodyPart(value);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ParentBodyPart = _parentBodyPart;
        AddInitialLayers();
    }

    public virtual void Init(BodyPart parent)
    {
        ParentBodyPart = parent;
    }

    public virtual void Init(BodyPart parentBodyPart, List<BodyPart> childBodyParts, List<BodyLayer> bodyLayers)
    {
        ParentBodyPart = parentBodyPart;
        _childBodyParts.AddRange(childBodyParts);
        _bodyLayers.AddRange(bodyLayers);
        foreach (BodyLayer bodylayer in BodyLayers)
        {
            bodylayer.BodyPart = this;
        }
    }

    /// <summary>
    /// Properly set a new parent body part, should be useful when detaching or attaching again a bodypart.
    /// </summary>
    public void SetParentBodyPart(BodyPart value)
    {
        if (value == null)
        {
            _parentBodyPart = null;
            return;
        }

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

    /// <summary>
    /// The body part is not destroyed, it's simply detached from the entity.
    /// Spawn a detached body part from the entity, and destroy this one.
    /// This spawns an item based on this body part. Upon being detached, some specific treatments are needed for some bodyparts.
    /// Implementation should handle instantiating _bodyPartItem, removing the bodypart game object and doing whatever else is necessary.
    /// </summary>
    protected virtual void DetachBodyPart()
    {

        if (_isDetached) return;
        DetachChildBodyParts();
        HideSeveredBodyPart();
        SpawnDetachedBodyPart();
        _isDetached = true;
        InvokeOnBodyPartDetached();
        Dispose(false);
    }

    protected void DetachChildBodyParts()
    {
        for (int i = _childBodyParts.Count - 1; i >= 0; i--)
        {
            _childBodyParts[i].DetachBodyPart();
        }
    }

    protected BodyPart SpawnDetachedBodyPart()
    {
        /*
         * When detaching a bodypart, a prefab is spawned, very similar but having a few different scripts like the Item script, or removing a few others.
         * Fishnet in version 3.10.7 does not allow adding networkbehaviours, however it allows disabling and enabling.
         * When detaching a body part, don't forget to check if some scripts were enabled/disabled, and update the relevant values of the spawned body part with 
         * those of the just detached body part.
         */
        GameObject go = Instantiate(_bodyPartItem, Position, Rotation);
        InstanceFinder.ServerManager.Spawn(go, null);
        BodyPart bodyPart = go.GetComponent<BodyPart>();
        CopyValuesToBodyPart(bodyPart);
        bodyPart._isDetached = true;
        return bodyPart;
    }

    /// <summary>
    /// Copy the value of this to another body part. 
    /// Especially useful to keep sustained damages on a spawned body part upon detaching it. 
    /// </summary>
    protected virtual void CopyValuesToBodyPart(BodyPart bodyPart)
    {
        foreach(BodyLayer layer in bodyPart.BodyLayers)
        {
            BodyLayer layerToWrite = BodyLayers.Where(x => x.LayerType == layer.LayerType).First();
            if (layerToWrite == null) continue;
            layer.CopyLayerValues(layerToWrite);
        }
    }

    /// <summary>
    /// The body part took so much damages that it's simply destroyed.
    /// Think complete crushing, burning to dust kind of stuff.
    /// All child body parts are detached, all internal body parts are destroyed.
    /// </summary>
    [Server]
    public virtual void DestroyBodyPart()
    {
        DetachChildBodyParts();

        // Destroy all internal body parts
        if (_internalBodyParts != null){

            foreach (Item item in _internalBodyParts.Items)
            {
                BodyPart internalBodyPart = item.GetComponentInChildren<BodyPart>();
                internalBodyPart?.DestroyBodyPart();
            }
            _internalBodyParts.Purge();
        }

        // Dispose of this body part
        InvokeOnBodyPartDestroyed();
        Dispose(true);
    }

    /// <summary>
    /// Method to call at the end of Destroy and/or Detach. Remove parent body part, child in parent, dump or destroy content of containers 
    /// and deactivate this body part's game object for all observers.
    /// </summary>
    [Server]
    protected void Dispose(bool purgeContainersContent)
    {
        RemoveChildAndParent();
        DumpOrPurgeContainers(purgeContainersContent);
        CleanLayers();
        Deactivate();
    }

    /// <summary>
    /// Simply dump the content of all containers
    /// </summary>
    private void DumpOrPurgeContainers(bool purgeContainersContent)
    {
        IEnumerable<AttachedContainer> containers = GetComponentsInChildren<AttachedContainer>().Where(x => x.GetComponent<OrganContainer>() == null);
        foreach (AttachedContainer container in containers)
        {
            if (purgeContainersContent)
            {
                container.Purge();
            }
            else
            {
                container.Dump();
            }
        }
    }

    /// <summary>
    /// Remove the reference to this in the parent body part, and make the parent body part reference null.
    /// </summary>
    protected void RemoveChildAndParent()
    {
        _parentBodyPart?._childBodyParts.Remove(this);
        _parentBodyPart = null;
    }

    /// <summary>
    /// Destroy the body layers properly
    /// </summary>
    protected void CleanLayers()
    {
        _bodyLayers.ForEach(x => x.Cleanlayer());
    }



    /// <summary>
    /// Deactivate this game object, should run for all observers, and for late joining (hence bufferlast = true).
    /// </summary>
    [ObserversRpc(RunLocally = true, BufferLast = true)]
    protected void Deactivate()
    {
        gameObject.SetActive(false);
        gameObject.Dispose(true);
    }

    /// <summary>
    /// Add a body layer if none of the same type are already present on this body part.
    /// TODO : use generic to check type, actually check if only one body layer of each kind.
    /// </summary>
    /// <returns> The body layer was added.</returns>
    public virtual bool TryAddBodyLayer(BodyLayer layer)
    {
        layer.BodyPart = this;
        _bodyLayers.Add(layer);
        return true;
    }


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
    public virtual bool TryInflictDamage(BodyLayerType type, DamageTypeQuantity damageTypeQuantity)
    {
        BodyLayer layer = FirstBodyLayerOfType(type);
        if (!BodyLayers.Contains(layer)) return false;
        layer.InflictDamage(damageTypeQuantity);
        if (IsDestroyed)
        {
            DestroyBodyPart();
        }
        else if (IsSevered && !_isDetached)
        {
            DetachBodyPart();
        }

        OnDamageInflicted?.Invoke(this, EventArgs.Empty);
        return true;    
    }

    /// <summary>
    /// inflict same type damages to all layers present on this body part.
    /// </summary>
    public void InflictDamageToAllLayer(DamageTypeQuantity damageTypeQuantity)
    {
        foreach (BodyLayer layer in BodyLayers)
        {
            TryInflictDamage(layer.LayerType, damageTypeQuantity);
        }
    }

    /// <summary>
    /// inflict same type damages to all layers present on this body part except one.
    /// </summary>
    public void InflictDamageToAllLayerButOne<T>(DamageTypeQuantity damageTypeQuantity)
    {
        foreach (BodyLayer layer in BodyLayers)
        {
            if (!(layer is T))
                TryInflictDamage(layer.LayerType, damageTypeQuantity);    
        }
    }

    /// <summary>
    /// Check if this body part contains a given layer type.
    /// </summary>
    public bool ContainsLayer(BodyLayerType layerType)
    {
        return BodyLayers.Any(x => x.LayerType == layerType);
    }

    public BodyLayer FirstBodyLayerOfType(BodyLayerType layerType)
    {
        return BodyLayers.Where(x => x.LayerType == layerType).First();
    }


    /// <summary>
    /// GetBodyLayer of type T on this bodypart.
    /// Todo : change that with TryGetBody.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public BodyLayer GetBodyLayer<T>()
    {
        foreach (BodyLayer layer in BodyLayers)
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
    public string Describe()
    {
        string description = "";
        foreach (BodyLayer layer in BodyLayers)
        {
            description += "Layer " + layer.GetType().ToString() + "\n";
        }
        description += "Child connected body parts : \n";
        foreach (BodyPart part in _childBodyParts)
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

    public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        return new IInteraction[] {};
    }

	public void AddInternalBodyPart(BodyPart part)
	{
		_internalBodyParts.Container.AddItem(part.gameObject.GetComponent<Item>());
		part._externalBodyPart = this;
	}

	public void RemoveInternalBodyPart(BodyPart part)
	{
		_internalBodyParts.Container.RemoveItem(part.gameObject.GetComponent<Item>());
		part._externalBodyPart = null;
	}

	/// <summary>
	/// Hide a freshly cut body part on the player.
	/// </summary>
	protected void HideSeveredBodyPart()
    {
        if (_skinnedMeshRenderer == null) return;
        _skinnedMeshRenderer.enabled = false;
    }

    protected void InvokeOnBodyPartDetached()
    {
        OnBodyPartDetached?.Invoke(this, EventArgs.Empty);
    }

    protected void InvokeOnBodyPartDestroyed()
    {
        OnBodyPartDestroyed?.Invoke(this, EventArgs.Empty);
    }

    protected void InvokeOnBodyPartLayerAdded()
    {
        OnBodyPartLayerAdded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Add the body layers in their initial states on the player. 
    /// </summary>
    protected abstract void AddInitialLayers();


}
