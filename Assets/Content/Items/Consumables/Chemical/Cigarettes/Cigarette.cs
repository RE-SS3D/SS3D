using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Tiles;
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
        private float timeToSmoke;


        private Coroutine consumeCoroutine;
        private float activeBurnTime;
        private float amountConsumed;

        public bool FullyConsumed => amountConsumed == 1;

        public bool CanBeLit => !Lit && !FullyConsumed;

        public bool Lit => consumeCoroutine != null;

        public void Ignite()
        {
            if (FullyConsumed)
            {
                Extinguish();
                return;
            }

            //Lit = true;
            consumeCoroutine = StartCoroutine(ConsumeCigaretteCoroutine());
            UpdateMesh();
        }

        public void Extinguish()
        {
            if (consumeCoroutine != null)
            {
                StopCoroutine(consumeCoroutine);
                consumeCoroutine = null;
            }
            CreateButt();
        }

        private void UpdateMesh()
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

        private void CreateButt()
        {
            ItemHelpers.ReplaceItem(this, ItemHelpers.CreateItem(buttPrefab));
        }

        private IEnumerator ConsumeCigaretteCoroutine()
        {
            while (!FullyConsumed)
            {
                activeBurnTime += Time.deltaTime;
                amountConsumed = activeBurnTime / timeToSmoke;
                amountConsumed = Mathf.Min(amountConsumed, 1);
                UpdateMesh();
                yield return null;
            }
            Extinguish();
        }

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.GenerateInteractions(interactionEvent).ToList();
            interactions.Add(new IgniteInteraction() { extinguishIcon = extinguishIcon, igniteIcon = igniteIcon });
            return interactions.ToArray();
        }
    }
}