using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Graphics.UI;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using SS3D.Engine.Tiles;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Content.Systems.Construction
{
    [RequireComponent(typeof(Item))]
    public class ConstructionMaterial : NetworkBehaviour, IInteractionSourceExtension
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

        private void OpenConstruction(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            // Set selected tile to build on
            var tile = interactionEvent.Target.GetComponent<Transform>().parent.GetComponent<TileObject>();
            if (tile == selectedTile)
            {
                return;
            }
            selectedTile = tile;

            string material = GetComponent<Item>().Name;
            ConstructionUiData[] uiData = constructions.Select(x => x.ToUi(material)).ToArray();

            NetworkConnection owner = interactionEvent.Source.GetInstanceFromReference(arg2).Owner;
            if (owner == null)
            {
                // Fuck you mirror, just act like you love me (or as if you had a connection)
                UpdateUi(uiData);
            }
            else
            {
                TargetUpdateUi(owner, uiData);
            }
        }

        [TargetRpc]
        public void TargetUpdateUi(NetworkConnection target, ConstructionUiData[] data)
        {
            UpdateUi(data);
        }

        [Client]
        private void UpdateUi(ConstructionUiData[] data)
        {
            // Construct menu if it doesn't exist
            if (constructionMenu == null)
            {
                // Create menu
                GameObject ui = Instantiate(listMenuPrefab);
                constructionMenu = ui.GetComponent<ListMenu>();
                constructionMenu.Title = "Construct";
            }
            
            constructionMenu.Clear();
            
            foreach (var uiData in data)
            {
                GameObject uiEntry = Instantiate(entryPrefab);

                var entry = uiEntry.GetComponent<ConstructionEntry>();
                entry.SetConstruction(uiData);
                entry.Click += ClientConstruct;
                
                constructionMenu.AddElement(uiEntry);
            }
        }

        private void ClientConstruct(object sender, EventArgs e)
        {
            var entry = (ConstructionEntry)sender;
            CmdConstruct(entry.transform.GetSiblingIndex());
        }

        [Command(ignoreAuthority = true)]
        public void CmdConstruct(int index, NetworkConnectionToClient client = null)
        {
            // Check if sending player is holding 
            if (client != null && client.identity.GetComponent<Hands>().GetItemInHand().gameObject != gameObject)
            {
                return;
            }
            
            Construction construction = constructions[index];
            TileDefinition tile = selectedTile.Tile;

            IInteraction interaction;

            if (construction.turf)
            {
                interaction = new TurfConstructionInteraction
                {
                    Turf = construction.turf,
                    ConstructIfTurf = construction.constructOverTurf,
                    LoadingBarPrefab = loadingBarPrefab,
                    Delay = construction.buildTime,
                    ObstacleMask = obstacleMask
                };
            }
            else if (construction.fixture)
            {
                interaction = new FixtureConstructionInteraction
                {
                    Fixture = construction.fixture,
                    TileLayer = construction.tileLayer,
                    WallLayer = construction.wallLayer,
                    FloorLayer = construction.floorLayer,
                    FixtureType = construction.type,
                    LoadingBarPrefab = loadingBarPrefab,
                    Delay = construction.buildTime,
                    ObstacleMask = obstacleMask
                };
            }
            else
            {
                Debug.LogError("Construction does not have a turf or fixture to construct");
                return;
            }
            
            // Add material requirement
            interaction = new ItemRequirement(interaction, item.ItemId, construction.amount);
            
            var source = GetComponent<IInteractionSource>();
            // Create interaction data
            var @event = new InteractionEvent(source, new InteractionTargetGameObject(selectedTile.gameObject), 
                selectedTile.transform.position);
            // Check if interaction is possible
            if (!interaction.CanInteract(@event))
            {
                return;
            }
            // Start interaction
            source.Interact(@event,
                interaction);
        }

        private bool CanOpenConstruction(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is IGameObjectProvider provider)
            {
                return provider.GameObject.GetComponentInParent<TileObject>();
            }

            return false;
        }

        [Serializable]
        public struct ConstructionUiData
        {
            public string name;
            public string description;

            public ConstructionUiData(string name, string description)
            {
                this.name = name;
                this.description = description;
            }
        }
        
        [Serializable]
        public struct Construction
        {
            public string name;
            public int amount;
            public float buildTime;
            
            // Turf data
            public Turf turf;
            public bool constructOverTurf;
            
            // Fixture data
            public Fixture fixture;
            public FixtureType type;
            public TileFixtureLayers tileLayer;
            public WallFixtureLayers wallLayer;
            public FloorFixtureLayers floorLayer;

            public ConstructionUiData ToUi(string materialName)
            {
                return new ConstructionUiData(name, $"{amount} {materialName}");
            }
        }
    }
}