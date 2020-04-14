using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CustomNetworkManager : NetworkManager
{
    public void StartClient(TMP_InputField address)
    {
        mode = NetworkManagerMode.ClientOnly;

        InitializeSingleton();

        if (authenticator != null)
        {
            authenticator.OnStartClient();
            authenticator.OnClientAuthenticated.AddListener(OnClientAuthenticated);
        }

        if (runInBackground)
            Application.runInBackground = true;

        isNetworkActive = true;

        RegisterClientMessages();

        this.networkAddress = address.text;
        if (LogFilter.Debug) Debug.Log("NetworkManager StartClient address:" + networkAddress);

        NetworkClient.Connect(networkAddress);

        OnStartClient();
    }
}