using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    Animator animator;
    
    void Start() {    
        animator = GetComponent<Animator>();
    }
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            animator.SetBool("Toggle", !animator.GetBool("Toggle"));
        }
    }
}
