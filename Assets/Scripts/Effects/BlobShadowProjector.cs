using UnityEngine;

namespace Effects
{

    public class BlobShadowProjector : MonoBehaviour
    {
        private Transform parent = null;

        void Start()
        {
            parent = this.transform.parent;

            TurnShadowCastingOffInChildren();
        }

        private void OnDestroy()
        {
            TurnShadowCastingOnInChildren();
        }

        //Turns dynamic shadows off for the parent transform
        private void TurnShadowCastingOffInChildren()
        {

            foreach (Transform child in parent)
            {
                if (child.GetComponent<Renderer>() != null)
                {
                    child.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        //Turns dynamic shadows on
        private void TurnShadowCastingOnInChildren()
        {
            foreach (Transform child in parent)
            {
                if (child.GetComponent<Renderer>() != null)
                {
                    child.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }
        }
    }
}