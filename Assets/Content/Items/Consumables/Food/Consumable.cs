using System.Collections.Generic;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Substances;
using UnityEngine;
using Random = UnityEngine.Random;


namespace SS3D.Content.Items.Consumables.Food
{
    /// <summary>
    /// This class handles items that can be consumed
    /// they have an substance container which has have it's contents processed when ingested
    /// </summary>
    [RequireComponent(typeof(SubstanceContainer))]
    [RequireComponent(typeof(Item))]
    public class Consumable : InteractionTargetNetworkBehaviour, IInteractionSourceExtension, IConsumable
    {
        // What's this made of? How much of that will get in the organism per use?
        public SubstanceContainer content;
        public float contentPerUse;
    
        // Does this food leaves trash behind? Like a chocolate bar dropping it's casing
        public GameObject trashPrefab;
        public bool destroyObject;
    
        // This handles the eating sounds
        public AudioSource eatingAudio;
        public AudioClip[] sounds;

        // Verb used to display the interaction
        public string consumeVerb = "eat";

        // Time necessary to make other creatures consume
        public float feedTime = 1f;
        public GameObject loadingBarPrefab;
        // Item
        public Item item;

        private void Start()
        {
            item = GetComponent<Item>();
            content.Locked = true;
        }

        // Eat action, the target is there for when you make someone eat that food
        // For now there's no nutriment or digestion
        // TODO: Digestion
        public void ConsumeAction(GameObject origin, GameObject target = null)
        {
            // To avoid more ifs and elses
            if (target == null) target = origin;
            eatingAudio = target.transform.GetComponent<AudioSource>();

            if (!eatingAudio.isPlaying && content.TotalMoles > 0)
            {
                // Gets player's audio source
                eatingAudio = target.GetComponentInChildren<AudioSource>();

                // Randomly select the audio clip and the pitch
                eatingAudio.pitch = Random.Range(0.7f, 1.5f);
                int audioClip = Random.Range(0, sounds.Length);
                AudioClip sound = sounds[audioClip];

                // Plays the clip in the client and server
                eatingAudio.PlayOneShot(sound, eatingAudio.pitch);
                RpcPlayEatingSound(eatingAudio.pitch, audioClip);


                // Check if the item is fully consumed
                if (content.TotalMoles - contentPerUse <= 0.001)
                {
                    if (trashPrefab != null)
                    {
                        Item trash = Instantiate(trashPrefab, transform.position, transform.rotation).GetComponent<Item>();

                        // Spawn the trash in the server
                        NetworkServer.Spawn(trash.gameObject);
                        trash.GenerateNewIcon();

                        // Replaces the item with trash
                        ItemHelpers.ReplaceItem(item, trash);
                    }
                    else if (destroyObject)
                    {
                        item.Destroy();
                    }
                }
            
                // Remove contents
                content.RemoveMoles(contentPerUse);
            }
        }

        [ClientRpc]
        void RpcPlayEatingSound(float pitch, int audioClip)
        {
            eatingAudio.pitch = pitch;
            eatingAudio.PlayOneShot(sounds[audioClip]);
        }
    
    
        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> list = new List<IInteraction>();    
            list.Add(new ConsumeInteraction {ConsumeIcon = item.sprite, Verb = consumeVerb, LoadingBarPrefab = loadingBarPrefab});
    
            return list.ToArray();
        }

        public void GenerateInteractionsFromSource(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            interactions.Add(new InteractionEntry(targets[0], new ConsumeInteraction {ConsumeIcon = item.sprite, Verb = consumeVerb, LoadingBarPrefab = loadingBarPrefab }));
        }

        private class ConsumeInteraction : DelayedInteraction
        {
            public Sprite ConsumeIcon;
            public string Verb;
        
            public override string GetName(InteractionEvent interactionEvent)
            {
                return Verb;
            }

            public override bool CanInteract(InteractionEvent interactionEvent)
            {
                // Range check
                if (!InteractionExtensions.RangeCheck(interactionEvent))
                {
                    return false;
                }
            
                // easy access to shit
                GameObject source = interactionEvent.Source.GetComponentInTree<Entity>().gameObject;
                GameObject target = interactionEvent.Target?.GetComponent<Transform>().gameObject;

                // I absolutely despise how this is done, please if you have the knowledge to clean this mess do it
                Consumable itemInHand = interactionEvent.Source.GetComponent<Consumable>();
                Entity entity = source.GetComponent<Entity>();
                Entity targetEntity = target?.GetComponent<Entity>();

                // if there's no targeted entity and no item in hand
                if (targetEntity == null && itemInHand == null)
                {
                    return false;
                }
                // if there's no targeted entity and the item in hand is not the target (interactions with itself require this check)
                if (targetEntity == null && itemInHand.gameObject != target)
                {
                    return false;
                }
            
                // if the target is yourself
                if (target == itemInHand)
                {
                    Delay = 0f;
                }

                // if there's another entity as target and it's not the player
                if (targetEntity && target != source)
                {
                    Verb = "Feed";
                    Delay = itemInHand.feedTime;
                }
            
                // Consumable checks (is playing audio, is empty)
                if (itemInHand.eatingAudio.isPlaying || itemInHand.content.TotalMoles <= 0)
                {
                    return false;
                }
                

                return true;
            }       

            public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
            {
            
            }
            protected override void StartDelayed(InteractionEvent interactionEvent)
            {
                GameObject source = interactionEvent.Source?.GetComponentInTree<Entity>().gameObject;
                GameObject target = interactionEvent.Target.GetComponent<Transform>().gameObject;            
                Consumable itemInHand = source.GetComponent<Consumable>();

                // Item in hand and interacting with origin
                if (target == null) 
                {
                    itemInHand.ConsumeAction(source);
                }
                // Item not in hand
                Consumable targetedItem = target.GetComponent<Consumable>();
                if (targetedItem)
                {
                    targetedItem.ConsumeAction(source);
                }
                // Item in hand and interacting with other player
                if (target.GetComponent<Entity>())
                {
                    itemInHand.ConsumeAction(source, target);
                }
            }
        }
    }
}
