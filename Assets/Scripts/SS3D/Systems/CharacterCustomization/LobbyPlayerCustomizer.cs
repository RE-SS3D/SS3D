using System.Linq;
using SS3D.Systems.Clothing;
using SS3D.Systems.Entities;
using SS3D.Systems.Storage;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.CharacterCustomization
{
    public class LobbyPlayerCustomizer : MonoBehaviour
    {
        [FormerlySerializedAs("_clothingAssetCollection")] [SerializeField] private ClothingAssetCollection _clothingCollection;

        [FormerlySerializedAs("_previewDummyPrefab")] [SerializeField] private PlayerControllable _dummyPrefab;
        [FormerlySerializedAs("_previewDummyStartPoint")] [SerializeField] private Transform _dummyStartPoint;

        [FormerlySerializedAs("_DummyInstance")] [FormerlySerializedAs("_previewDummyInstance")] [SerializeField] private PlayerControllable _dummyInstance;

        private ClothingAsset _selectedClothingAsset;

        private void Start()
        {
            _dummyInstance = Instantiate(_dummyPrefab, _dummyStartPoint);
        }



        #region TEMP
        [ContextMenu("Add clothing to player")]
        private void TestClothing()
        {
            // hardcoded for now
            AddSelectedClothing(_clothingCollection.ClothingAssets[0].ClothingObject[0]);
        }

        private void AddSelectedClothing(ClothingObject clothingObject)
        {
            _dummyInstance.GetComponent<EntityInventory>().GetSlots<ClothingSlot>().ToList()[0].AddClothing(clothingObject);
        }
        #endregion
    }
}