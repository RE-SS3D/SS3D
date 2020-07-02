using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] Animator animator;
    
    void Start() 
    {
        if (animator == null)
        animator = GetComponent<Animator>();
    }
    
    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        animator.SetBool("Toggle", !animator.GetBool("Toggle"));
    }
}
