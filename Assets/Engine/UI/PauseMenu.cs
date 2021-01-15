using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Networking.Types;

public class PauseMenu : NetworkBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] LoginNetworkManager networkManager;
    
    void Start() 
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        networkManager = FindObjectOfType<LoginNetworkManager>();
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
        if (!animator.gameObject.active)
        { 
            animator.gameObject.SetActive(true); 
            return;
        }
        animator.SetBool("Toggle", !animator.GetBool("Toggle"));
    }

    public void Disconnect()
    {
        if (isServer)
        {
            networkManager.StopHost();
            Debug.Log("server");
        }
        if (isClient)
        {
            Debug.Log("client");
            networkManager.StopClient();
        }
        
    }
}
