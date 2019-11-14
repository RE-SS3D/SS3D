using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Generally used to calculate the damage distributed to certain body parts and the status of said body parts once a threshold is broken

public class Health : MonoBehaviour
{
    //current entity type (0 being organic based on enum)
    public Entities currentEntity;

    public int maxHealth = 100;
    public int currentHealth;

    //sets the max blood capacity to 560ccs
    private float maxBloodVol = 560f;
    private float currentBloodVol;

    //number of limbs that are currently bleeding
    private int numBleedingLimbs;

    //Booleans for crit and soft crit
    private bool scrit = false;
    private bool crit = false;

    //structs for status bools
    private struct limbStatus
    {
        public bool bruised;
        public bool bleeding;
        public bool crippled;
        public bool burned;
        public bool numb;
        public bool blistered;
    }

    //structs for dmgType values
    private struct HealthInfo
    {
        public float brute;
        public float burn;
        public float toxic;
        public float suffocation;
        public float maxLimbHP;
    }

    private limbStatus[] statusBools = new limbStatus[7];
    private HealthInfo[] limbHealthInfo = new HealthInfo[7];

    //enum for entity types
    public enum Entities
    {
        Organic,
        Onebi,
        Connie
    }

    //enum for each body part
    private enum SkeletalSections
    {
        torso,
        head,
        leftArm,
        rightArm,
        leftLeg,
        rightLeg,
        overall
    }

    // Start is called before the first frame update
    void Start()
    {
        //Intializes overall player health
        currentHealth = maxHealth;
        currentBloodVol = maxBloodVol;

        //Initilize the HP of each respective limb
        limbHealthInfo[(int)SkeletalSections.torso].maxLimbHP = limbHealthInfo[(int)SkeletalSections.head].maxLimbHP =
        limbHealthInfo[(int)SkeletalSections.leftArm].maxLimbHP = limbHealthInfo[(int)SkeletalSections.rightArm].maxLimbHP =
        limbHealthInfo[(int)SkeletalSections.leftLeg].maxLimbHP = limbHealthInfo[(int)SkeletalSections.rightLeg].maxLimbHP = maxHealth;

        //Initilizes Thresholds dependent on Entity type
        Thresholds.InitializeThresholds();
    }

    //Updates the damage value of corresponding damage to corresponding body part targetted
    public void dmgUpdate(int target, int dmgType , float dmg)
    {
        if (dmgType == 0)
        {
            limbHealthInfo[target].brute += dmg;
            limbHealthInfo[(int)SkeletalSections.overall].brute += dmg;
        }
        else if (dmgType == 1)
        {
            limbHealthInfo[target].burn += dmg;
            limbHealthInfo[(int)SkeletalSections.overall].burn += dmg;
        }
        else if (dmgType == 2)
            limbHealthInfo[(int)SkeletalSections.overall].toxic += dmg;
        else
            limbHealthInfo[(int)SkeletalSections.overall].suffocation += dmg;

        dmgEval(target, dmgType);
        dmgDebug();
       }

    public void dmgEval(int target, int dmgType)
    {
        //Determines the status of an affected limb based on the brute damage
        if (dmgType == 0)
        {
            //Calculates if a limb is bruised
            if (statusBools[target].bruised == false && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minBruise, Thresholds.thresholds[(int)Entities.Organic].maxBruise)* limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].brute)
            {
                statusBools[target].bruised = true;
            }

            //Calculates if a limb is bleeding. If true, begin the BloodLoss Coroutine
            if (statusBools[target].bleeding == false && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minBleed, Thresholds.thresholds[(int)Entities.Organic].maxBleed) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].brute)
            {
                statusBools[target].bleeding = true;
                numBleedingLimbs++;
                /*if(numBleedingLimbs == 1)
                StartCoroutine("BloodLoss");
                StartCoroutine("BloodRegen");
                */
            }

            //Calculates if a limb is crippled
            if (statusBools[target].crippled == false && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minCrippled, Thresholds.thresholds[(int)Entities.Organic].maxCrippled) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].brute)
            {
                statusBools[target].crippled = true;
            }
        }

        //Determines the status of an affected limb based on the burn damage
        else if (dmgType == 1)
        {
            //Calculates if a limb is burned
            if (statusBools[target].burned == false && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minBurn, Thresholds.thresholds[(int)Entities.Organic].maxBurn) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].burn)
            {
                statusBools[target].burned = true;
            }

            //Calculates if a limb is numb
            if (statusBools[target].numb == false && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minNumb, Thresholds.thresholds[(int)Entities.Organic].maxNumb) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].burn)
            {
                statusBools[target].numb = true;
            }

            //Calculates if a limb is blistered
            if (statusBools[target].blistered == false && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minBlistered, Thresholds.thresholds[(int)Entities.Organic].maxBlistered) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].burn)
            {
                statusBools[target].blistered = true;
            }

        }

        //Determines if the player is in soft crit or regular crit
        if(scrit == false && currentHealth <= 0)
        {
            scrit = true;
        }
        if(crit == false && currentHealth <= -50)
        {
            scrit = false;
            crit = true;
        }
    }

    //Coroutine to lose blood overtime. Currently (10 * the number of bleeding limbs) cc's of blood are lost every 5 seconds
    private IEnumerator BloodLoss()
    {
        while(numBleedingLimbs > 0)
        {
            currentBloodVol -= numBleedingLimbs * 10;
            //Debug.Log("Current Blood Volume: " + currentBloodVol);
            yield return new WaitForSecondsRealtime(5);
        }
    }

    //Coroutine to regain blood overtime. Currently 2 cc's of blood are gained every 7 seconds
    private IEnumerator BloodRegen()
    {
        while(currentBloodVol < maxBloodVol - 1)
        {
            currentBloodVol += 2;
            yield return new WaitForSecondsRealtime(7);
        }
        if (currentBloodVol == maxBloodVol - 1)
            currentBloodVol++;
    }

    private void dmgDebug()
    {

         Debug.Log("Torso=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.torso].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.torso].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.torso].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.torso].crippled + 
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.torso].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.torso].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.torso].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.torso].blistered +
             "\r\n Head=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.head].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.head].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.head].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.head].crippled +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.head].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.head].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.head].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.head].blistered +
             "\r\n LeftArm=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.leftArm].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.leftArm].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.leftArm].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.leftArm].crippled +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.leftArm].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.leftArm].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.leftArm].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.leftArm].blistered +
             "\r\n RightArm=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.rightArm].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.rightArm].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.rightArm].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.rightArm].crippled +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.rightArm].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.rightArm].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.rightArm].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.rightArm].blistered +
             "\r\n LeftLeg=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.leftLeg].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.leftLeg].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.leftLeg].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.leftLeg].crippled +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.leftLeg].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.leftLeg].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.leftLeg].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.leftLeg].blistered +
             "\r\n RightLeg=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.rightLeg].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.rightLeg].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.rightLeg].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.rightLeg].crippled +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.rightLeg].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.rightLeg].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.rightLeg].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.rightLeg].blistered +
             "\r\n \r\n Soft Crit? " + scrit +
             "\r\n Crit? " + crit);
           
    }

}
