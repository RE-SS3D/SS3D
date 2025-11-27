using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Camera
{
    public class CameraActor : Actor
    {
        [field: SerializeField]
        public UnityEngine.Camera Camera { get; private set; }
    }
}
