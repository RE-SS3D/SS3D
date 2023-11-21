using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
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
        public GameObject ghost;
        public Vector3 position;
        public Direction direction;

        public BuildGhost(GameObject ghostObject, Vector3 targetPosition, Direction dir)
        {
            ghost = ghostObject;
            position = targetPosition;
            direction = dir;
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


            foreach (MeshRenderer mr in ghost.GetComponentsInChildren<MeshRenderer>())
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
            ghost.transform.position = Vector3.Lerp(ghost.transform.position, position + new Vector3(0, 0.1f, 0), Time.deltaTime * 15f);
            ghost.transform.rotation = Quaternion.Lerp(ghost.transform.rotation, Quaternion.Euler(0, TileHelper.GetRotationAngle(direction), 0), Time.deltaTime * 15f);
        }

        public void SetNextRotation()
        {
            if (ghost.TryGetComponent(out ICustomGhostRotation customRotationComponent))
            {
                direction = customRotationComponent.GetNextDirection(direction);
            }
            else
            {
                direction = TileHelper.GetNextCardinalDir(direction);
            }
        }
    }
}