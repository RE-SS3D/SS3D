using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Electricity
{
    public interface IPowerConsumer : IElectricDevice
    {
        public float PowerNeeded { get; }

        public PowerStatus PowerStatus { get; set; }
    }
}
