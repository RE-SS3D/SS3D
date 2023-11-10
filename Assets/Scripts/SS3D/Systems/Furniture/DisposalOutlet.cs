using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Currently a simple script to spot disposal outlets.
    /// </summary>
    public class DisposalOutlet : MonoBehaviour, IDisposalElement
    {
        public GameObject GameObject => gameObject;
    }
}
