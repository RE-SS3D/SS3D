using Coimbra;
using SS3D.Data;
using SS3D.Data.Enums;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        public Direction Direction => _direction;
        public bool ActiveSelf => Hologram.activeSelf;
        public bool SetActive { set => Hologram.SetActive(value); }
        public Vector3 TargetPosition { get => _targetPosition; set => _targetPosition = value; }
        
        /// <summary>
        /// Build a new hologram
        /// </summary>
        /// <param name="ghostObject"> the game object we want to make a hologram from.</param>
        /// <param name="targetTargetPosition"> the initial position of the hologram in space.</param>
        /// <param name="dir"> the expected original direction. Note that not all directions are compatible with
        /// all tile objects. If it's not, it will choose another available direction.</param>
        public ConstructionHologram(GameObject ghostObject, Vector3 targetTargetPosition, Direction dir, ConstructionMode mode = ConstructionMode.Valid)
        {
            List<MonoBehaviour> components = ghostObject.GetComponentsInChildren<MonoBehaviour>()
                .Where(x => x is not ICustomGhostRotation).ToList();

            components.ForEach(x => x.enabled = false);

            Hologram = ghostObject;
            _targetPosition = targetTargetPosition;
            _direction = dir;

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
            ChangeHologramColor(mode);
        }

        /// <summary>
        /// Chooses which material to set on the ghost based on which mode we are building.
        /// </summary>
        /// <param name="mode"></param>
        public void ChangeHologramColor(ConstructionMode mode)
        {
            Material ghostMat = null;

            switch (mode)
            {
                case ConstructionMode.Valid:
                    ghostMat = Assets.Get<Material>((int)AssetDatabases.Materials, (int)MaterialsIds.ValidConstruction);
                    break;

                case ConstructionMode.Invalid:
                    ghostMat = Assets.Get<Material>((int)AssetDatabases.Materials, (int)MaterialsIds.InvalidConstruction);
                    break;

                case ConstructionMode.Delete:
                    ghostMat = Assets.Get<Material>((int)AssetDatabases.Materials, (int)MaterialsIds.DeleteConstruction);
                    break;
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

            foreach (SkinnedMeshRenderer smr in _hologram.GetComponentsInChildren<SkinnedMeshRenderer>())
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
            Hologram.transform.position = Vector3.Lerp(Hologram.transform.position, _targetPosition + new Vector3(0, 0.1f, 0), Time.deltaTime * 15f);
            Hologram.transform.rotation = Quaternion.Lerp(Hologram.transform.rotation, Quaternion.Euler(0, TileHelper.GetRotationAngle(_direction), 0), Time.deltaTime * 15f);
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
    }
}