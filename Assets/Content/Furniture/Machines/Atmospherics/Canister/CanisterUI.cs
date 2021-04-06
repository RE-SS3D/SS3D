using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// This should manage the UI for the canister
// The UI prefab needs a total rework
public class CanisterUI : MonoBehaviour
{
    // Canister name
    public TMP_Text label;
    // Current pressure
    public TMP_Text pressure;

    // if it is connected to a pipe or not
    public TMP_Text portStatus;

    // I don't know what this is
    public TMP_Text holdingLabel;
    public TMP_Text holdingPressure;

    // Nor this
    public TMP_Text releasePressure;

    // UI scale, we should dump this tho
    [Range(0.5f, 2.5f)]
    public float scale;

    public void Init(Canvas canvas)
    {
        transform.SetParent(canvas.transform);
        GetComponent<RectTransform>().localPosition = Vector2.zero;
    }

    private void Update()
    {
       GetComponent<RectTransform>().localScale = new Vector2(scale, scale);
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }
}
