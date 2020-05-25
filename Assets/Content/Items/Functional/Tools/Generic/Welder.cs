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

        private Dictionary<Turf, Turf> reinforceDict;
        
        public void Start()
        {
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

        public override IInteraction[] GenerateInteractions(IInteractionTarget[] targets)
        {
            List<IInteraction> interactions = base.GenerateInteractions(targets).ToList();
            interactions.Insert(0, new WelderConstructionInteraction {TurfReinforceList = reinforceDict, LoadingBarPrefab = LoadingBarPrefab, Delay = Delay});
            ToggleInteraction toggleInteraction = new ToggleInteraction {OnName = "Turn off", OffName = "Turn on"};
            interactions.Insert(GetState() ? 1 : 0, toggleInteraction);
            return interactions.ToArray();
        }
    }
}