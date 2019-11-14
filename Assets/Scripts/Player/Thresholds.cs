using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Used to hold the thresholds of each entity type so that the values can all be stored in one place and won't have to be reinitialized for each entity that spawns

public class Thresholds : MonoBehaviour
{

    public struct DamageThresholds
    {
        public float minBruise;
        public float maxBruise;
        public float minBleed;
        public float maxBleed;
        public float minCrippled;
        public float maxCrippled;
        public float minDetached;
        public float maxDetached;
        public float minBurn;
        public float maxBurn;
        public float minNumb;
        public float maxNumb;
        public float minBlistered;
        public float maxBlistered;
    }

    public static DamageThresholds[] thresholds = new DamageThresholds[7];

    public enum Entities
    {
        Organic,
        Weasle
    }

    // Update is called once per frame
    public static void InitializeThresholds()
    {
        //Initilizes the health thresholds for organic entities
        //Brute
        thresholds[(int)Entities.Organic].minBruise = .15f;
        thresholds[(int)Entities.Organic].maxBruise = .25f;
        thresholds[(int)Entities.Organic].minBleed = .35f;
        thresholds[(int)Entities.Organic].maxBleed = .45f;
        thresholds[(int)Entities.Organic].minCrippled = .55f;
        thresholds[(int)Entities.Organic].maxCrippled = .75f;
        thresholds[(int)Entities.Organic].minDetached = .55f;
        thresholds[(int)Entities.Organic].maxDetached = .75f;
        //Burn
        thresholds[(int)Entities.Organic].minBurn = .15f;
        thresholds[(int)Entities.Organic].maxBurn = .25f;
        thresholds[(int)Entities.Organic].minNumb = .35f;
        thresholds[(int)Entities.Organic].maxNumb = .45f;
        thresholds[(int)Entities.Organic].minBlistered = .55f;
        thresholds[(int)Entities.Organic].maxBlistered = .75f;
    }
}
