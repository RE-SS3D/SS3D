using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Petal : MonoBehaviour
{
    public Animator animator;
    public RawImage petalSprite;
    public GameObject rotator;
    public RawImage iconImage;
    private RadialMenuButton button;
    private Animator parentAnimator;
    private RectTransform rect;

    public void Awake()
    {
        button = transform.GetChild(0).GetChild(0).GetComponent<RadialMenuButton>();
        parentAnimator = transform.parent.parent.GetComponent<Animator>();
        rect = GetComponent<RectTransform>();
    }

    public void UpdateRotation()
    {
        float rot = rect.transform.rotation.eulerAngles.z;
        button.angle = rot + 90 > 359 ? (rot + 90) - 360 : rot + 90;
    }

    public void Update()
    {
        iconImage.rectTransform.localEulerAngles = (-this.gameObject.transform.localEulerAngles - rotator.gameObject.transform.localEulerAngles);
        UpdateRotation();
    }

    public void SetIcon(Texture2D icon)
    {
        iconImage.texture = icon;
    }

    public void SetColor(Color color)
    {
        petalSprite.color = color;
    }

    public void Appear()
    {
        animator.SetBool("Visible", true);
    }

    public void Disappear()
    {
        animator.SetBool("Visible", false);
    }

    public void Rotate(bool clockwise)
    {
        animator.SetTrigger(clockwise ? "RotRight" : "RotLeft");
    }

    public bool IsVisible()
    {
        return (animator.GetBool("Visible"));
    }

    public bool IsAnimationInProgress()
    {
        if (this.animator.GetCurrentAnimatorStateInfo(1).IsName("Standby") == false)
            return (false);
        return (true);
    }
}
