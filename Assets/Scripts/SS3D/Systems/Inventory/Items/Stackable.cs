using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Examine;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    [RequireComponent(typeof(Item))]
    public class Stackable : NetworkActor, IExaminable
    {
        [SerializeField]
        [SyncVar(hook = nameof(OnMaxStackChanged))]
        private int _maxStack = 10;

        [SerializeField]
        [SyncVar(hook = nameof(OnAmountChanged))]
        private int _amountInStack = 1;

        [SerializeField]
        private GameObject[] _stackVisualCopies;

        [SerializeField]
        private ExamineData _examineData;

        public int MaxStack => _maxStack;

        public int AmountInStack => _amountInStack;

        public bool IsFull => _amountInStack >= _maxStack;

        protected override void OnStart()
        {
            base.OnStart();
            UpdateVisuals();
        }

        private void OnValidate()
        {
            if (_maxStack < 2)
            {
                _maxStack = 2;
            }

            if (_amountInStack < 1)
            {
                _amountInStack = 1;
            }
            else if (_amountInStack > _maxStack)
            {
                _amountInStack = _maxStack;
            }
        }

        [Server]
        public void Init(int maxStack, int amountInStack)
        {
            _maxStack = maxStack;
            _amountInStack = amountInStack;
            UpdateVisuals();
        }

        public bool CanAddToStack(int amount)
        {
            return _amountInStack + amount <= _maxStack;
        }

        [Server]
        public int AddToStack(int amount)
        {
            int space = _maxStack - _amountInStack;
            int added = Mathf.Min(space, amount);
            _amountInStack = _amountInStack + added;
            UpdateVisuals();
            return amount - added;
        }

        [Server]
        public int RemoveFromStack(int amount)
        {
            int actualRemoved = Mathf.Min(_amountInStack - 1, amount);
            _amountInStack = _amountInStack - actualRemoved;
            UpdateVisuals();
            return actualRemoved;
        }

        [Server]
        public void SetAmountInStack(int amount)
        {
            _amountInStack = Mathf.Clamp(amount, 1, _maxStack);
            UpdateVisuals();
        }

        public bool IsSameTypeAs(Stackable other)
        {
            if (other == null)
            {
                return false;
            }

            Item thisItem = GetComponent<Item>();
            Item otherItem = other.GetComponent<Item>();

            if (thisItem == null || otherItem == null)
            {
                return false;
            }

            if (thisItem.Asset != null && otherItem.Asset != null)
            {
                return thisItem.Asset == otherItem.Asset;
            }

            if (thisItem.Prefab != null && otherItem.Prefab != null)
            {
                return thisItem.Prefab == otherItem.Prefab;
            }

            Log.Warning(this, $"IsSameTypeAs falling back to name comparison for {thisItem.Name} — Asset or Prefab reference was null on one side");

            return thisItem.Name == otherItem.Name;
        }

        private void OnAmountChanged(int oldValue, int newValue, bool asServer)
        {
            UpdateVisuals();
        }

        private void OnMaxStackChanged(int oldValue, int newValue, bool asServer)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            for (int i = 0; i < _stackVisualCopies.Length; i++)
            {
                if (_stackVisualCopies[i] != null)
                {
                    _stackVisualCopies[i].SetActive(i < _amountInStack - 1);
                }
            }
        }

        public ExamineData GetData()
        {
            return _examineData;
        }
    }
}
