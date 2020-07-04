﻿using System.Collections.Generic;
using System.Linq;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;
using UnityEditor;
using SS3D.Engine.Utilities;
using System;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif

namespace SS3D.Engine.Inventory
{
    /**
     * An item describes what is held in a container.
     */
    [DisallowMultipleComponent]
    public class Item : InteractionSourceNetworkBehaviour, IInteractionTarget
    {
        public string ItemId;
        public string Name;
        public float Volume = 10f;
        [HideInInspector]public Container container;
        public Sprite sprite;
        public GameObject prefab;
        public Transform attachmentPoint;
        public BulkSize bulkSize = BulkSize.Medium;
        public List<Trait> traits;

        [ContextMenu("Create Icon")]
        public void Start()
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
        
        public override void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            base.CreateInteractions(targets, interactions);
            DropInteraction dropInteraction = new DropInteraction();
            interactions.Add(new InteractionEntry(null, dropInteraction));
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
        public virtual IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new PickupInteraction { icon = sprite } };
        }

        public bool InContainer()
        {
            return container != null;
        }

        public bool HasTrait(Trait trait)
        {
            return traits.Contains(trait);
        }

        public bool HasTrait(string name)
        {
            var hash = Animator.StringToHash(name.ToUpper());
            foreach (Trait trait in traits)
            {
                if (trait.Hash == hash)
                    return true;
            }
            return false;
        }
    }
}