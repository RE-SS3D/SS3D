using SS3D.Core.Systems.Entities;
using SS3D.Core.Systems.Inventory.Subsystem.Clothing;
using SS3D.Core.Systems.Inventory.Subsystem.Clothing.Items;
using SS3D.Systems.Inventory.Subsystem.Clothing;
using SS3D.Systems.Inventory.Subsystem.Clothing.Items;
using UnityEngine;

namespace SS3D.Systems.Lobby.CharacterCustomization
{
    public class LobbyPlayerCustomizer : MonoBehaviour
    {
        [SerializeField] private ClothingAssetCollection _clothingAssetCollection;

        [SerializeField] private Entity _previewDummyPrefab;
        [SerializeField] private Transform _previewDummyStartPoint;

        [SerializeField] private Entity _previewDummyInstance;

        private ClothingObject _selectedClothingObject;

        private void Start()
        {
            _previewDummyInstance = Instantiate(_previewDummyPrefab, _previewDummyStartPoint);
        }

        [ContextMenu("Add clothing to player")]
        private void TestClothing()
        {
            // hardcoded for now
            AddSelectedClothing(_clothingAssetCollection.ClothingAssets[0].ClothingObject[0]);
        }

        private void AddSelectedClothing(ClothingObject clothingObject)
        {
            _previewDummyInstance.Inventory.GetSlots<ClothingSlot>()[0].AddClothing(clothingObject);
        }
    }
}