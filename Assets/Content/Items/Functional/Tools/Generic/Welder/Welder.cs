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
    public class Welder : Item, IToggleable
    {
        [SerializeField]
        public ParticleSystem hotFlame;
        [SerializeField]
        public ParticleSystem coldFlame;
        [SerializeField]
        public ParticleSystem lightParticle;
        [SerializeField]
        private Turf commonWall = null;
        [SerializeField]
        private Turf reinforcedWall = null;
        [SerializeField]
        private Turf commonFloor = null;
        [SerializeField]
        private Turf reinforcedFloor = null;
        public GameObject LoadingBarPrefab;
        public float Delay;

        public Sprite turnOnIcon;
        public Sprite constructIcon;

        private Dictionary<Turf, Turf> reinforceDict;
        
        public override void Start()
        {
            base.Start();
            reinforceDict = new Dictionary<Turf, Turf> {{commonWall, reinforcedWall}, {commonFloor, reinforcedFloor}};
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

        public override void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            base.CreateInteractions(targets, interactions);
            interactions.Insert(0, new InteractionEntry(targets[0], new WelderConstructionInteraction
            {
                TurfReinforceList = reinforceDict,
                LoadingBarPrefab = LoadingBarPrefab,
                Delay = Delay,
                icon = constructIcon
            }));
        }

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> list = base.GenerateInteractions(interactionEvent).ToList();
            
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