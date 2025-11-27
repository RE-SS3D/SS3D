using Coimbra.Services.Events;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Intents
{
    /// <summary>
    /// This manages intent and it's done to easily support other intents
    /// </summary>
    public class IntentController : NetworkActor
    {
        public event EventHandler<IntentType> OnIntentChange;

        [SyncVar(OnChange = nameof(SyncIntent))]
        private IntentType _intent = IntentType.Help;

        public IntentType Intent => _intent;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _intent = IntentType.Help;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
            }

            AddHandle(IntentChanged.AddListener(HandleRoundStateUpdated));
        }

        public void HandleIntentButtonPressed()
        {
            UpdateIntent();
        }

        protected void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Space))
            {
                return;
            }

            UpdateIntent();
        }

        private void HandleRoundStateUpdated(ref EventContext context, in IntentChanged e)
        {
            UpdateIntent();
        }

        [ServerRpc]
        private void UpdateIntent()
        {
            _intent = _intent == IntentType.Help ? IntentType.Harm : IntentType.Help;
        }

        private void SyncIntent(IntentType prev, IntentType next, bool asServer)
        {
            Debug.Log($"Selected intent of {Owner} is {_intent}");

            if (asServer)
            {
                return;
            }

            OnIntentChange?.Invoke(this, _intent);
        }
    }
}
