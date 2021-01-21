using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Tile.TileMapEditorHelpers;

namespace SS3D.Engine.Tiles.Editor.TileMap
{
    /**
     * Handles creating a dragging rect and converting that into real tiles in the scene.
     */
    public class TileDragHandler
    {
        public TileDragHandler(TileManager tileManager, TileDefinition tileDefinition, Vector2Int mousePosition)
        {
            this.tileManager = tileManager;
            this.tileDefinition = tileDefinition;
            startPosition = mousePosition;
            curPosition = mousePosition;

            var tile = CreateGhostTile(tileManager, tileDefinition, " [" + mousePosition.x.ToString() + ", " + mousePosition.y.ToString() + "]");
            tile.transform.position = tileManager.GetPosition(mousePosition.x, mousePosition.y);
            dragTiles.Insert(0, tile);
        }

        /**
         * Warning: Here be dragons.
         * 
         * This code handles creating the square showing a dragging rectangle of tiles on the screen
         * It has to deal with any number of rows or columns changing, even to the point where the rectangle may flip axes.
         * 
         * TODO: Deal with if the index is out-of-bounds.
         */
        public void HandleDrag(Vector2Int mousePosition)
        {
            // Don't need to update if the mouse hasn't moved
            if (curPosition == mousePosition)
                return;

            var prevPosition = curPosition;

            // Change the number of columns and rows of tiles to match
            // Note: This is kinda insane.
            int rowStart = startPosition.x;
            int rowEnd = prevPosition.x;
            int rowInc = rowStart <= rowEnd ? 1 : -1;
            int columnStart = startPosition.y;
            int columnEnd = prevPosition.y;
            int columnInc = columnStart <= columnEnd ? 1 : -1;

            int rowLength = 1 + Math.Abs(rowEnd - rowStart);
            int columnLength = 1 + Math.Abs(columnEnd - columnStart);

            // Update columns
            int columnDiff = mousePosition.y - prevPosition.y;

            if (columnDiff * columnInc < 0) {
                for (int i = rowLength - 1; i >= 0; i--) {
                    // Remove tiles from (and including) columnEnd down to (and exluding) columnEnd - columnDiff
                    int minColumn = Math.Max(columnLength - Math.Abs(columnDiff), 1);
                    for (int j = minColumn; j < columnLength; j++) {
                        GameObject.DestroyImmediate(dragTiles[columnLength * i + j].gameObject);
                    }
                    if (columnLength - minColumn > 0)
                        dragTiles.RemoveRange(columnLength * i + minColumn, columnLength - minColumn);
                }

                // We need to add more columns in the negative direction,
                // so we trigger the next if with updated values
                if (columnLength - Math.Abs(columnDiff) <= 0) {
                    // We've already deleted all but one column, so:
                    columnEnd = columnStart;
                    columnLength = 1;
                    columnDiff = mousePosition.y - startPosition.y;
                    // Flip the incrementor, causing columnDiff * columnInc > 0
                    columnInc = -columnInc;
                }
            }
            if (columnDiff * columnInc > 0) {
                for (int i = rowLength - 1; i >= 0; i--) { // Go backwards so we can add and remove from array without messing with indices
                                                           // Add tiles from (but excluding) columnEnd, up to (and including) columnEnd + columnDiff
                    for (int j = columnLength; j < columnLength + Math.Abs(columnDiff); j++) {
                        var tile = CreateGhostTile(tileManager, tileDefinition, " [" + (rowStart + i * rowInc).ToString() + ", " + (columnStart + j * columnInc).ToString() + "]");
                        tile.transform.position = tileManager.GetPosition(rowStart + i * rowInc, columnStart + j * columnInc);

                        dragTiles.Insert(columnLength * i + j, tile);
                    }
                }
            }

            columnEnd = Convert.ToInt32(mousePosition.y);
            columnInc = columnStart <= columnEnd ? 1 : -1;
            columnLength = 1 + Math.Abs(columnEnd - columnStart);

            // Now update rows
            int rowDiff = mousePosition.x - prevPosition.x;
            if (rowDiff * rowInc < 0) {
                int minRow = Math.Max(rowLength - Math.Abs(rowDiff), 1);

                for (int i = rowLength - 1; i >= minRow; i--) {
                    for (int j = 0; j < columnLength; j++) {
                        GameObject.DestroyImmediate(dragTiles[columnLength * i + j].gameObject);
                    }
                }
                if (rowLength - minRow > 0)
                    dragTiles.RemoveRange(minRow * columnLength, (rowLength - minRow) * columnLength);

                // If we've crossed axes, we need to create more rows, so we manipulate values for the next if to trigger
                if (rowLength - Math.Abs(rowDiff) <= 0) {
                    // We've already deleted all but one row, so:
                    rowEnd = rowStart;
                    rowLength = 1;
                    rowDiff = mousePosition.x - startPosition.x;

                    // Flip the incrementor, so that the code now things we're increasing rows
                    rowInc = -rowInc;
                }
            }
            if (rowDiff * rowInc > 0) {
                for (int i = rowLength; i < rowLength + Math.Abs(rowDiff); i++) {
                    for (int j = 0; j < columnLength; j++) {
                        var tile = CreateGhostTile(tileManager, tileDefinition, " [" + (rowStart + i * rowInc).ToString() + ", " + (columnStart + j * columnInc).ToString() + "]");
                        tile.transform.position = tileManager.GetPosition(rowStart + i * rowInc, columnStart + j * columnInc);
                        dragTiles.Insert(columnLength * i + j, tile);
                    }
                }
            }

            curPosition = mousePosition;
        }

        /**
         * Destroys all temporary objects, and actually creates the tiles the drag ended on.
         */
        public void EndDrag()
        {
            CancelDrag();

            int xInc = startPosition.x < curPosition.x ? 1 : -1;
            int yInc = startPosition.y < curPosition.y ? 1 : -1;
            for (int x = startPosition.x; x != curPosition.x + xInc; x += xInc) {
                for (int y = startPosition.y; y != curPosition.y + yInc; y += yInc)
                {
                    if (deleteTiles)
                        DeleteTile(tileManager, x, y);
                    else
                        SetTile(tileManager, tileDefinition, x, y);
                }
            }
        }

        /**
         * Destroys all temporary objects without creating the objects.
         */
        public void CancelDrag()
        {
            // Ensure the tiles are cleared no matter what. Otherwise some annoying stuff can happen.
            for (int i = 0; i < dragTiles.Count; ++i) {
                if(dragTiles[i] && dragTiles[i].gameObject)
                    UnityEngine.Object.DestroyImmediate(dragTiles[i].gameObject);
            }
            dragTiles.Clear();
        }

        public void SetDelete()
        {
            deleteTiles = true;
        }

        private readonly TileManager tileManager;
        private readonly TileDefinition tileDefinition;
        private readonly Vector2Int startPosition;
        private Vector2Int curPosition;
        private List<TileObject> dragTiles = new List<TileObject>();

        private bool deleteTiles;
    }

}
    