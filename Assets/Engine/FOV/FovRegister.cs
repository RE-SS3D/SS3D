using Mirror;
using UnityEngine;

namespace SS3D.Engine.FOV
{
    public class FovRegister : NetworkBehaviour
    {
        [SerializeField]
        private FieldOfView fieldOfView;

        [SerializeField]
        private MeshRenderer fog;

        void Start()
        {
            fieldOfView.target = transform;
            fieldOfView.enabled = true;
            fog.enabled = true;
        }
    }
}