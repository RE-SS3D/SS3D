using System.Collections.Generic;
using SS3D.Core;
using SS3D.Data;
using SS3D.Tilemaps;
using SS3D.Tilemaps.Enums;
using SS3D.Tilemaps.Objects;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Tilemaps
{
    public class TileEditorView : MonoBehaviour
    {
        [SerializeField] private TileObjects _selectedTileObject;

        [SerializeField] private GameObject _itemListParent;
        [SerializeField] private TileEditorItemView _listItemPrefab;

        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _openButton;

        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _editorGameObject;

        private void Start()
        {
            InitializeItemList();

            _closeButton.onClick.AddListener(() =>
            {
                ToggleView(false);
            });
            _openButton.onClick.AddListener(() =>
            {
                ToggleView(true);
            });

            ToggleView(false);
            _root.SetActive(false);
        }

        public void ToggleView(bool active)
        {
            _editorGameObject.SetActive(active);
            _openButton.gameObject.SetActive(!active);
        }

        private void InitializeItemList()
        {
            Dictionary<TileObjectLayer, List<TileObject>> assets = TileObjectSystem.AssetsPerLayer;

            foreach (KeyValuePair<TileObjectLayer, List<TileObject>> asset in assets)
            {
                foreach (TileObject tileObject in asset.Value)
                {
                    TileEditorItemView itemView = Instantiate(_listItemPrefab, _itemListParent.transform);

                    itemView.SetupLabel(tileObject.Id);
                    itemView.OnPressed.AddListener(() => { _selectedTileObject = tileObject.Id; });
                }
            }
        }
    }
}