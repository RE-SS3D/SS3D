#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SS3D.Systems.Tile;
using SS3D.Tilemaps;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Editor.TileMap
{
    /**
     * Handles creating a dragging rect and converting that into real tiles in the scene.
     */
    public class TileDragHandler
    {
        public TileDragHandler(TileManager tileManager, TileMapEditor mapEditor, SS3D.Systems.Tile.TileMap map, int subLayerIndex, TileObjectSo tileObjectSo, Direction selectedDir, Vector3Int snappedPosition)
        {
            _tileManager = tileManager;
            _mapEditor = mapEditor;
            _map = map;
            _subLayerIndex = subLayerIndex;
            _tileObjectSo = tileObjectSo;
            _selectedDir = selectedDir;
            _startPosition = snappedPosition;
            _curPosition = snappedPosition;

            GameObject tile = CreateGhost();
            tile.transform.position = new Vector3(snappedPosition.x, 0, snappedPosition.y);
            _dragTiles.Insert(0, tile);
        }

        private GameObject CreateGhost()
        {
            GameObject ghostObject = (GameObject)PrefabUtility.InstantiatePrefab(_tileObjectSo.prefab);
            ghostObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ghostObject.name = "Ghost object";
            ghostObject.tag = "EditorOnly";
            ghostObject.transform.SetParent(_tileManager.transform);
            return ghostObject;
        }

        /**
         * Warning: Here be dragons.
         * 
         * This code handles creating the square showing a dragging rectangle of tiles on the screen
         * It has to deal with any number of rows or columns changing, even to the point where the rectangle may flip axes.
         * 
         * TODO: Deal with if the index is out-of-bounds.
         */
        public void HandleDrag(Vector3Int mousePosition)
        {
            // Don't need to update if the mouse hasn't moved
            if (_curPosition == mousePosition)
                return;

            Vector3Int prevPosition = _curPosition;

            // Change the number of columns and rows of tiles to match
            // Note: This is kinda insane.
            int rowStart = _startPosition.x;
            int rowEnd = prevPosition.x;
            int rowInc = rowStart <= rowEnd ? 1 : -1;
            int columnStart = _startPosition.y;
            int columnEnd = prevPosition.y;
            int columnInc = columnStart <= columnEnd ? 1 : -1;

            int rowLength = 1 + Math.Abs(rowEnd - rowStart);
            int columnLength = 1 + Math.Abs(columnEnd - columnStart);

            // Update columns
            int columnDiff = mousePosition.y - prevPosition.y; 

            if (columnDiff * columnInc < 0)
            {
                for (int i = rowLength - 1; i >= 0; i--)
                {
                    // Remove tiles from (and including) columnEnd down to (and exluding) columnEnd - columnDiff
                    int minColumn = Math.Max(columnLength - Math.Abs(columnDiff), 1);
                    for (int j = minColumn; j < columnLength; j++)
                    {
                        Object.DestroyImmediate(_dragTiles[columnLength * i + j].gameObject);
                    }
                    if (columnLength - minColumn > 0)
                        _dragTiles.RemoveRange(columnLength * i + minColumn, columnLength - minColumn);
                }

                // We need to add more columns in the negative direction,
                // so we trigger the next if with updated values
                if (columnLength - Math.Abs(columnDiff) <= 0)
                {
                    // We've already deleted all but one column, so:
                    columnEnd = columnStart;
                    columnLength = 1;
                    columnDiff = mousePosition.y - _startPosition.y;
                    // Flip the incrementor, causing columnDiff * columnInc > 0
                    columnInc = -columnInc;
                }
            }
            if (columnDiff * columnInc > 0)
            {
                for (int i = rowLength - 1; i >= 0; i--)
                { // Go backwards so we can add and remove from array without messing with indices
                  // Add tiles from (but excluding) columnEnd, up to (and including) columnEnd + columnDiff
                    for (int j = columnLength; j < columnLength + Math.Abs(columnDiff); j++)
                    {
                        GameObject tile = CreateGhost();
                        tile.transform.position = new Vector3(rowStart + i * rowInc, 0, columnStart + j * columnInc);
                        _dragTiles.Insert(columnLength * i + j, tile);
                    }
                }
            }

            columnEnd = Convert.ToInt32(mousePosition.y);
            columnInc = columnStart <= columnEnd ? 1 : -1;
            columnLength = 1 + Math.Abs(columnEnd - columnStart);

            // Now update rows
            int rowDiff = mousePosition.x - prevPosition.x;
            if (rowDiff * rowInc < 0)
            {
                int minRow = Math.Max(rowLength - Math.Abs(rowDiff), 1);

                for (int i = rowLength - 1; i >= minRow; i--)
                {
                    for (int j = 0; j < columnLength; j++)
                    {
                        Object.DestroyImmediate(_dragTiles[columnLength * i + j]);
                    }
                }
                if (rowLength - minRow > 0)
                    _dragTiles.RemoveRange(minRow * columnLength, (rowLength - minRow) * columnLength);

                // If we've crossed axes, we need to create more rows, so we manipulate values for the next if to trigger
                if (rowLength - Math.Abs(rowDiff) <= 0)
                {
                    // We've already deleted all but one row, so:
                    rowEnd = rowStart;
                    rowLength = 1;
                    rowDiff = mousePosition.x - _startPosition.x;

                    // Flip the incrementor, so that the code now things we're increasing rows
                    rowInc = -rowInc;
                }
            }
            if (rowDiff * rowInc > 0)
            {
                for (int i = rowLength; i < rowLength + Math.Abs(rowDiff); i++)
                {
                    for (int j = 0; j < columnLength; j++)
                    {
                        GameObject tile = CreateGhost();
                        tile.transform.position = new Vector3(rowStart + i * rowInc, 0, columnStart + j * columnInc);
                        _dragTiles.Insert(columnLength * i + j, tile);
                    }
                }
            }

            if (DeleteTiles)
                HideTiles();
            else
                ShowTiles();

            _curPosition = mousePosition;
        }

        /**
         * Destroys all temporary objects, and actually creates the tiles the drag ended on.
         */
        public void EndDrag()
        {
            CancelDrag();

            int xInc = _startPosition.x < _curPosition.x ? 1 : -1;
            int yInc = _startPosition.y < _curPosition.y ? 1 : -1;
            for (int x = _startPosition.x; x != _curPosition.x + xInc; x += xInc)
            {
                for (int y = _startPosition.y; y != _curPosition.y + yInc; y += yInc)
                {
                    if (DeleteTiles)
                    {
                        _tileManager.ClearTileObject(_map, SelectedLayer, 0, new Vector3(x, 0, y));
                    }
                    else
                    {
                        if (AllowOverwrite)
                            _tileManager.ClearTileObject(_map, SelectedLayer, 0, new Vector3(x, 0, y));

                        _tileManager.SetTileObject(_map, _subLayerIndex, _tileObjectSo, new Vector3(x, 0, y), _selectedDir);
                    }
                }
            }
        }

        /**
         * Destroys all temporary objects without creating the objects.
         */
        private void CancelDrag()
        {
            // Ensure the tiles are cleared no matter what. Otherwise some annoying stuff can happen.
            foreach (GameObject tiles in _dragTiles)
            {
                if (tiles && tiles)
                {
                    Object.DestroyImmediate(tiles);
                }
            }

            _dragTiles.Clear();
        }

        public List<GameObject> GetDragTiles()
        {
            return _dragTiles;
        }

        private void HideTiles()
        {
            foreach (GameObject tile in _dragTiles)
            {
                tile.SetActive(false);
            }
        }

        private void ShowTiles()
        {
            foreach (GameObject tile in _dragTiles)
            {
                tile.SetActive(true);
            }
        }

        private readonly TileManager _tileManager;
        private readonly TileMapEditor _mapEditor;
        private readonly SS3D.Systems.Tile.TileMap _map;
        private readonly int _subLayerIndex;
        private readonly TileObjectSo _tileObjectSo;
        private readonly Direction _selectedDir;
        private readonly Vector3Int _startPosition;
        private Vector3Int _curPosition;
        private readonly List<GameObject> _dragTiles = new();

        public bool DeleteTiles { get; set; }
        public bool AllowOverwrite { get; set; }
        public TileLayer SelectedLayer { get; set; }
    }

}
#endif