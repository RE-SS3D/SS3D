using Mirror;
using UnityEngine;

namespace SS3D.Engine.Utilities
{
    public class DestroyOnRemote : NetworkBehaviour
    {
        public GameObject TargetObject;
        
        void Start()
        {
            if (!isServer)
            {
                Destroy(TargetObject);
                Destroy(this);
            }
        }
    }
}
