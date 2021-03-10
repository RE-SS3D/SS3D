using SS3D.Engine.Tiles;
using SS3D.Engine.Tiles.State;
using System.Collections.Generic;
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
            if (tileDefinition.fixtures != null)
            {
                FixturesContainer f = (FixturesContainer)tileDefinition.fixtures.Clone();
                tileDefinition.fixtures = f;
            }

            if (tileManager.GetTile(x, y) == null)
            {
                // Create a new tile, but only if the plenum is not empty
                if (tileDefinition.plenum == null)
                    return;

                tileManager.EditorCreateTile(x, y, tileDefinition);
                Undo.RegisterCreatedObjectUndo(tileManager.GetTile(x, y).gameObject, "Created tile");
            }
            else
            {
                // Save old definition
                TileDefinition oldDefinition = tileManager.GetTile(x, y).Tile;
                // Copy object to avoid dupplication between editor and tilemap
                if (oldDefinition.fixtures != null)
                {
                    FixturesContainer f = (FixturesContainer)oldDefinition.fixtures.Clone();
                    oldDefinition.fixtures = f;
                }


                // Existing tile found. We try to update the non-null items in the tiledefinition
                List<TileBase> tileBases = GetTileItems(tileDefinition);
                for (int i = 0; i < tileBases.ToArray().Length; i++)
                {
                    if (tileBases[i] != null)
                    {
                        oldDefinition = SetTileItem(oldDefinition, tileBases[i], i);
                    }
                }

                Undo.RecordObject(tileManager.GetTile(x, y).gameObject, "Updated tile");
                tileManager.EditorUpdateTile(x, y, oldDefinition);
            }
        }

        public static void SetFixtureRotation(TileObject tileObject, int fixtureIndex, Rotation rotation)
        {
            if (tileObject == null)
                return;


            GameObject fixtureObject = tileObject.GetLayer(fixtureIndex);

            if (fixtureObject != null)
            {
                


                FixtureStateMaintainer maintainer = fixtureObject.GetComponent<FixtureStateMaintainer>();
                if (maintainer != null)
                {
                    var fixtureSerial = new SerializedObject(maintainer);
                    fixtureSerial.Update();

                    SerializedProperty property = fixtureSerial.FindProperty("tileState");
                    property.FindPropertyRelative("rotation").intValue = (int)rotation;
                    fixtureSerial.ApplyModifiedProperties();

                    //var stateNow = maintainer.TileState;
                    
                    //stateNow.rotation = rotation;
                    //maintainer.SetTileState(stateNow);

                    // Refresh the subdata because it still has the old tilestate
                    tileObject.RefreshSubData();
                }
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

        public static List<TileBase> GetTileItems(TileDefinition tileDefinition)
        {
            List<TileBase> items = new List<TileBase>();

            items.Add(tileDefinition.plenum);
            items.Add(tileDefinition.turf);
            items.AddRange(tileDefinition.fixtures.GetAllFixtures());

            return items;
        }

        public static TileDefinition SetTileItem(TileDefinition tileDefinition, TileBase item, int index)
        {
            TileDefinition def = tileDefinition;

            if (index == 0)
                def.plenum = (Plenum)item;
            else if (index == 1)
                def.turf = (Turf)item;
            
            // We are a fixture
            else if (index > 1 && index < (2 + TileDefinition.GetAllFixtureLayerSize()))
            {
                def.fixtures.SetFixtureAtIndex((Fixture)item, index - 2);
            }
            else
            {
                Debug.LogWarning("Out of range tile item was tried to be set");
            }

            return def;
        }


#endif
    }
}
