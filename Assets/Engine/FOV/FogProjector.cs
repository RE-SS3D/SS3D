using UnityEngine;

namespace SS3D.Engine.FOV
{
    [RequireComponent(typeof(Projector))]
    public class FogProjector : MonoBehaviour
    {
        [SerializeField] private Material projectorMaterial = null;
        [SerializeField] private RenderTexture fogTexture = null;

        private RenderTexture currTexture;
        private Projector projector;
    
        private void Awake()
        {
            projector = GetComponent<Projector>();
        }

        private void OnEnable()
        {
            currTexture = fogTexture;

            // Projector materials aren't instanced, resulting in the material asset getting changed.
            // Instance it here to prevent us from having to check in or discard these changes manually.
            projector.material = new Material(projectorMaterial);

            projector.material.SetTexture("_CurrTexture", currTexture);
        }

        private void Update()
        {
            // Swap the textures
//        Graphics.Blit(fogTexture, currTexture);
        }
    }
}