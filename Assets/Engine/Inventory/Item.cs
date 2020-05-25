using System.Collections.Generic;
using System.Linq;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;
using UnityEditor;
using SS3D.Engine.Utilities;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif

namespace SS3D.Engine.Inventory
{
    /**
     * An item describes what is held in a container.
     */
    [DisallowMultipleComponent]
    public class Item : InteractionSourceNetworkBehaviour
    {
        // Distinguishes what can go in what slot
        public enum ItemType
        {
            Other,
            Hat,
            Glasses,
            Mask,
            Earpiece,
            Shirt,
            OverShirt,
            Gloves,
            Shoes
        }

        public string ItemId;
        public Container container;
        public ItemType itemType;
        public Sprite sprite;
        public GameObject prefab;
        public Transform attachmentPoint;

        [ContextMenu("Create Icon")]
        private void Start()
        {
            GenerateNewIcon();
        }
        public void GenerateNewIcon()
        {
            RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
            RuntimePreviewGenerator.OrthographicMode = true;

            Texture2D texture = RuntimePreviewGenerator.GenerateModelPreview(this.transform, 128, 128, false);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
            sprite.name = transform.name;
        }

        public override IInteraction[] GenerateInteractions(IInteractionTarget[] targets)
        {
            List<IInteraction> interactions = base.GenerateInteractions(targets).ToList();
            interactions.Add(new DropInteraction());
            return interactions.ToArray();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Don't even have to check without attachment
            if (attachmentPoint == null)
            {
                return;
            }

            // Make sure gizmo only draws in prefab mode
            if (EditorApplication.isPlaying || PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                return;
            }


            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Quaternion localRotation = attachmentPoint.localRotation;
                Vector3 eulerAngles = localRotation.eulerAngles;
                Vector3 parentPosition = attachmentPoint.parent.position;
                Vector3 position = attachmentPoint.localPosition;
                // Draw a wire mesh of the rotated model
                Vector3 rotatedPoint = RotatePointAround(parentPosition, position, eulerAngles);
                rotatedPoint += new Vector3(0, position.z, position.y);
                Gizmos.DrawWireMesh(meshFilter.sharedMesh,
                    rotatedPoint, localRotation);
            }
        }

        private Vector3 RotatePointAround(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot);
        }

#endif
    }
}