using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This handles the little buttons on the lobby menu
/// this is solely to manage quitting and disconnecting
/// </summary>
public class MainLobbyButtonHelper : MonoBehaviour
{
    private LoginNetworkManager networkManager;
    private LocalPlayerManager player;
    
    [SerializeField] private Animator animator;

    private void Start()
    {
        networkManager = LoginNetworkManager.singleton;
        player = LocalPlayerManager.singleton;
		Debug.Log("player.name = " + player.name);
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void Update()
    {
        // this used to toggle the button menu
        //if (Input.GetKeyDown(KeyCode.Escape)) animator.SetTrigger("Fade");
    }

    public void Disconnect()
    {
		// A client apparently doesn't know it's own connectionId, so we will use that
		// as a proxy to determine whether we are the client or the server.
		NetworkConnection connection = player.networkConnection;

        if (connection != null)
		{
            networkManager.StopHost();
        }
		else
		{
            networkManager.StopClient();
		}
    }
}
