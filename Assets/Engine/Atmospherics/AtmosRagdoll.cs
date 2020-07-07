using System.Collections;
using Mirror;
using SS3D.Content.Creatures.Human;
using SS3D.Engine.Tiles;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Atmospherics
{
    [RequireComponent(typeof(HumanRagdoll))]
    public class AtmosRagdoll : MonoBehaviour
    {
        public float minVelocity = 1;
        public float knockdownTime = 3;
        public float checkInterval = 0.2f;
        
        private HumanRagdoll ragdoll;
        private int lastX;
        private int lastY;
        private float lastCheck;
        private TileManager tileManager;
        private Vector3 origin;
        private AtmosObject atmosObject;

        void Start()
        {
            if (!NetworkServer.active)
            {
                Destroy(this);
            }
            
            ragdoll = GetComponent<HumanRagdoll>();
            tileManager = FindObjectOfType<TileManager>();
            Assert.IsNotNull(tileManager);
            origin = tileManager.Origin;
        }

        void Update()
        {
            float time = Time.time;
            // Reduce check interval
            if (lastCheck + checkInterval < time)
            {
                lastCheck = time;

                // Get current tile position
                Vector3 position = transform.position;
                int x = Mathf.FloorToInt(position.x);
                int y = Mathf.FloorToInt(position.z);

                // Update atmos object if tile changed
                if (x != lastX || y != lastY)
                {
                    lastX = x;
                    lastY = y;
                    atmosObject = tileManager.GetTile((int) (x - origin.x), (int) (y - origin.z))?.atmos;
                }

                // Check velocity
                if (atmosObject != null)
                {
                    ApplyVelocity(atmosObject.GetVelocity());
                }
            }
        }

        private void ApplyVelocity(Vector2 velocity)
        {
            if (velocity.sqrMagnitude > minVelocity * minVelocity)
            {
                ragdoll.KnockDown(knockdownTime);
            }
        }
    }
}