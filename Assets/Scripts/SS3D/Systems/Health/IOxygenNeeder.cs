using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public interface IOxygenNeeder
    {
        /// <summary>
        /// Return the amount in moles of needed oxygen
        /// </summary>
        public double GetOxygenNeeded();
    }
}
