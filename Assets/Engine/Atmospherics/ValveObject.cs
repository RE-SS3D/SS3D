using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class ValveObject : MonoBehaviour
    {
        public enum ValveType
        {
            Manual,
            Digital
        }

        public ValveType valveType;
        private PipeObject pipe;
        public bool isEnabled;

        // Start is called before the first frame update
        void Start()
        {
            pipe = GetComponent<PipeObject>();
            if (!isEnabled)
            {
                pipe.SetBlocked(true);
            }
            else
            {
                pipe.SetBlocked(false);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!isEnabled)
            {
                pipe.SetBlocked(true);
            }
            else
            {
                pipe.SetBlocked(false);
            }
        }
    }
}
