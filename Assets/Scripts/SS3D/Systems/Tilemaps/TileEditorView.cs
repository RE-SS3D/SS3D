using System;
using System.Collections.Generic;
using SS3D.Core;
using SS3D.Data;
using SS3D.Tilemaps;
using SS3D.Tilemaps.Objects;
using UnityEngine;

namespace SS3D.Systems.Tilemaps
{
    public class TileEditorView : MonoBehaviour
    {
        [SerializeField] private TileObjects _selectedTileObject;

        [SerializeField] private GameObject _itemListParent;
        [SerializeField] private TileEditorItemView _listItemPrefab;

        private void Start()
        {
            InitializeItemList();
        }

        private void InitializeItemList()
        {
            Dictionary<TileLayer,List<TileObject>> assets = TileObjectSystem.AssetsPerLayer;

            foreach (KeyValuePair<TileLayer,List<TileObject>> asset in assets)
            {
                foreach (TileObject tileObject in asset.Value)
                {
                    TileEditorItemView itemView = Instantiate(_listItemPrefab, _itemListParent.transform);

                    itemView.SetupLabel(tileObject.Id);
                    itemView.OnPressed.AddListener(() =>
                    {
                        _selectedTileObject = tileObject.Id;
                    });
                }
            }
        }
    }
}