using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MainLobbyButtonHelper : NetworkBehaviour
{
    private LoginNetworkManager networkManager;

    private void Awake()
    {
        networkManager = LoginNetworkManager.singleton;
    }

    public void Quit()
    {
        Application.Quit();
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
