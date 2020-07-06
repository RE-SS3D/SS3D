using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Content.Graphics.UI;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Tiles;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Content.Systems.Construction
{
    [RequireComponent(typeof(Item))]
    public class ConstructionMaterial : MonoBehaviour, IInteractionSourceExtension
    {
        public GameObject listMenuPrefab;
        public GameObject entryPrefab;
        public GameObject loadingBarPrefab;
        public LayerMask obstacleMask;
        public Construction[] constructions;

        private Item item;
        private ListMenu constructionMenu;
        private TileObject selectedTile;

        private void Start()
        {
            item = GetComponent<Item>();
        }

        public void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            interactions.Add(new InteractionEntry(targets.First(), new SimpleInteraction
            {
                Name = "Construct",
                CanInteractCallback = CanOpenConstruction,
                Interact = OpenConstruction
            }));
        }

        private void OpenConstruction(InteractionEvent arg1, InteractionReference arg2)
        {
            // Set selected tile to build on
            selectedTile = arg1.Target.GetComponent<Transform>().parent.GetComponent<TileObject>();
            // Create menu
            GameObject ui = Instantiate(listMenuPrefab);
            // Close old menu if it exists
            if (constructionMenu != null)
            {
                constructionMenu.Close();
            }

            constructionMenu = ui.GetComponent<ListMenu>();
            constructionMenu.Title = "Construct";

            // Create elements to construct
            foreach (Construction construction in constructions)
            {
                GameObject uiEntry = Instantiate(entryPrefab);

                var entry = uiEntry.GetComponent<ConstructionEntry>();
                entry.SetConstruction(construction, item.Name);
                entry.Click += Construct;

                constructionMenu.AddElement(uiEntry);
            }
        }

        private void Construct(object sender, EventArgs e)
        {
            var entry = (ConstructionEntry) sender;
            Construction construction = entry.Construction;
            TileDefinition tile = selectedTile.Tile;
            Debug.Log(construction.name);

            if (construction.turf)
            {
                var source = GetComponent<IInteractionSource>();
                // Create interaction data
                var @event = new InteractionEvent(source, new InteractionTargetGameObject(selectedTile.gameObject), 
                    selectedTile.transform.position);
                var interaction = new TurfConstructionInteraction
                {
                    Turf = construction.turf,
                    ConstructIfTurf = construction.constructOverTurf,
                    LoadingBarPrefab = loadingBarPrefab,
                    Delay = construction.buildTime,
                    ObstacleMask = obstacleMask
                };
                // Check if interaction is possible
                if (!interaction.CanInteract(@event))
                {
                    return;
                }
                // Start interaction
                source.Interact(@event,
                    interaction);
            }

            constructionMenu.Close();
            constructionMenu = null;
        }

        private bool CanOpenConstruction(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is IGameObjectProvider provider)
            {
                return provider.GameObject.transform.parent.GetComponent<TileObject>();
            }

            return false;
        }

        [Serializable]
        public struct Construction
        {
            public string name;
            public int amount;
            public float buildTime;
            public Turf turf;
            public bool constructOverTurf;
        }
    }
}