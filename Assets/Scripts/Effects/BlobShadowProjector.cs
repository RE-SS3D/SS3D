using UnityEngine;

namespace Effects
{

    public class BlobShadowProjector : MonoBehaviour
    {
        private Transform parent = null;

        void Start()
        {
            parent = this.transform.parent;

            TurnDynamicShadowsOnIfTrue(false);
        }

        private void OnDestroy()
        {
            TurnDynamicShadowsOnIfTrue(true);
        }

        private void TurnDynamicShadowsOnIfTrue(bool b)
        {
            if (b)
            {
                foreach (Transform child in parent)
                {
                    if (child.GetComponent<Renderer>() != null)
                    {
                        child.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    }
                }
            }
            else
            {
                foreach (Transform child in parent)
                {
                    if (child.GetComponent<Renderer>() != null)
                    {
                        child.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }
        }
    }
}