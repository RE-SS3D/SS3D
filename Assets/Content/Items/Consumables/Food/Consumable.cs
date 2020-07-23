using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using SS3D.Engine.Substances;
using UnityEngine;
using Random = UnityEngine.Random;


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
    public AudioSource audio;
    public AudioClip[] sounds;

    // Verb used to display the interaction
    public string consumeVerb = "eat";

    // Time necessary to make other creatures consume
    public float feedTime = 1f;
    public GameObject LoadingBarPrefab;
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
        audio = target.transform.GetComponent<AudioSource>();

        if (!audio.isPlaying && content.CurrentVolume > 0)
        {
            // Gets player's audio source
            audio = target.GetComponentInChildren<AudioSource>();
            
            // Randomly select the audio clip and the pitch
            audio.pitch = Random.Range(0.7f,1.5f);
            int audioClip = Random.Range(0, sounds.Length);
            AudioClip sound = sounds[audioClip];
            
            // Plays the clip in the client and server
            audio.PlayOneShot(sound, audio.pitch);
            RpcPlayEatingSound(audio.pitch, audioClip);
            
            Item itemInHand = origin.GetComponentInChildren<Hands>().GetItemInHand();
            
            // Does the portion is in the last one? Does it leaves trash? 
            if (content.CurrentVolume == 1 && trashPrefab != null )
            {
                Item trash = Instantiate(trashPrefab, transform.position, transform.rotation).GetComponent<Item>();
                
                // Spawn the trash in the server
                NetworkServer.Spawn(trash.gameObject);
                trash.GenerateNewIcon();

                if (itemInHand == null)
                {
                    // Destroys the item in the hand,  it's all networked
                    item.Destroy();
                }
                else
                {
                    // Replaces the item in the hand for the trash instance, it's all networked
                    ItemHelpers.ReplaceItem(itemInHand, trash);
                }
            }
            // Last one? Should it destroy the object?
            if (content.CurrentVolume - contentPerUse <= 0 && destroyObject)
            {
                item.Destroy();
            }
            // Not last one?
            else
            {
                content.RemoveMoles(contentPerUse);
                Debug.Log(target.name + " consumed " + transform.name);
            }
        }
    }

    [ClientRpc]
    void RpcPlayEatingSound(float pitch, int audioClip)
    {
        audio.pitch = pitch;
        audio.PlayOneShot(sounds[audioClip]);
    }
    
    
    public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
    {
        List<IInteraction> list = new List<IInteraction>();    
            list.Add(new ConsumeInteraction {icon = item.sprite, Verb = consumeVerb, LoadingBarPrefab = LoadingBarPrefab});
    
        return list.ToArray();
    }

    public void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
    {
        interactions.Add(new InteractionEntry(targets[0], new ConsumeInteraction {icon = item.sprite, Verb = consumeVerb, LoadingBarPrefab = LoadingBarPrefab }));
    }

    private class ConsumeInteraction : DelayedInteraction
    {
        public Sprite icon;
        public string Verb;
        
        public override string GetName(InteractionEvent interactionEvent)
        {
            return Verb;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // easy access to shit
            GameObject source = interactionEvent.Source.GetComponentInTree<Creature>().gameObject;
            GameObject target = interactionEvent.Target?.GetComponent<Transform>().gameObject;

            // I absolutely despise how this is done, please if you have the knowledge to clean this mess do it
            Consumable itemInHand = source.GetComponentInChildren<Hands>().GetItemInHand()?.GetComponent<Consumable>();
            Creature creature = source.GetComponent<Creature>();
            Creature targetCreature = target?.GetComponent<Creature>();

            Debug.Log("source:  " + source.name + " target: " + target?.name + " item: " + itemInHand?.gameObject.name);

            // if there's no targeted creature and no item in hand
            if (targetCreature == null && itemInHand == null)
            {
                return false;
            }
            // if there's no targeted creature and the item in hand is not the target (interactions with itself require this check)
            if (targetCreature == null && itemInHand.gameObject != target)
            {
                return false;
            }
            
            // if the target is yourself
            if (target == itemInHand)
            {
                Delay = 0f;
            }

            // if there's another creature as target and it's not the player
            if (targetCreature && target != source)
            {
                Verb = "Feed";
                Delay = itemInHand.feedTime;
            }

            // Range check
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                // Consumable checks (is playing audio, is empty)
                if (!itemInHand.audio.isPlaying && itemInHand.content.CurrentVolume > 0)
                    return false;
            }

            return true;
        }       

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            
        }
        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            GameObject source = interactionEvent.Source?.GetComponentInTree<Creature>().gameObject;
            GameObject target = interactionEvent.Target.GetComponent<Transform>().gameObject;            
            Consumable itemInHand = source?.GetComponentInChildren<Hands>().GetItemInHand()?.GetComponent<Consumable>();
            
            //Debug.Log("source:  " + source.name + " target: " + target?.name + " item: " + itemInHand?.gameObject.name);
			
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
            if (target.GetComponent<Creature>())
            {
                itemInHand.ConsumeAction(source, target);
            }
        }
    }
}
