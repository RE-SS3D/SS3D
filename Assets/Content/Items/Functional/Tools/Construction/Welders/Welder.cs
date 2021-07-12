using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Tiles;

namespace SS3D.Content.Items.Functional.Tools
{
    public class Welder : Item, IToggleable, IIgniter
    {
	// hot flame, cold flame and light particle are mere VFX stuff
        [SerializeField]
        public ParticleSystem hotFlame;
        [SerializeField]
        public ParticleSystem coldFlame;
        [SerializeField]
        public ParticleSystem lightParticle;
        [SerializeField]

	// for the temporary construction stuff
        private TileObjectSO commonWall = null;
        [SerializeField]
        private TileObjectSO reinforcedWall = null;
        [SerializeField]
        private TileObjectSO commonFloor = null;
        [SerializeField]
        private TileObjectSO reinforcedFloor = null;

	// the prefab for the loading bar that is spawned when we start an interaction
        public GameObject LoadingBarPrefab;
	// the time this takes to construct something
        public float Delay;

	// temporary icon stuff while we don't have asset data
        public Sprite turnOnIcon;
        public Sprite constructIcon;

        private Dictionary<TileObjectSO, TileObjectSO> reinforceDict;

        public bool CanIgnite => GetState();

        public override void Start()
        {
            base.Start();
            reinforceDict = new Dictionary<TileObjectSO, TileObjectSO> {{commonWall, reinforcedWall}, {commonFloor, reinforcedFloor}};
            GenerateNewIcon();
        }

        public void OnEnable()
        {
            hotFlame.enableEmission = false;
        }

        [ClientRpc]
        private void RpcTurnOn(bool value)
        {
            if (value)
            {
                hotFlame.Stop();
                coldFlame.Stop();
                lightParticle.Stop();
            }
            else
            {
                hotFlame.Play();
                coldFlame.Play();
                lightParticle.Play();
            }
        }

        public bool GetState()
        {
            return hotFlame.emission.enabled;
        }

        public void Toggle()
        {
            hotFlame.enableEmission = !hotFlame.enableEmission;
            if (hotFlame.isEmitting)
            {
                hotFlame.Stop();
                coldFlame.Stop();
                lightParticle.Stop();
            } 
            else
            {
                hotFlame.Play();
                coldFlame.Play();
                lightParticle.Play();
            }
            RpcTurnOn(!hotFlame.isEmitting);
        }

        public override void GenerateInteractionsFromSource(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            /*
            base.GenerateInteractionsFromSource(targets, interactions);
            interactions.Insert(0, new InteractionEntry(targets[0], new WelderConstructionInteraction
            {
                TurfReinforceList = reinforceDict,
                LoadingBarPrefab = LoadingBarPrefab,
                Delay = Delay,
                icon = constructIcon
            }));
            */
        }

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> list = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            
            ToggleInteraction toggleInteraction = new ToggleInteraction 
            {
                OnName = "Turn off",
                OffName = "Turn on",
                IconOn = turnOnIcon,
                IconOff = turnOnIcon 
            };
            list.Add(toggleInteraction);
            return list.ToArray();
        }
    }
}