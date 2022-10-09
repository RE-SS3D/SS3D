using UnityEngine;

namespace SS3D.Utils
{
    public class SkyboxController : MonoBehaviour
    {
        private Skybox _current;
    
        [Range(0.005f, 0.02f)]
        public float rotationRate = 0.02f;

        private static readonly int Rotation = Shader.PropertyToID(("_Rotation"));

        private void Awake()
        {
            _current = GetComponent<Skybox>();

            Material clone = new(_current.material);
            _current.material = clone;
        }

        private void FixedUpdate()
        {
            _current.material.SetFloat(Rotation, _current.material.GetFloat(Rotation) + rotationRate);
        }
    }
}
