using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using static Tile.TileMapEditorHelpers;


namespace SS3D.Engine.Tiles.Editor.TileMap
{
    /**
     * Stores and keeps up to date info about the set of tiles
     * used by the tilemap editor.
     * 
     * Creates ghost objects and editors for all objects, allowing them to
     * be inspected all the way down to subtile state.
     */
    public class TileSet
    {
        public List<TileDefinition> Definitions
        {
            get => savedSettings.tiles;
        }
        // Ghost tiles for each of the definitions.
        public List<TileObject> Objects { get; private set; } = new List<TileObject>();

        /**
         * Ensures that each list is up-to-date after potential updates.
         */
        public void Update(TileManager tileManager)
        {
            // Clean up tiles which where deleted in the editor
            // Debug.Log("Looping all tiles...");
            //foreach (TileObject tile in tileManager.GetAllTiles())
            //{
            //    if (tile.transform.childCount == 0)
            //    {
            //        tileManager.RemoveTile(tile);
            //        Debug.LogWarning("Deleting tiles which where not deleted correctly");
            //    }
            //}

            if (savedSettings == null)
            {
                savedSettings = TileMapEditorSettingsAsset.LoadFromAsset();
                serializedSettings = new SerializedObject(savedSettings);
            }
            else
            {
                AssetDatabase.SaveAssets();
                serializedSettings.Update();
            }

            // Ensure that all lists are the same size
            int difference = Definitions.Count - Objects.Count;
            if (difference > 0)
            {
                Objects.AddRange(Enumerable.Repeat<TileObject>(null, difference));
                Editors.AddRange(Enumerable.Repeat<TileObjectEditor>(null, difference));
            }
            else if (difference < 0)
            {
                for (int i = 0; i < -difference; ++i)
                {
                    Object.DestroyImmediate(Objects[Definitions.Count + i]);
                    Object.DestroyImmediate(Editors[Definitions.Count + i]);
                }

                Objects.RemoveRange(Definitions.Count, -difference);
                Editors.RemoveRange(Definitions.Count, -difference);
            }

            // Now ensure each object is correct
            for (int i = 0; i < Definitions.Count; ++i)
            {
                if (Objects[i] == null && !Definitions[i].IsEmpty())
                {
                    // Create an element in this place to match the definition
                    Objects[i] = CreateGhostTile(tileManager, Definitions[i], "(" + i + ")");
                    HideTile(i);

                    Editors[i] = (TileObjectEditor)UnityEditor.Editor.CreateEditor(Objects[i]);
                }
                else if (Objects[i] != null && Definitions[i].IsEmpty())
                {
                    // Remove the object as the definition no longer exists
                    Object.DestroyImmediate(Objects[i]);
                    Object.DestroyImmediate(Editors[i]);
                    Objects[i] = null;
                    Editors[i] = null;
                }
                else if (Objects[i] != null && !Objects[i].Tile.Equals(Definitions[i])) // Update the object
                {
                    FixturesContainer.ValidateFixtures(Definitions[i]);
                    Objects[i].Tile = Definitions[i];
                }
            }
        }

        public void Add()
        {
            TileDefinition def = new TileDefinition
            {
                fixtures = new FixturesContainer()
            };

            savedSettings.tiles.Add(def);
            Objects.Add(null);
            Editors.Add(null);
        }
        public void RemoveAt(int i)
        {
            savedSettings.tiles.RemoveAt(i);

            if (Objects[i] != null)
                Object.DestroyImmediate(Objects[i]);
            if (Editors[i] != null)
                Object.DestroyImmediate(Editors[i]);

            Objects.RemoveAt(i);
            Editors.RemoveAt(i);
        }

        public void Destroy()
        {
            savedSettings = null;
            serializedSettings = null;
            for (int i = 0; i < Objects.Count; ++i)
            {
                Object.DestroyImmediate(Objects[i]);
                Object.DestroyImmediate(Editors[i]);
            }
            Objects.Clear();
            Editors.Clear();
        }

        /**
         * Shows the inspector for the given definition.
         * Note: this.Update() should be called before or after this, at some point.
         */
        public void ShowInspectorFor(int i)
        {
            // Use the editor if it exists.
            // The only time it doesn't exist is if the object IsEmpty, in which case
            // it's fine to just show a PropertyField version.
            if (Editors[i] != null)
            {
                Editors[i].OnInspectorGUI();
                Definitions[i] = Objects[i].Tile;
                serializedSettings.Update();
            }
            else
            {
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("tiles").GetArrayElementAtIndex(i), true);
                serializedSettings.ApplyModifiedProperties();
            }
        }

        public void ShowTile(int i)
        {
            Objects[i].gameObject.SetActive(true);
        }
        public void HideTile(int i)
        {
            Objects[i].gameObject.SetActive(false);
            Objects[i].transform.position = new Vector3(0, -1, 0);
        }

        // Maintain a list of editors that are reused in this inspector to allow
        // user to modify subtile state.
        private List<TileObjectEditor> Editors { get; set; } = new List<TileObjectEditor>();
        private SerializedObject serializedSettings;
        private TileMapEditorSettingsAsset savedSettings;
    }
}