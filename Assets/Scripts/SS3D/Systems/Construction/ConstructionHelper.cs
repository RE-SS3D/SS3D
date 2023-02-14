using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Construction.UI
{
    public class ConstructionHelper: Actor
    {
        public Material validConstruction;
        public Material invalidConstruction;
        public Material deleteConstruction;

        private GameObject _ghostObject;
        private Vector3 _targetPosition;
        private Direction _dir = Direction.North;

        public enum BuildMatMode
        {
            Valid,
            Invalid,
            Building,
            Deleting
        }

        public void CreateGhost(GameObject prefab)
        {
            if (_ghostObject == null)
            {
                _ghostObject = Instantiate(prefab);

                var collider = _ghostObject.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }
        public void DestroyGhost()
        {
            if (_ghostObject != null)
            {
                Destroy(_ghostObject);
                _ghostObject = null;
            }
        }

        public void SetTargetPosition(Vector3 target)
        {
            _targetPosition = target;
        }

        public void MoveGhost()
        {
            // Required if the object has a network script attached
            _ghostObject.SetActive(true);

            _ghostObject.transform.position = Vector3.Lerp(_ghostObject.transform.position, _targetPosition, Time.deltaTime * 15f);
            _ghostObject.transform.rotation = Quaternion.Lerp(_ghostObject.transform.rotation, Quaternion.Euler(0, TileHelper.GetRotationAngle(_dir), 0), Time.deltaTime * 15f);
        }

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

        public void NextRotation()
        {
            _dir = TileHelper.GetNextDir(_dir);
        }

        public Direction GetDir()
        {
            return _dir;
        }
    }
}