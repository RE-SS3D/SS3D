using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobShadowProjector : MonoBehaviour
{
    private Transform parent = null;

    void Start()
    {
        parent = this.transform.parent;

        turnShadowCastingOffInChildren();
    }

    //Turns dynamic shadows off the parent transform
    private void turnShadowCastingOffInChildren()
    {

        foreach (Transform child in parent)
        {
            if (child.GetComponent<Renderer>() != null) {
                child.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
