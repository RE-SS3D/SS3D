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

    public void Update()
    {
        iconImage.rectTransform.localEulerAngles = (-this.gameObject.transform.localEulerAngles - rotator.gameObject.transform.localEulerAngles);
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
