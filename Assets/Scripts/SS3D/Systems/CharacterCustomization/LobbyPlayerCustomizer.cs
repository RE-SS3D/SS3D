using System.Linq;
using SS3D.Systems.Clothing;
using SS3D.Systems.Entities;
using SS3D.Systems.Storage;
using UnityEngine;

namespace SS3D.Systems.CharacterCustomization
{
    public class LobbyPlayerCustomizer : MonoBehaviour
    {
        [SerializeField] private ClothingAssetCollection _clothingAssetCollection;

        [SerializeField] private PlayerControllable _previewDummyPrefab;
        [SerializeField] private Transform _previewDummyStartPoint;

        [SerializeField] private PlayerControllable _previewDummyInstance;

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
            _previewDummyInstance.GetComponent<EntityInventory>().GetSlots<ClothingSlot>().ToList()[0].AddClothing(clothingObject);
        }
    }
}