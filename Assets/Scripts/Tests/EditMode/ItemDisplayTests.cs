using System.Reflection;
using NUnit.Framework;
using SS3D.Systems.Inventory.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EditorTests
{
    public class ItemDisplayTests
    {
        private HashSet<GameObject> _sceneRootsBeforeTest;

        [SetUp]
        public void SetUp()
        {
            _sceneRootsBeforeTest = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .ToHashSet();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!_sceneRootsBeforeTest.Contains(gameObject))
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }
        }

        [Test]
        public void CountLabelShowsOnlyForStackAmountsAboveOne()
        {
            GameObject displayObject = new("ItemDisplay");
            ItemDisplay display = displayObject.AddComponent<ItemDisplay>();
            display.ItemImage = new GameObject("ItemImage").AddComponent<Image>();
            display.ItemImage.transform.SetParent(displayObject.transform);

            Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Assert.NotNull(tmpType);

            Component countLabel = new GameObject("CountLabel").AddComponent(tmpType);
            countLabel.transform.SetParent(displayObject.transform);
            typeof(ItemDisplay)
                .GetField("_countLabel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(display, countLabel);

            display.Item = StackableTests.CreateStackableItem("Cable", 5).Item;

            Assert.False(countLabel.gameObject.activeSelf);

            display.Item = StackableTests.CreateStackableItem("Cable", 5, 3).Item;

            Assert.True(countLabel.gameObject.activeSelf);
            string text = (string)tmpType.GetProperty("text")?.GetValue(countLabel);
            Assert.AreEqual("3", text);
        }

        [Test]
        public void GridItemDropAcceptedRestoresVisibility()
        {
            GameObject displayObject = new("ItemGridItem");
            displayObject.AddComponent<RectTransform>();
            ItemGridItem display = displayObject.AddComponent<ItemGridItem>();
            Image image = new GameObject("ItemImage").AddComponent<Image>();
            image.transform.SetParent(displayObject.transform);

            display.MakeVisible(false);
            Assert.False(image.enabled);

            display.OnDropAccepted();

            Assert.True(image.enabled);
        }

        [Test]
        public void DropAcceptedReturnsDisplayToOriginalSlot()
        {
            GameObject slotObject = new("Slot");
            GameObject displayObject = new("ItemDisplay");
            RectTransform rectTransform = displayObject.AddComponent<RectTransform>();
            ItemDisplay display = displayObject.AddComponent<ItemDisplay>();

            Image image = new GameObject("ItemImage").AddComponent<Image>();
            image.transform.SetParent(displayObject.transform);

            displayObject.transform.SetParent(new GameObject("DragRoot").transform);
            display.OldPosition = new Vector3(12f, 8f, 0f);
            display.MakeVisible(false);
            typeof(ItemDisplay)
                .GetField("_oldParent", ReflectionFlags)
                ?.SetValue(display, slotObject.transform);

            display.OnDropAccepted();

            Assert.AreSame(slotObject.transform, displayObject.transform.parent);
            Assert.AreEqual(display.OldPosition, rectTransform.localPosition);
            Assert.True(image.enabled);
        }

        [UnityTest]
        public IEnumerator CountLabelUpdatesWhenContainerStackMergeRefreshesExistingGridItem()
        {
            AttachedContainer source = StackableTests.CreateContainer(new Vector2Int(1, 1));
            AttachedContainer destination = StackableTests.CreateContainer(new Vector2Int(1, 1));
            var sourceStack = StackableTests.CreateStackableItem("Cable", 5);
            var destinationStack = StackableTests.CreateStackableItem("Cable", 5);
            source.AddItem(sourceStack.Item);
            destination.AddItem(destinationStack.Item);

            GameObject gridObject = new("ItemGrid");
            ItemGrid grid = gridObject.AddComponent<ItemGrid>();
            grid.AttachedContainer = destination;
            grid.ItemDisplayPrefab = CreateDisplayPrefab();
            grid.ItemSlotPrefab = CreateSlotPrefab();

            GameObject gridLayoutObject = new("Grid");
            gridLayoutObject.transform.SetParent(gridObject.transform);
            GridLayoutGroup layout = gridLayoutObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(32, 32);
            UnityEngine.Object.Instantiate(grid.ItemSlotPrefab, gridLayoutObject.transform);

            typeof(ItemGrid)
                .GetField("_gridLayout", ReflectionFlags)
                ?.SetValue(grid, layout);

            typeof(ItemGrid)
                .GetMethod("CreateItemDisplay", ReflectionFlags)
                ?.Invoke(grid, new object[] { destinationStack.Item, Vector2Int.zero, false });

            Assert.True(source.TransferItemToOther(sourceStack.Item, Vector2Int.zero, destination));
            yield return null;

            TMP_TextLike label = FindCountLabel(gridObject);
            Assert.NotNull(label.Component);
            Assert.True(label.Component.gameObject.activeSelf);
            Assert.AreEqual("2", label.Text);
        }

        private const BindingFlags ReflectionFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static GameObject CreateDisplayPrefab()
        {
            GameObject displayObject = new("ItemDisplayPrefab");
            displayObject.AddComponent<RectTransform>();
            displayObject.AddComponent<Image>();
            ItemGridItem display = displayObject.AddComponent<ItemGridItem>();

            GameObject itemImageObject = new("ItemImage");
            itemImageObject.transform.SetParent(displayObject.transform);
            display.ItemImage = itemImageObject.AddComponent<Image>();

            Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Assert.NotNull(tmpType);
            Component countLabel = new GameObject("CountLabel").AddComponent(tmpType);
            countLabel.transform.SetParent(displayObject.transform);
            typeof(ItemDisplay)
                .GetField("_countLabel", ReflectionFlags)
                ?.SetValue(display, countLabel);

            return displayObject;
        }

        private static GameObject CreateSlotPrefab()
        {
            GameObject slotObject = new("Slot");
            slotObject.AddComponent<RectTransform>();
            return slotObject;
        }

        private static TMP_TextLike FindCountLabel(GameObject root)
        {
            Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Assert.NotNull(tmpType);
            foreach (Component component in root.GetComponentsInChildren(tmpType, true))
            {
                if (component.name == "CountLabel")
                {
                    return new TMP_TextLike(component, tmpType);
                }
            }

            return new TMP_TextLike(null, tmpType);
        }

        private readonly struct TMP_TextLike
        {
            private readonly Type _type;

            public TMP_TextLike(Component component, Type type)
            {
                Component = component;
                _type = type;
            }

            public Component Component { get; }

            public string Text => (string)_type.GetProperty("text")?.GetValue(Component);
        }
    }
}
