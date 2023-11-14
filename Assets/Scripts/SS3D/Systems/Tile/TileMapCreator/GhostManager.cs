using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using UnityEngine;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Class for creating and managing ghost objects used by the TileMapCreator.
    /// </summary>
    public class GhostManager: Actor
    {
        private Material _validConstruction;
        private Material _invalidConstruction;
        private Material _deleteConstruction;
        public Vector3 TargetPosition;

        public enum BuildMatMode
        {
            Valid,
            Invalid,
            Building,
            Deleting
        }

        public Direction Dir { get; private set; } = Direction.North;
        public void SetupMaterials(Material validConstruction, Material invalidConstruction, Material deleteConstruction)
        {
            _validConstruction = validConstruction;
            _invalidConstruction = invalidConstruction;
            _deleteConstruction = deleteConstruction;
        }

        protected override void OnStart()
        {
            base.OnStart();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
            if (TryGetComponent(out Rigidbody ghostRigidbody))
            {
                ghostRigidbody.useGravity = false;
                ghostRigidbody.isKinematic = true;
            }
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            // Small offset is added so that meshes don't overlap with already placed objects.
            transform.position = Vector3.Lerp(transform.position, TargetPosition + new Vector3(0, 0.1f, 0), Time.deltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, TileHelper.GetRotationAngle(Dir), 0), Time.deltaTime * 15f);
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
                case BuildMatMode.Building:
                    break;
                case BuildMatMode.Deleting:
                    ghostMat = _deleteConstruction;
                    break;
            }

            foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
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