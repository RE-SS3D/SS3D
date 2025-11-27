using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public interface ISpeedProvider
    {
        public event Action<float> OnSpeedChangeEvent;
    }
}
