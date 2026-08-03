using System;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Item))]
    public class Stackable : NetworkBehaviour
    {
        [SerializeField, Min(2)]
        private int _maxStack = 2;

        [SerializeField, HideInInspector]
        private AttachedContainer _stackContainer;

        [SerializeField]
        private GameObject[] _visualCopies = Array.Empty<GameObject>();

        [SyncVar(OnChange = nameof(SyncAmount))]
        private int _amount = 1;

        private Item _item;

        public int MaxStack => Mathf.Max(2, _maxStack);

        public AttachedContainer StackContainer => _stackContainer;

        public GameObject[] VisualCopies => _visualCopies;

        // Use the local container count until the replicated amount catches up.
        public int Amount => Math.Max(_amount, LocalAmount);

        public int AvailableSpace => Mathf.Max(0, MaxStack - Amount);

        public bool IsFull => AvailableSpace == 0;

        public Item Item => _item != null ? _item : _item = GetComponent<Item>();

        public event Action<Stackable> OnAmountChanged;

        private int LocalAmount => 1 + (_stackContainer != null ? _stackContainer.ItemCount : 0);

        private void Awake()
        {
            _item = GetComponent<Item>();
            _amount = LocalAmount;
            SubscribeToContainer();
            UpdateVisualCopies();
        }

        private void OnEnable()
        {
            SubscribeToContainer();
        }

        private void OnDisable()
        {
            if (_stackContainer != null)
            {
                _stackContainer.OnContentsChanged -= HandleStackContainerChanged;
            }
        }

        private void OnValidate()
        {
            _maxStack = Mathf.Max(2, _maxStack);
            UpdateVisualCopies();
        }

        public void Init(int maxStack, AttachedContainer stackContainer = null)
        {
            Init(maxStack, stackContainer, _visualCopies);
        }

        public void Init(int maxStack, AttachedContainer stackContainer, GameObject[] visualCopies)
        {
            _maxStack = Mathf.Max(2, maxStack);
            _stackContainer = stackContainer;
            _visualCopies = visualCopies ?? Array.Empty<GameObject>();
            SubscribeToContainer();
            UpdateVisualCopies();
        }

        public bool CanStackWith(Item other)
        {
            return other != null
                   && other.TryGetStackable(out Stackable otherStackable)
                   && CanStackWith(otherStackable);
        }

        public bool CanStackWith(Stackable other)
        {
            if (other == null || other == this)
            {
                return false;
            }

            Item thisItem = Item;
            Item otherItem = other.Item;

            if (thisItem.Asset != null || otherItem.Asset != null)
            {
                return thisItem.Asset != null
                       && otherItem.Asset != null
                       && thisItem.Asset.Equals(otherItem.Asset);
            }

            return thisItem.Name == otherItem.Name;
        }

        [Server]
        public int MergeFrom(Stackable source)
        {
            if (!CanMergeFrom(source))
            {
                return 0;
            }

            int amountToMove = Math.Min(AvailableSpace, source.Amount);
            int moved = MoveContainedItems(source, amountToMove);

            if (moved < amountToMove)
            {
                moved += MoveVisibleItem(source);
            }

            NotifyAmountChanged();
            source.NotifyAmountChanged();
            return moved;
        }

        [Server]
        public Item TakeOne()
        {
            return Split(1).FirstOrDefault();
        }

        [Server]
        public Item[] Split(int amount)
        {
            if (amount <= 0 || Amount <= 1 || _stackContainer == null)
            {
                return Array.Empty<Item>();
            }

            int amountToTake = Math.Min(amount, Amount - 1);
            Item[] items = _stackContainer.Items.Reverse().Take(amountToTake).ToArray();

            foreach (Item item in items)
            {
                _stackContainer.RemoveItem(item);
            }

            NotifyAmountChanged();
            return items;
        }

        public bool CanMergeFrom(Stackable source)
        {
            return source != null
                   && CanStackWith(source)
                   && AvailableSpace > 0
                   && _stackContainer != null;
        }

        private int MoveContainedItems(Stackable source, int amountToMove)
        {
            if (source._stackContainer == null)
            {
                return 0;
            }

            int moved = 0;
            foreach (Item containedItem in source._stackContainer.Items.ToArray())
            {
                if (moved >= amountToMove)
                {
                    break;
                }

                if (source._stackContainer.TransferItemToOther(containedItem, NextStackPosition(), _stackContainer))
                {
                    moved++;
                }
            }

            return moved;
        }

        private int MoveVisibleItem(Stackable source)
        {
            Item sourceItem = source.Item;
            AttachedContainer sourceContainer = sourceItem.Container;

            if (sourceContainer == null)
            {
                return _stackContainer.AddItemPosition(sourceItem, NextStackPosition()) ? 1 : 0;
            }

            return sourceContainer.TransferItemToOther(sourceItem, NextStackPosition(), _stackContainer) ? 1 : 0;
        }

        private Vector2Int NextStackPosition()
        {
            if (_stackContainer == null)
            {
                return Vector2Int.zero;
            }

            for (int y = 0; y < _stackContainer.Size.y; y++)
            {
                for (int x = 0; x < _stackContainer.Size.x; x++)
                {
                    Vector2Int position = new(x, y);
                    if (_stackContainer.ItemAt(position) == null)
                    {
                        return position;
                    }
                }
            }

            return Vector2Int.zero;
        }

        private void SubscribeToContainer()
        {
            if (_stackContainer == null)
            {
                return;
            }

            _stackContainer.OnContentsChanged -= HandleStackContainerChanged;
            _stackContainer.OnContentsChanged += HandleStackContainerChanged;
        }

        private void HandleStackContainerChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            NotifyAmountChanged();
        }

        private void NotifyAmountChanged()
        {
            _amount = LocalAmount;
            bool isServer = HasNetworkObject() && IsServer;
            bool isSpawned = HasNetworkObject() && IsSpawned;
            ApplyAmountChanged();

            if (isServer && isSpawned)
            {
                RpcSyncAmount(_amount);
            }
        }

        private void SyncAmount(int oldAmount, int newAmount, bool asServer)
        {
            ApplyAmountChanged();
        }

        [ObserversRpc(BufferLast = true, RunLocally = true)]
        private void RpcSyncAmount(int amount)
        {
            _amount = amount;
            ApplyAmountChanged();
        }

        private void ApplyAmountChanged()
        {
            UpdateVisualCopies();
            OnAmountChanged?.Invoke(this);
        }

        private bool HasNetworkObject()
        {
            try
            {
                return NetworkObject != null;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        private void UpdateVisualCopies()
        {
            if (_visualCopies == null)
            {
                return;
            }

            for (int i = 0; i < _visualCopies.Length; i++)
            {
                if (_visualCopies[i] != null)
                {
                    bool active = i < Amount - 1;
                    _visualCopies[i].SetActive(active);
                    foreach (Renderer renderer in _visualCopies[i].GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.enabled = active && Item.IsVisible();
                    }

                    foreach (Collider collider in _visualCopies[i].GetComponentsInChildren<Collider>(true))
                    {
                        collider.enabled = false;
                    }
                }
            }
        }
    }
}
