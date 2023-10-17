using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core.Behaviours;
using SS3D.Core;

namespace SS3D.Systems.Selection {


    public class Selectable : Actor
    {

        [SerializeField] private MaterialPropertyBlock propertyBlock;

        protected override void OnStart()
        {

            Color32 color = Subsystems.Get<SelectionSystem>().RegisterSelectable(this);
            propertyBlock = new MaterialPropertyBlock();
            //Color32 color = new Color32((byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)255);
            //propertyBlock.SetColor("_SelectionColor", color);
            SetColorRecursively(this.gameObject, color, this);
            
        }

        private void SetColorRecursively(GameObject go, Color32 color, Selectable initial)
        {
            // If the gameobject is selectable in its own right, it will set its own color
            Selectable current = go.GetComponent<Selectable>();
            if (current != null && current != initial) return;

            // If the gameobject has renderers, add SelectionColor to their MaterialPropertyBlock.
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock MPB = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(MPB);
                MPB.SetColor("_SelectionColor", color);
                renderer.SetPropertyBlock(MPB);
            }

            // Call this for all children
            foreach (Transform child in go.transform)
            {
                SetColorRecursively(child.gameObject, color, initial);
            }

        }

        /*
        private void AddExaminablesRecursive(GameObject gameObject, IExaminable current = null)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            IExaminable old = current;
            current = gameObject.GetComponent<IExaminable>();

            if (current == null)
            {
                current = old;
            }
            else if (old != current)
            {
                GameObject examinable = ((MonoBehaviour)current).gameObject;
                if (identifiers.HasExaminable(examinable))
                {
                    return;
                }

                identifiers.AddExaminable(examinable);
            }

            if (current != null)
            {
                var meshFilter = gameObject.GetComponent<MeshFilter>();
                if (meshFilter)
                {
                    identifiers.AddMesh(meshFilter.sharedMesh, gameObject, ((MonoBehaviour)current).gameObject);
                }
                else
                {
                    var meshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                    if (meshRenderer)
                    {
                        var mesh = new Mesh();
                        meshRenderer.BakeMesh(mesh);
                        identifiers.AddMesh(mesh, gameObject, ((MonoBehaviour)current).gameObject);
                    }
                }
            }

            Transform objectTransform = gameObject.transform;
            int childCount = objectTransform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                AddExaminablesRecursive(objectTransform.GetChild(i).gameObject, current);
            }
        }
        */

    }
}