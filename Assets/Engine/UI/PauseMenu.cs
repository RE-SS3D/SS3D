using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Server.Round;
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

        RoundManager.ServerRoundEnded += ForceToggleOff;
    }
    
    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !CameraManager.singleton.lobbyCamera.gameObject.activeSelf) 
        {
            Toggle();
        }
    }

    
    public void Toggle()
    {
        if (!animator.enabled)
        { 
            animator.enabled = true; 
            return;
        }
        animator.SetBool("Toggle", !animator.GetBool("Toggle"));
    }

    public void ForceToggleOff()
    {
        if (!animator.enabled)
        { 
            animator.enabled = true;
            return;
        }
        animator.SetBool("Toggle", false); 
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
