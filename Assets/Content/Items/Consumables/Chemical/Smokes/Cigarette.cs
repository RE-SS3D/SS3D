using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;
using Mirror;
using UnityEngine;

namespace SS3D.Content.Items.Consumables
{
    public class Cigarette : Item, IIgnitable
    {
        [SerializeField]
        private Sprite extinguishIcon, igniteIcon;

        [SerializeField]
        private GameObject buttPrefab;

        [SerializeField]
        private MeshRenderer unlitMeshRenderer;

        [SerializeField]
        private SkinnedMeshRenderer litMeshRenderer;

        [SerializeField]
        private ParticleSystem smokeParticle;

        [SerializeField]
        private float timeToSmoke;


        private Coroutine consumeCoroutine;
        [SyncVar] private float activeBurnTime;
        [SyncVar] private float amountConsumed;
        [SyncVar] private bool lit;

        public bool FullyConsumed => amountConsumed == 1;

        public bool CanBeLit => !Lit && !FullyConsumed;

        public bool Lit => lit;

        public void Ignite()
        {
            if (FullyConsumed)
            {
                Extinguish();
                return;
            }

            lit = true;
            consumeCoroutine = StartCoroutine(ConsumeCigaretteCoroutine());
            RpcSetParticle(true); 
            RpcUpdateMesh();
        }

        public void Extinguish()
        {
            lit = false;
            if (consumeCoroutine != null)
            {
                StopCoroutine(consumeCoroutine);
                consumeCoroutine = null;
            }
            RpcSetParticle(false);
            CreateButt();
        }

        [ClientRpc]
        private void RpcUpdateMesh()
        {
            if (Lit)
            {
                litMeshRenderer.gameObject.SetActive(true);
                litMeshRenderer.SetBlendShapeWeight(0, amountConsumed * 100);
                unlitMeshRenderer.gameObject.SetActive(false);
            }
            else
            {
                litMeshRenderer.gameObject.SetActive(false);
                unlitMeshRenderer.gameObject.SetActive(true);
            }
        }

        [ClientRpc]
        private void RpcSetParticle(bool play)
        {
            if (smokeParticle == null)
            {
                return;
            }

            if (play) smokeParticle.Play();
            else smokeParticle.Stop();
        }

        private void CreateButt()
        {
            Item butt = ItemHelpers.CreateItem(buttPrefab);
            ItemHelpers.ReplaceItem(this, butt);
        }

        private IEnumerator ConsumeCigaretteCoroutine()
        {
            while (!FullyConsumed)
            {
                ConsumeUpdate();
                yield return null;
            }
            Extinguish();
        }

        private void ConsumeUpdate()
        {
            activeBurnTime += Time.deltaTime;
            amountConsumed = activeBurnTime / timeToSmoke;
            amountConsumed = Mathf.Min(amountConsumed, 1);
            RpcUpdateMesh();
        }

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            interactions.Add(new IgniteInteraction() { extinguishIcon = extinguishIcon, igniteIcon = igniteIcon });
            return interactions.ToArray();
        }
    }
}