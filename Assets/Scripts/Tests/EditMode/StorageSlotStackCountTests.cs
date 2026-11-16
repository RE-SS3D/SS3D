using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SS3D.Systems;
using SS3D.Systems.Inventory.Items;
using SS3D.UI.StoragePanel;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorTests
{
    /// <summary>
    /// Playtest 2026-11-16: dropping a floor tile onto a stack of them merged it, but the panel kept the old count until
    /// reopened. A merge changes only the stack's synced count, not the container's contents, so no contents event
    /// redraws the slot; the host re-reads counts each frame through <see cref="StorageSlot.RefreshStackCount"/>.
    /// </summary>
    public sealed class StorageSlotStackCountTests
    {
        private GameObject _itemObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_itemObject);
        }

        [Test]
        public void RefreshStackCount_ShowsACountThatChangedAfterBinding()
        {
            Item tiles = CreateStack(3);
            StorageSlot slot = new();
            slot.Bind(tiles);
            Assert.AreEqual("×3", StackLabel(slot).text);

            SetStackCount(tiles, 4);
            slot.RefreshStackCount();

            Assert.AreEqual(4, tiles.StackCount, "test setup: the stack holds four now");
            Assert.AreEqual("×4", StackLabel(slot).text);
        }

        private Item CreateStack(int count)
        {
            _itemObject = new GameObject("Tiles");
            Item item = _itemObject.AddComponent<Item>();
            item.Init("Tiles", 1f, new List<Trait>());
            SetField(item, "_maxStackSize", 10);
            SetStackCount(item, count);
            return item;
        }

        /// <summary>FishNet's weaver wraps the SyncVar; use its generated setter, as TileMapPhase1Tests does.</summary>
        private static void SetStackCount(Item item, int count)
        {
            MethodInfo setter = typeof(Item).GetMethod("sync___set_value__stackCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (setter != null)
            {
                setter.Invoke(item, new object[] { count, true });
                return;
            }

            SetField(item, "_stackCount", count);
        }

        private static Label StackLabel(StorageSlot slot) => slot.Q<Label>(className: "storage-slot__stack-count");

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }
    }
}
