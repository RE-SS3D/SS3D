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
        private Dictionary<HumanRigidBody, float> slippedBodies;

        private void Start()
        {
            slippedBodies = new Dictionary<HumanRigidBody, float>();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!isServer)
            {
                return;
            }
            
            HumanRigidBody humanRigidBody = other.transform.root.gameObject.GetComponent<HumanRigidBody>();
            if (humanRigidBody != null && !humanRigidBody.BodyEnabled)
            {
                CleanBodyDictionary();
                if (!slippedBodies.ContainsKey(humanRigidBody))
                {
                    humanRigidBody.BodyEnabled = true;
                    slippedBodies.Add(humanRigidBody, Time.time);
                    StartCoroutine(DisableBodyCoroutine(humanRigidBody));
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

        private IEnumerator DisableBodyCoroutine(HumanRigidBody body)
        {
            yield return new WaitForSeconds(3);
            body.BodyEnabled = false;
        }
    }
}
