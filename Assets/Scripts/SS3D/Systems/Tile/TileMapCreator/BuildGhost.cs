using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static SS3D.Systems.Tile.TileMapCreator.BuildGhost;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Class for managing ghost objects, that will be used for building or deleting by TileMapCreator.
    /// </summary>
    public class BuildGhost : NetworkActor
    {
        public static Material _validConstruction;
        public static Material _invalidConstruction;
        public static Material _deleteConstruction;
        

        public List<BuildGhostStruct> _ghosts = new();


        public Direction Dir { get; set; } = Direction.North;
        public enum BuildMatMode
        {
            Valid,
            Invalid,
            Delete
        }
        public void SetupMaterials(Material validConstruction, Material invalidConstruction, Material deleteConstruction)
        {
            _validConstruction = validConstruction;
            _invalidConstruction = invalidConstruction;
            _deleteConstruction = deleteConstruction;
        }

        public void SetNextRotation()
        {
            foreach (var ghost in _ghosts)
            {
                if (ghost.ghostObject.TryGetComponent(out ICustomGhostRotation customRotationComponent))
                {
                    Dir = customRotationComponent.GetNextDirection(Dir);
                }
                else
                {
                    Dir = TileHelper.GetNextCardinalDir(Dir);
                }
            }
        }

        public BuildGhostStruct CreateGhost(GameObject prefab, Vector3 position, Direction direction)
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

            var ghostStruct = new BuildGhostStruct(_ghostObject, position);

            _ghosts.Add(ghostStruct);
            return ghostStruct;
        }

        public void DestroyGhosts()
        {
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                _ghosts[i].ghostObject.Dispose(true);
            }
            _ghosts.Clear();
        }

        public class BuildGhostStruct
        {
            public GameObject ghostObject;
            public Vector3 TargetPosition;

            public BuildGhostStruct(GameObject ghostObject, Vector3 targetPosition)
            {
                this.ghostObject = ghostObject;
                this.TargetPosition = targetPosition;
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
                        ghostMat = _validConstruction;
                        break;

                    case BuildMatMode.Invalid:
                        ghostMat = _invalidConstruction;
                        break;

                    case BuildMatMode.Delete:
                        ghostMat = _deleteConstruction;
                        break;
                }

                foreach (MeshRenderer mr in ghostObject.GetComponentsInChildren<MeshRenderer>())
                {
                    Material[] materials = mr.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = ghostMat;
                    }

                    mr.materials = materials;
                }
            }
        }

    }

   

}
