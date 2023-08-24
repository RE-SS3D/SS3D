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

/// <summary>
/// Class to handle all networking stuff related to a body part, there should be only one on a given game object.
/// </summary>
public abstract class BodyPart : InteractionTargetNetworkBehaviour
{

    [SyncVar]
    protected BodyPart _parentBodyPart;

    [SerializeField]
    private SkinnedMeshRenderer _skinnedMeshRenderer;

	[SerializeField]
	protected GameObject _bodyPartItem;


	protected readonly List<BodyPart> _childBodyParts = new List<BodyPart>();

	protected readonly List<BodyLayer> _bodyLayers = new List<BodyLayer>();

	[SerializeField]
	protected AttachedContainer _internalBodyParts;

	[SerializeField]
	protected Collider _bodyCollider;

	public Collider BodyCollider => _bodyCollider;

    public string Name => gameObject.name;




    public ReadOnlyCollection<BodyLayer> BodyLayers
    {
        get { return _bodyLayers.AsReadOnly(); }
    }


	public ReadOnlyCollection<BodyPart> ChildBodyParts
    {
        get { return _childBodyParts.AsReadOnly(); }
    }

	public IEnumerable<Item> InternalBodyParts
	{
		get { return _internalBodyParts.Items; }
	}

	public bool IsDestroyed => TotalDamage >= MaxDamage;

	public bool IsSevered => GetBodyLayer<BoneLayer>().IsDestroyed();


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
        foreach (var bodylayer in BodyLayers)
        {
            bodylayer.BodyPart = this;
        }
    }

	/// <summary>
	/// The body part is not destroyed, it's simply detached from the entity.
	/// Spawn a detached body part from the entity, and destroy this one.
	/// This spawns an item based on this body part. Upon being detached, some specific treatments are needed for some bodyparts.
	/// Implementation should handle instantiating _bodyPartItem, removing the bodypart game object and doing whatever else is necessary.
	/// </summary>
	protected virtual void DetachBodyPart()
	{
		/*
		 * When detaching a bodypart, a prefab is spawned, very similar but having a few different scripts like the Item script, or removing a few others.
		 * Fishnet in version 3.10.7 does not allow adding networkbehaviours, however it allows disabling and enabling.
		 * When detaching a body part, don't forget to check if some scripts were enabled/disabled, and update the relevant values of the spawned body part with 
		 * those of the just detached body part.
		 */
		GameObject go = Instantiate(_bodyPartItem, Position, Rotation);
		InstanceFinder.ServerManager.Spawn(go, null);
		Dispose();
	}

	/// <summary>
	/// The body part took so much damages that it's simply destroyed.
	/// Think complete crushing, burning to dust kind of stuff.
	/// All child body parts are detached, all internal are destroyed.
	/// </summary>
	/// <exception cref="NotImplementedException"></exception>
	[Server]
	public virtual void DestroyBodyPart()
    {
		// destroy this body part with all childs on the entity, detach all childs.
		for (int i = _childBodyParts.Count - 1; i >= 0; i--)
		{
			_childBodyParts[i].RemoveBodyPart();
		}

		if (_internalBodyParts != null){

			foreach (var item in _internalBodyParts.Items)
			{
				var internalBodyPart = item.GetComponentInChildren<BodyPart>();
				internalBodyPart?.DestroyBodyPart();
			}
			_internalBodyParts.Container?.Purge();
		}

		Dispose();
	}

	/// <summary>
	/// Method to call at the end of Destroy and/or Detach
	/// </summary>
	[Server]
	protected void Dispose()
	{
		_parentBodyPart?._childBodyParts.Remove(this);
		_parentBodyPart = null;
		DumpContainers();
		Deactivate();
	}

	protected void DumpContainers()
	{
		var containers = GetComponentsInChildren<AttachedContainer>();
		foreach (var container in containers)
		{
			container.Container.Dump();
		}
	}

	protected void RemoveChildAndParent()
	{
		_parentBodyPart?._childBodyParts.Remove(this);
		_parentBodyPart = null;
	}



	[ObserversRpc(RunLocally = true, BufferLast = true)]
	protected void Deactivate()
	{
		gameObject.SetActive(false);
	}



	public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        return new IInteraction[] { new KillInteraction() };
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
    public virtual bool TryInflictDamage(BodyLayerType type, DamageTypeQuantity damageTypeQuantity)
    {
		BodyLayer layer = FirstBodyLayerOfType(type);
		if (!BodyLayers.Contains(layer)) return false;
		layer.InflictDamage(damageTypeQuantity);
		if (IsDestroyed)
		{
			DestroyBodyPart();
		}
		else if (IsSevered)
		{
			RemoveBodyPart();
		}

		return true;	
    }

	/// <summary>
	/// inflict same type damages to all layers present on this body part.
	/// </summary>
	public virtual void InflictDamageToAllLayer(DamageTypeQuantity damageTypeQuantity)
    {
        foreach (BodyLayer layer in BodyLayers)
        {
			TryInflictDamage(layer.LayerType, damageTypeQuantity);
        }
    }

    /// <summary>
    /// inflict same type damages to all layers present on this body part except one.
    /// </summary>
    public virtual void InflictDamageToAllLayerButOne<T>(DamageTypeQuantity damageTypeQuantity)
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

	private void RemoveBodyPart()
	{
		for(int i= _childBodyParts.Count-1; i>=0;i--)
		{
			_childBodyParts[i].RemoveBodyPart();
		}
		RemoveSingleBodyPart();
	}

	protected virtual void RemoveSingleBodyPart()
	{
		HideSeveredBodyPart();
		DetachBodyPart();
		_parentBodyPart?._childBodyParts.Remove(this);
		_parentBodyPart = null;
	}

	private void HideSeveredBodyPart()
    {
        _skinnedMeshRenderer.enabled = false;
    }

    protected abstract void AddInitialLayers();


}
