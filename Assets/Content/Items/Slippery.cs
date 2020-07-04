using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Creatures.Human;
using UnityEngine;

namespace SS3D.Content.Items
{
    public class Slippery : NetworkBehaviour
    {
        /// <summary>
        /// How long a character is knocked down
        /// </summary>
        public float knockdownDuration = 3;
        /// <summary>
        /// How long before a character can get slipped again
        /// </summary>
        public float knockdownPause = 1;
        
        // Keeps track of who was recently slipped
        private Dictionary<HumanRagdoll, float> slippedBodies;

        private void Start()
        {
            slippedBodies = new Dictionary<HumanRagdoll, float>();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!isServer)
            {
                return;
            }
            
            HumanRagdoll humanRagdoll = other.transform.root.gameObject.GetComponent<HumanRagdoll>();
            if (humanRagdoll != null && !humanRagdoll.BodyEnabled)
            {
                CleanBodyDictionary();
                if (!slippedBodies.ContainsKey(humanRagdoll))
                {
                    humanRagdoll.KnockDown(knockdownDuration);
                    slippedBodies.Add(humanRagdoll, Time.time);
                }
            }
            
        }

        private void CleanBodyDictionary()
        {
            var keyValuePairs = slippedBodies.Where(x => x.Value + 4f < Time.time).ToList();
            foreach (var pair in keyValuePairs)
            {
                slippedBodies.Remove(pair.Key);
            }
        }
    }
}
