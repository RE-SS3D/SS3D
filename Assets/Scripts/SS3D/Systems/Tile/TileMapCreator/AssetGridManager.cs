using System;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Component, that changes number of columns in GridLayoutGroup to fit it's width. Elements of the group will take as much width as possible, but won't exceed it. 
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class AssetGridManager : MonoBehaviour
    {
        private GridLayoutGroup _grid;
        private float _cellWidth;
        private float _paddingWidth;
        private void Start()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _cellWidth = _grid.cellSize.x;
            _paddingWidth = _grid.spacing.x;
        }

        private void Update()
        {
            float width = transform.parent.GetComponent<RectTransform>().rect.width;
            int constraintCount = Convert.ToInt32(Math.Floor(width / (_cellWidth + _paddingWidth)));

            if (constraintCount != _grid.constraintCount)
                _grid.constraintCount = constraintCount;
        }
    }
}