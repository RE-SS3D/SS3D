using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Substances
{
    /// <summary>
    /// Put it next to a substance container and set up the reference to see at run time
    /// the actual amount of different substances present in the substance container
    /// </summary>
    public class SubstanceContainerDisplay : MonoBehaviour
    {
        [SerializeField]
        private SubstanceContainer _container;

        #if UNITY_EDITOR
        [FormerlySerializedAs("Substances")]
        [ReadOnly]
        #endif
        [SerializeField]
        private List<SubstanceEntry> _substances;

        // Update is called once per frame
        protected void Update()
        {
            _substances = _container.Substances.ToList();
        }
    }
}
