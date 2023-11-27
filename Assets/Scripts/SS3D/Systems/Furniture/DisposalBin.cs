using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SS3D.Systems.Furniture
{
    /// Currently a simple script to spot disposal Bins.
    public class DisposalBin : MonoBehaviour, IDisposalElement
    {
        public GameObject GameObject => gameObject;
    }
}
