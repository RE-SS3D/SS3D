using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Creatures.Human;
using UnityEngine;

namespace SS3D.Content.Items
{
    // handles sliperry items, that makes you slip
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

	// when something touches it
        private void OnCollisionEnter(Collision other)
        {
            if (!isServer)
            {
                return;
            }
            
	    // tries to get the ragdoll manager in the collision that touched it
            HumanRagdoll humanRagdoll = other.transform.root.gameObject.GetComponent<HumanRagdoll>();
            if (humanRagdoll != null && !humanRagdoll.BodyEnabled)
            {
		// Add a threshold for the ragdoll to slip, for example when you use clown shoes you cant slip on bananas
                CleanBodyDictionary();
                if (!slippedBodies.ContainsKey(humanRagdoll))
                {
                    humanRagdoll.KnockDown(knockdownDuration);
                    slippedBodies.Add(humanRagdoll, Time.time);
                }
            }
            
        }

	// Removes all the previous slipped people
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
