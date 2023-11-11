using Coimbra;
using UnityEngine;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Class for creating and managing ghost objects used by the TileMapCreator.
    /// </summary>
    public class GhostManager: Actor
    {
        public Material validConstruction;
        public Material invalidConstruction;
        public Material deleteConstruction;

        private GameObject _ghostObject;
        private Vector3 _targetPosition;

        public enum BuildMatMode
        {
            Valid,
            Invalid,
            Building,
            Deleting
        }

        public Direction Dir { get; private set; } = Direction.North;

        public void CreateGhost(GameObject prefab, Vector3 position)
        {
            if (_ghostObject != null) return;
            
            _ghostObject = Instantiate(prefab, position, Quaternion.identity);
            if (_ghostObject.TryGetComponent(out Rigidbody ghostRigidbody))
            {
                ghostRigidbody.useGravity = false;
                ghostRigidbody.isKinematic = true;
            }
            var colliders = _ghostObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
        }
        public void DestroyGhost()
        {
            if (_ghostObject == null) return;
            
            _ghostObject.Dispose(true);
            _ghostObject = null;
            
        }

        public void SetTargetPosition(Vector3 target)
        {
            // Small offset is added so that meshes don't overlap with already placed objects.
            _targetPosition = target + new Vector3(0, 0.1f, 0);
        }

        /// <summary>
        /// Moves a ghost object to the target position via a lerp over time.
        /// </summary>
        public void MoveGhost()
        {
            // Required if the object has a network script attached
            _ghostObject.SetActive(true);

            _ghostObject.transform.position = Vector3.Lerp(_ghostObject.transform.position, _targetPosition, Time.deltaTime * 15f);
            _ghostObject.transform.rotation = Quaternion.Lerp(_ghostObject.transform.rotation, Quaternion.Euler(0, TileHelper.GetRotationAngle(Dir), 0), Time.deltaTime * 15f);
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
                    ghostMat = validConstruction;
                    break;
                case BuildMatMode.Invalid:
                    ghostMat = invalidConstruction;
                    break;
                case BuildMatMode.Building:
                    break;
                case BuildMatMode.Deleting:
                    ghostMat = deleteConstruction;
                    break;
            }

            foreach (MeshRenderer mr in _ghostObject.GetComponentsInChildren<MeshRenderer>())
            {
                Material[] materials = mr.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = ghostMat;
                }

                mr.materials = materials;
            }
        }

        public void SetNextRotation()
        {
            Dir = TileHelper.GetNextCardinalDir(Dir);
        }
    }
}