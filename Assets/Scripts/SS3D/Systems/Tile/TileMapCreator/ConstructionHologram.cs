using Coimbra;
using SS3D.Data.Generated;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SS3D.Data;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Represent a hologram tile object used for construction, through the tilemap menu.
    /// </summary>
    public class ConstructionHologram
    {
        public GameObject Hologram;
        private Vector3 _targetPosition;
        private Direction _direction;
        private Quaternion _worldRestRotation;
        public Direction Direction => _direction;
        public bool ActiveSelf => Hologram.activeSelf;
        public bool SetActive { set => Hologram.SetActive(value); }
        public Vector3 TargetPosition { get => _targetPosition; set => _targetPosition = value; }
        public float PlacementYOffset { get; private set; }

        /// <summary>Yaw from direction composed with authored item rest pitch/roll.</summary>
        public Quaternion TargetWorldRotation =>
            SS3D.Systems.Inventory.Items.Item.ComposeWorldFacing(
                _worldRestRotation,
                TileHelper.GetRotationAngle(_direction));
        
        /// <summary>
        /// Build a new hologram
        /// </summary>
        /// <param name="ghostObject"> the game object we want to make a hologram from.</param>
        /// <param name="targetTargetPosition"> the initial position of the hologram in space.</param>
        /// <param name="dir"> the expected original direction. Note that not all directions are compatible with
        /// all tile objects. If it's not, it will choose another available direction.</param>
        public ConstructionHologram(GameObject ghostObject, Vector3 targetPosition, Direction dir, float placementYOffset = 0f, ConstructionMode mode = ConstructionMode.Valid)
        {

            DisableBehaviours(ghostObject);

            Hologram = ghostObject;
            _targetPosition = targetPosition;
            PlacementYOffset = placementYOffset;
            _direction = dir;
            // Capture authored root rest before CreateHologram overwrites with yaw-only (M4 -90° X, etc.).
            _worldRestRotation = ghostObject.transform.localRotation;

            if (ghostObject.TryGetComponent(out ICustomGhostRotation customRotationComponent) 
                && !customRotationComponent.GetAllowedRotations().Contains(dir))
            {
                _direction = customRotationComponent.DefaultDirection;
            }

            if (ghostObject.TryGetComponent(out Rigidbody ghostRigidbody))
            {
                ghostRigidbody.useGravity = false;
                ghostRigidbody.isKinematic = true;
            }
            var colliders = ghostObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            foreach (HologramOnly hologramOnly in ghostObject.GetComponentsInChildren<HologramOnly>(true))
            {
                hologramOnly.gameObject.SetActive(true);
            }

            ChangeHologramColor(mode);
        }

        private ConstructionMode _appliedMode = (ConstructionMode)(-1);

        /// <summary>
        /// Chooses which material to set on the ghost based on which mode we are building.
        /// </summary>
        public void ChangeHologramColor(ConstructionMode mode)
        {
            if (_appliedMode == mode)
                return;

            _appliedMode = mode;
            Material ghostMat = null;

            string ghostMatName = mode switch
            {
                ConstructionMode.Valid => Materials.ValidConstruction,
                ConstructionMode.Invalid => Materials.InvalidConstruction,
                ConstructionMode.Delete => Materials.DeleteConstruction,
                _ => null,
            };

            if (ghostMatName != null)
            {
                ghostMat = Assets.Get<Material>(AssetDatabases.Materials, ghostMatName);
            }

            foreach (MeshRenderer mr in Hologram.GetComponentsInChildren<MeshRenderer>())
            {
                Material[] materials = mr.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = ghostMat;
                }

                mr.materials = materials;
            }

            foreach (SkinnedMeshRenderer smr in Hologram.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Material[] materials = smr.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = ghostMat;
                }

                smr.materials = materials;
            }
        }

        /// <summary>
        /// Smoothly change rotation and position for better visual effects.
        /// </summary>
        public void UpdateRotationAndPosition()
        {
            // Small offset is added so that meshes don't overlap with already placed objects.
            Hologram.transform.position = Vector3.Lerp(
                Hologram.transform.position,
                _targetPosition + new Vector3(0, PlacementYOffset + 0.1f, 0),
                Time.deltaTime * 15f);
            Hologram.transform.rotation = Quaternion.Lerp(Hologram.transform.rotation, TargetWorldRotation, Time.deltaTime * 15f);
        }

        /// <summary>
        /// Set the next allowed rotation, depends on the tile object.
        /// </summary>
        public void SetNextRotation()
        {
            if (Hologram.TryGetComponent(out ICustomGhostRotation customRotationComponent))
            {
                _direction = customRotationComponent.GetNextDirection(_direction);
            }
            else
            {
                _direction = TileHelper.GetNextCardinalDir(_direction);
            }
        }

        public void Destroy()
        {
            Hologram.Dispose(true);
            Hologram = null;
        }

        private void DisableBehaviours(GameObject ghostObject)
        {
            List<MonoBehaviour> components = ghostObject.GetComponentsInChildren<MonoBehaviour>()
                .Where(x => x is not ICustomGhostRotation).ToList();

            components.ForEach(x => x.enabled = false);
        }
    }
}