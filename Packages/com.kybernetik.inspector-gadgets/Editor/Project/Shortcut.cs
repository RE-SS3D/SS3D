// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2023 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets
{
    /// <summary>[Editor-Only] A link to another location in the project for easy navigation.</summary>
    [CreateAssetMenu(order = 25)]// Group with "Folder".
    [HelpURL(Strings.APIDocumentationURL + "/" + nameof(Shortcut))]
    public sealed class Shortcut : ScriptableObject
    {
        /************************************************************************************************************************/

        [SerializeField]
        private Object _Target;

        /// <summary>The destination object which this <see cref="Shortcut"/> leads to.</summary>
        public ref Object Target => ref _Target;

        /************************************************************************************************************************/

        /// <summary>Selects the <see cref="_Target"/> in the Project window.</summary>
        [Button]
        public void GoToTarget()
        {
            if (_Target == null)
                return;

            Selection.activeObject = null;

            EditorApplication.delayCall += () =>
            {
                // If the target is a folder, select an asset inside it if possible instead of selecting the folder.
                if (_Target is DefaultAsset)
                {
                    var path = AssetDatabase.GetAssetPath(_Target);
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        var files = Directory.GetFiles(path);
                        for (int i = 0; i < files.Length; i++)
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<Object>(files[i]);
                            if (asset != null)
                            {
                                Selection.activeObject = asset;
                                //EditorApplication.delayCall += () => Selection.activeObject = null;
                                return;
                            }
                        }
                    }
                }

                Selection.activeObject = _Target;
            };
        }

        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>
        /// If the asset is a <see cref="Shortcut"/> this method selects its <see cref="Target"/> and returns true.
        /// </summary>
        [UnityEditor.Callbacks.OnOpenAsset]
        private static bool HandleOpenEvent(int instanceID, int line)
        {
            if (Event.current == null)
                return false;

            var shortcut = EditorUtility.InstanceIDToObject(instanceID) as Shortcut;
            if (shortcut != null)
            {
                shortcut.GoToTarget();
                return true;
            }

            return false;
        }
#endif

        /************************************************************************************************************************/
    }
}

#endif
