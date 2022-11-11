using UnityEngine;

namespace SS3D.Rendering
{
    [ExecuteInEditMode]
    public class LaplacianEffect : MonoBehaviour
    {
        public Material Material;

        // Postprocess the image
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination, Material);
        }
    }
}