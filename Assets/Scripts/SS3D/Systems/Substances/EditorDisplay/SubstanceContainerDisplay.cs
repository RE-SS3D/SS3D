using SS3D.Attributes;
using SS3D.Substances;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Put it next to a substance container and set up the reference to see at run time
/// the actual amount of different substances present in the substance container
/// </summary>

namespace SS3D.Substances
{
    public class SubstanceContainerDisplay : MonoBehaviour
    {

        [SerializeField] private SubstanceContainer _container;

        [ReadOnly] public List<SubstanceEntry> Substances;

        // Update is called once per frame
        void Update()
        {
            Substances = _container.Substances.ToList();
        }
    }
}

