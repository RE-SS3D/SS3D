using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System;
using SS3D.Logging;


namespace SS3D.Core
{
    public class ServerStartedListener : NetworkBehaviour
    {
        public event EventHandler OnServerOrClientStarted;

        public void Awake()
        {
            OnServerOrClientStarted +=  LogManager.OnServerStarted;
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            OnServerOrClientStarted?.Invoke(this,EventArgs.Empty);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnServerOrClientStarted?.Invoke(this, EventArgs.Empty);
        }


        // Update is called once per frame
        void Update()
        {

        }
    }
}


