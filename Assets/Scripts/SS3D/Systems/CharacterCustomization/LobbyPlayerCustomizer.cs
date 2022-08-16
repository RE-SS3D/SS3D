using System.Linq;
using SS3D.Systems.Clothing;
using SS3D.Systems.Entities;
using UnityEngine;

namespace SS3D.Systems.CharacterCustomization
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
            _previewDummyInstance.Inventory.GetSlots<ClothingSlot>().ToList()[0].AddClothing(clothingObject);
        }
    }
}