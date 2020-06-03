using UnityEngine;
using UnityEngine.Rendering;

namespace Effects
{

    public class BlobShadowProjector : MonoBehaviour
    {
        private Transform parent = null;

        void Start()
        {
            parent = this.transform.parent;

            SetDynamicShadowsOnParent(false);
        }

        private void OnDestroy()
        {
            SetDynamicShadowsOnParent(true);
        }

        private void SetDynamicShadowsOnParent(bool enabled)
        {
            var mode = enabled ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (parent)
            {
                foreach (Transform child in parent)
                {
                    var renderer = child.GetComponent<Renderer>();
                    if (renderer)
                    {
                        renderer.shadowCastingMode = mode;
                    }
                }
            }
        }
    }
}