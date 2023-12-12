﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Electricity
{
    public interface IPowerProducer : IElectricDevice
    {
        public float PowerProduction { get; }
    }
}