using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static SS3D.Systems.Tile.TileMapCreator.BuildGhostManager;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Class for managing ghost objects, that will be used for building or deleting by TileMapCreator.
    /// </summary>
    public class BuildGhostManager : NetworkActor
    {
        public Material _validConstruction;
        public Material _invalidConstruction;
        public Material _deleteConstruction;
        

        public List<BuildGhost> _ghosts = new();


        public Direction Dir { get; set; } = Direction.North;
        public enum BuildMatMode
        {
            Valid,
            Invalid,
            Delete
        }


        public void Update()
        {
            foreach (var buildGhost in _ghosts)
            {
                buildGhost.UpdateRotationAndPosition();
            }
        }

        public void SetNextRotation()
        {
            foreach (var ghost in _ghosts)
            {
                ghost.UpdateRotationAndPosition();
            }
        }

        public BuildGhost CreateGhost(GameObject prefab, Vector3 position, Direction direction)
        {

            if (prefab.TryGetComponent(out ICustomGhostRotation customRotationComponent))
            {
                if (customRotationComponent.GetAllowedRotations().Contains(direction))
                {
                    Dir = direction;
                }
                else
                {
                    Dir = customRotationComponent.DefaultDirection;
                }
            }
            else
            {
                Dir = direction;
            }

            Quaternion rotation = Quaternion.Euler(0, TileHelper.GetRotationAngle(Dir), 0);
            var _ghostObject = Instantiate(prefab, position, rotation);

            if (_ghostObject.TryGetComponent<Rigidbody>(out var ghostRigidbody))
            {
                ghostRigidbody.useGravity = false;
                ghostRigidbody.isKinematic = true;
            }
            var colliders = _ghostObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            var ghostStruct = new BuildGhost(_ghostObject, position, direction);

            _ghosts.Add(ghostStruct);
            return ghostStruct;
        }

        public void DestroyGhosts()
        {
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                _ghosts[i].ghost.Dispose(true);
            }
            _ghosts.Clear();
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

   

}
