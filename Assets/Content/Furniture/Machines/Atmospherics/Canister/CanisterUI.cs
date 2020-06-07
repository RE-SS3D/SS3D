using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CanisterUI : MonoBehaviour
{
    public TMP_Text label;
    public TMP_Text pressure;
    public TMP_Text portStatus;

    public TMP_Text holdingLabel;
    public TMP_Text holdingPressure;

    public TMP_Text releasePressure;

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
