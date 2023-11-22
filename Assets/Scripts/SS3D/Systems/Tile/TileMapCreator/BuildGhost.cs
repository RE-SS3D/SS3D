using Coimbra;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{

    public enum BuildMatMode
    {
        Valid,
        Invalid,
        Delete
    }

    public class BuildGhost
    {
        private GameObject _ghost;
        private Vector3 _position;
        private Direction _direction;

        public Direction Direction => _direction;

        public bool ActiveSelf => _ghost.activeSelf;

        public bool SetActive { set => _ghost.SetActive(value); }

        public Vector3 Position { get => _position; set => _position = value; }


        public BuildGhost(GameObject ghostObject, Vector3 targetPosition, Direction dir)
        {
            _ghost = ghostObject;
            _position = targetPosition;
            _direction = dir;

            if (ghostObject.TryGetComponent(out ICustomGhostRotation customRotationComponent) 
                && !customRotationComponent.GetAllowedRotations().Contains(dir))
            {
                _direction = customRotationComponent.DefaultDirection;
            }

            if (ghostObject.TryGetComponent<Rigidbody>(out var ghostRigidbody))
            {
                ghostRigidbody.useGravity = false;
                ghostRigidbody.isKinematic = true;
            }
            var colliders = ghostObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
        }

        /// <summary>
        /// Chooses which material to set on the ghost based on which mode we are building.
        /// </summary>
        /// <param name="mode"></param>
        public void ChangeGhostColor(BuildMatMode mode)
        {
            Material ghostMat = null;

            switch (mode)
            {
                case BuildMatMode.Valid:
                    ghostMat = Assets.Get<Material>((int)AssetDatabases.Materials, (int)MaterialsIds.ValidConstruction);
                    break;

                case BuildMatMode.Invalid:
                    ghostMat = Assets.Get<Material>((int)AssetDatabases.Materials, (int)MaterialsIds.InvalidConstruction);
                    break;

                case BuildMatMode.Delete:
                    ghostMat = Assets.Get<Material>((int)AssetDatabases.Materials, (int)MaterialsIds.DeleteConstruction);
                    break;
            }


            foreach (MeshRenderer mr in _ghost.GetComponentsInChildren<MeshRenderer>())
            {
                Material[] materials = mr.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = ghostMat;
                }

                mr.materials = materials;
            }
        }

        public void UpdateRotationAndPosition()
        {
            // Small offset is added so that meshes don't overlap with already placed objects.
            _ghost.transform.position = Vector3.Lerp(_ghost.transform.position, _position + new Vector3(0, 0.1f, 0), Time.deltaTime * 15f);
            _ghost.transform.rotation = Quaternion.Lerp(_ghost.transform.rotation, Quaternion.Euler(0, TileHelper.GetRotationAngle(_direction), 0), Time.deltaTime * 15f);
        }

        public void SetNextRotation()
        {
            if (_ghost.TryGetComponent(out ICustomGhostRotation customRotationComponent))
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
            _ghost.Dispose(true);
        }
    }
}