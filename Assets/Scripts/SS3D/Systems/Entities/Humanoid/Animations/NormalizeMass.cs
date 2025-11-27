using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    public class NormalizeMass : MonoBehaviour
    {
        protected void Start()
        {
            Apply(transform);
        }

        private void Apply(Transform root)
        {
            Joint j = root.GetComponent<Joint>();

            // Apply the inertia scaling if possible
            if (j && j.connectedBody)
            {
                // Make sure that both of the connected bodies will be moved by the solver with equal speed
                j.massScale = j.connectedBody.mass / root.GetComponent<Rigidbody>().mass;
                j.connectedMassScale = 1f;
            }

            // Continue for all children...
            for (int childId = 0; childId < root.childCount; ++childId)
            {
                Apply(root.GetChild(childId));
            }
        }
    }
}
