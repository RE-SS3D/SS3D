using System.Collections;
using SS3D.Content.Creatures.Human;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    [RequireComponent(typeof(HumanRagdoll))]
    public class AtmosRagdoll : MonoBehaviour
    {
        public float minVelocity = 1;
        public float knockdownTime = 3;
        
        private float knockdownStart = -1;
        private HumanRagdoll ragdoll;

        void Start()
        {
            ragdoll = GetComponent<HumanRagdoll>();
        }
        
        public void ApplyVelocity(Vector2 velocity)
        {
            if (velocity.sqrMagnitude > minVelocity * minVelocity)
            {
                bool alreadyKnocked = knockdownStart >= 0;
                knockdownStart = Time.time;
                
                if (!alreadyKnocked)
                {
                    ragdoll.BodyEnabled = true;
                    StartCoroutine(GetUp());
                }
            }
        }

        private IEnumerator GetUp()
        {
            while (knockdownStart + knockdownTime > Time.time)
            {
                yield return new WaitForSeconds(knockdownStart + knockdownTime - Time.time);
            }

            ragdoll.BodyEnabled = false;
            knockdownStart = -1;
        }
    }
}