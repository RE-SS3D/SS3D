using SS3D.Engine.Tiles;
using UnityEditor;
using UnityEngine;

namespace Tile
{
    /**
     * Helper methods used in the tilemap editor.
     * Aren't in the editor namespace because they are used by components that aren't editor-only
     */
    public static class TileMapEditorHelpers
    {
        public static bool IsGhostTile(TileObject obj)
        {
            return obj?.gameObject.tag == "EditorOnly";
        }

#if UNITY_EDITOR
        /**
         * Creates a fake tile to use in-editor
         */
        public static TileObject CreateGhostTile(TileManager manager, TileDefinition definition, string append = "")
        {
            // Create the tile
            var gameObject = new GameObject("Ghost Tile" + append);
            var tile = gameObject.AddComponent<TileObject>();
            tile.Tile = definition;

            var meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var mesh in meshes) {
                mesh.sharedMaterial.color = mesh.sharedMaterial.color * new Color(1.0f, 1.0f, 1.0f, 0.5f);
            }

            gameObject.transform.SetParent(manager.transform);
            gameObject.tag = "EditorOnly";

            return tile;
        }

       public static void DeleteTile(TileManager tileManager, int x, int y)
        {
            if (tileManager.GetTile(x, y) != null)
            {
                Undo.RegisterCreatedObjectUndo(tileManager.GetTile(x, y).gameObject, "Deleted tile");
                tileManager.EditorDestroyTile(x, y);

            }
        }


        /**
         * Sets the tile at the given position
         * to the currently selected tile type.
         * 
         * Will create the tile if one does not exist
         */
        public static void SetTile(TileManager tileManager, TileDefinition tileDefinition, int x, int y)
        {
            // Copy object to avoid dupplication between editor and tilemap
            FixturesContainer f = (FixturesContainer)tileDefinition.fixtures.Clone();
            tileDefinition.fixtures = f;

            if (tileManager.GetTile(x, y) == null)
            {

                tileManager.EditorCreateTile(x, y, tileDefinition);
                Undo.RegisterCreatedObjectUndo(tileManager.GetTile(x, y).gameObject, "Created tile");
            }
            else
            {
                Undo.RecordObject(tileManager.GetTile(x, y).gameObject, "Updated tile");
                tileManager.EditorUpdateTile(x, y, tileDefinition);
            }
        }

        public static void DestroyAllGhosts(TileManager tileManager)
        {
            if(!tileManager)
                return;

            for (int i = tileManager.transform.childCount - 1; i >= 0; --i) {
                if (tileManager.transform.GetChild(i).tag == "EditorOnly")
                    Object.DestroyImmediate(tileManager.transform.GetChild(i).gameObject);
            }
        }
#endif
    }
}
