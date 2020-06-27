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
                    humanRagdoll.BodyEnabled = true;
                    slippedBodies.Add(humanRagdoll, Time.time);
                    StartCoroutine(DisableBodyCoroutine(humanRagdoll));
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

        private IEnumerator DisableBodyCoroutine(HumanRagdoll body)
        {
            yield return new WaitForSeconds(3);
            body.BodyEnabled = false;
        }
    }
}
