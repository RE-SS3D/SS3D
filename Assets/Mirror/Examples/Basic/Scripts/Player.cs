using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Mirror.Examples.Basic
{
    public class Player : NetworkBehaviour
    {
        //todo: temporary naming stuff so people have names
        [SerializeField]
        private string[] possibleNames;

        [SyncVar]
        public int data;

        public TextMesh text;

        private void Awake() => gameObject.name = possibleNames[Random.Range(0, possibleNames.Length - 1)];

        public override void OnStartServer()
        {
            base.OnStartServer();
            InvokeRepeating(nameof(UpdateData), 1, 1);
        }

        public void UpdateData()
        {
            data = Random.Range(0, 10);
        }

        public void Update()
        {
            if (text == null)
                return;
            if (isLocalPlayer)
                text.color = Color.red;

            text.text = $"Player {netId}\ndata={data}";
        }
    }
}