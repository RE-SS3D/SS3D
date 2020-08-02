using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace SS3D.Engine.Health
{
    public enum MetabolismDuration
    {
        Food = 3
    }

    /// <summary>
    /// Handles the intake of substances (food, drink, chemical reagents etc...)
    /// </summary>
    public class MetabolismSystem : NetworkBehaviour
    {
        public static int NutritionLevelMax = 500;
        public static int NutritionLevelStuffed = 450;
        public static int NutritionLevelNormal = 300;
        public static int NutritionLevelHungry = 200;
        public static int NutritionLevelMalnourished = 100;
        public static int NutritionLevelStarving = 0;

        //TODO: Maybe make this dependent on the heart rate?
        [SerializeField]
        [Tooltip("How often a metabolism tick occurs (in seconds)")]
        private float metabolismRate = 5f;

        [SerializeField]
        [Tooltip("Speed debuff when running and starving")]
        private float starvingRunDebuff = 2f;

        [SerializeField]
        [Tooltip("Speed debuff when walking and starving")]
        private float starvingWalkDebuff = 1f;

        public int NutritionLevel => nutritionLevel;
        private int nutritionLevel = 400;

        public HungerState HungerState
        {
            get
            {
                return hungerState;
            }
            set
            {
                // TODO: Handle hunger messages when chat is properly implemented
                //if (!NetworkManager.isHeadless && PlayerManager.LocalPlayer == gameObject)
                //{
                //    if (value == HungerState.Full && this.HungerState != HungerState.Full)
                //        Chat.AddExamineMsgToClient("You're stuffed!");

                //    if (value == HungerState.Normal && this.HungerState != HungerState.Normal)
                //        Chat.AddExamineMsgToClient("You're satiated.");

                //    if (value == HungerState.Hungry && this.HungerState != HungerState.Hungry)
                //        Chat.AddExamineMsgToClient("You feel hungry.");

                //    if (value == HungerState.Malnourished && this.HungerState != HungerState.Malnourished)
                //        Chat.AddWarningMsgToClient("Your stomach rumbles violently.");

                //    if (value == HungerState.Starving && this.HungerState != HungerState.Starving)
                //        Chat.AddWarningMsgToClient("Your malnourished body aches!");
                //}

                hungerState = value;
            }
        }

        private HungerState hungerState;

        public bool IsHungry => HungerState >= HungerState.Hungry;
        public bool IsStarving => HungerState == HungerState.Starving;

        // <summary>
        /// How much hunger is applied per metabolism tick
        /// </summary>
        public int HungerRate { get; set; } = 1;

        private BloodSystem bloodSystem;
        private List<MetabolismEffect> effects;
        private float tick = 0;
        private bool appliedStarvingDebuff;

        void Start()
        {
            bloodSystem = GetComponent<BloodSystem>();
        }


        void Update()
        {
            //Server only
            if (isServer)
            {
                tick += Time.deltaTime;

                if (tick >= metabolismRate && !bloodSystem.HeartStopped) //Metabolism tick
                {
                    //Apply hunger
                    nutritionLevel -= HungerRate;

                    //Apply effects
                    for (int i = effects.Count - 1; i >= 0; i--)
                    {
                        MetabolismEffect e = effects[i];

                        if (e.duration <= 0)
                        {
                            effects.RemoveAt(i);
                            continue;
                        }

                        nutritionLevel += e.totalNutrients / e.initialDuration;
                        bloodSystem.ToxinLevel += e.totalToxins / e.initialDuration;
                        e.duration--;
                        effects[i] = e;
                    }

                    nutritionLevel = Mathf.Clamp(nutritionLevel, 0, NutritionLevelMax);

                    HungerState oldState = this.HungerState;

                    if (nutritionLevel > NutritionLevelStuffed) //TODO: Make character nauseous when he's too full
                        HungerState = HungerState.Full;
                    else if (nutritionLevel > NutritionLevelNormal)
                        HungerState = HungerState.Normal;
                    else if (nutritionLevel > NutritionLevelHungry)
                        HungerState = HungerState.Hungry;
                    else if (nutritionLevel > NutritionLevelMalnourished)
                        HungerState = HungerState.Malnourished;
                    else
                        HungerState = HungerState.Starving;

                    if (oldState != this.HungerState) //HungerState was altered, send new one to player
                        UpdateHungerStateMessage.Send(this.gameObject, HungerState);

                    tick = 0;
                }
            }

            //Client and server
            if (HungerState == HungerState.Starving & !appliedStarvingDebuff)
            {
                ApplySpeedDebuff();
            }
            else if (HungerState != HungerState.Starving & appliedStarvingDebuff)
            {
                RemoveSpeedDebuff();
            }
        }

        // <summary>
        /// Applies the speed debuff when starving
        /// </summary>
        private void ApplySpeedDebuff()
        {
            // TODO: Apply debuff to the actual player
            //playerMove.ServerChangeSpeed( run: playerMove.RunSpeed - starvingRunDebuff,
            //    walk: playerMove.WalkSpeed - starvingWalkDebuff);
            appliedStarvingDebuff = true;
        }

        // <summary>
        /// Removes the speed debuff when starving
        /// </summary>
        private void RemoveSpeedDebuff()
        {
            // TODO: Remove debuff from the actual player
            //playerMove.ServerChangeSpeed( run: playerMove.RunSpeed + starvingRunDebuff,
            //    walk: playerMove.WalkSpeed + starvingWalkDebuff);
            appliedStarvingDebuff = false;
        }

        /// <summary>
        /// Adds a MetabolismEffect to the system. The effect is applied every metabolism tick.
        /// </summary>
        public void AddEffect(MetabolismEffect effect)
        {
            effects.Add(effect);
        }
    }
}