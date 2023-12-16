using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Electricity
{
    /// <summary>
    /// Interface to use for anything that need power to function, such as lamps, or electric devices.
    /// </summary>
    public interface IPowerConsumer : IElectricDevice
    {
        public float PowerNeeded { get; }

        public PowerStatus PowerStatus { get; set; }
    }
}
