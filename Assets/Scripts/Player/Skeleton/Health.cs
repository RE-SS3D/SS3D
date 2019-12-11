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

    //Array for establishing max limb HP
    public float[] maxLimbHP = new float[7];

    //Used to keep track of current crit status
    private critStatus critLevel;

    //structs for status bools
    private struct limbStatus
    {
        public bool bruised;
        public bool bleeding;
        public bool crippled;
        public bool detached;
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
        Organic
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

    [System.Flags]
    private enum critStatus
    {
        STABLE,
        CRIT_LEVEL_SOFT,
        CRIT_LEVEL_HARD
    }

    // Start is called before the first frame update
    void Start()
    {
        //Intializes overall player health
        currentHealth = maxHealth;
        currentBloodVol = maxBloodVol;
        critLevel = critStatus.STABLE;

        //Initilize the HP of each respective limb
        for(int i = (int)SkeletalSections.torso; i <= (int)SkeletalSections.overall; i++)
        {
            limbHealthInfo[i].maxLimbHP = maxLimbHP[i];
        }

        //Initilizes Thresholds dependent on Entity type
        Thresholds.InitializeThresholds();
    }

    //Updates the damage value of corresponding damage to corresponding body part targetted
    public void dmgUpdate(int target, int dmgType , float dmg, Damage.weaponTags[] tags)
    {
        switch (dmgType)
        {
            case 0:             
                limbHealthInfo[target].brute += dmg;
                limbHealthInfo[(int)SkeletalSections.overall].brute += dmg;
                bruteDmgEval(target, tags);
                break;
            case 1: 
                limbHealthInfo[target].burn += dmg;
                limbHealthInfo[(int)SkeletalSections.overall].burn += dmg;
                burnDmgEval(target, tags);
                break;
            case 2:
                limbHealthInfo[(int)SkeletalSections.overall].toxic += dmg;
                break;
            case 3:
                limbHealthInfo[(int)SkeletalSections.overall].suffocation += dmg;
                break;
        }
        critEval();
        dmgDebug();
    }

    private void bruteDmgEval(int target, Damage.weaponTags[] tags)
    {
        for (int i = 0; i < tags.Length; i++)
        {
            if ((int)tags[i] == (int)Damage.weaponTags.blunt)
            {
                if (!statusBools[target].bruised && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minBruise, Thresholds.thresholds[(int)Entities.Organic].maxBruise) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].brute)
                {
                    statusBools[target].bruised = true;
                }

                //Calculates if a limb is crippled
                if ((!statusBools[target].crippled && !statusBools[target].detached) && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minCrippled, Thresholds.thresholds[(int)Entities.Organic].maxCrippled) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].brute)
                {
                    statusBools[target].crippled = true;
                }
            }

            else if ((int)tags[i] == (int)Damage.weaponTags.stab || (int)tags[i] == (int)Damage.weaponTags.cut)
            {
                //Calculates if a limb is bleeding. If true, begin the BloodLoss Coroutine
                if (!statusBools[target].bleeding && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minBleed, Thresholds.thresholds[(int)Entities.Organic].maxBleed) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].brute)
                {
                    statusBools[target].bleeding = true;
                    numBleedingLimbs++;
                    /*if(numBleedingLimbs == 1)
                    StartCoroutine("BloodLoss");
                    StartCoroutine("BloodRegen");
                    */
                }

                //Calculates if a limb is crippled
                if (target != (int)SkeletalSections.torso && !statusBools[target].detached && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minDetached, Thresholds.thresholds[(int)Entities.Organic].maxDetached) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].brute)
                {
                    statusBools[target].detached = true;
                    //BodyPart.severMe();
                }
            }
        }
    }

    private void burnDmgEval(int target, Damage.weaponTags[] tags)
    {
        //Calculates if a limb is burned
        if (!statusBools[target].burned && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minBurn, Thresholds.thresholds[(int)Entities.Organic].maxBurn) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].burn)
        {
            statusBools[target].burned = true;
        }

        //Calculates if a limb is numb
        if (!statusBools[target].numb && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minNumb, Thresholds.thresholds[(int)Entities.Organic].maxNumb) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].burn)
        {
            statusBools[target].numb = true;
        }

        //Calculates if a limb is blistered
        if (!statusBools[target].blistered && Random.Range(Thresholds.thresholds[(int)Entities.Organic].minBlistered, Thresholds.thresholds[(int)Entities.Organic].maxBlistered) * limbHealthInfo[target].maxLimbHP < limbHealthInfo[target].burn)
        {
            statusBools[target].blistered = true;
        }
    }

    private void critEval()
    {
        //Determines if the player is in soft crit or regular crit
        if(currentHealth <= Thresholds.thresholds[(int)Entities.Organic].hardCrit)
        {
            critLevel = critStatus.CRIT_LEVEL_HARD;
        }
        else if (currentHealth <= Thresholds.thresholds[(int)Entities.Organic].softCrit)
        {
            critLevel = critStatus.CRIT_LEVEL_SOFT;
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
             "\r\n  Detached? " + statusBools[(int)SkeletalSections.torso].detached +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.torso].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.torso].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.torso].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.torso].blistered +
             "\r\n Head=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.head].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.head].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.head].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.head].crippled +
             "\r\n  Detached? " + statusBools[(int)SkeletalSections.head].detached +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.head].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.head].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.head].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.head].blistered +
             "\r\n LeftArm=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.leftArm].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.leftArm].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.leftArm].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.leftArm].crippled +
             "\r\n  Detached? " + statusBools[(int)SkeletalSections.leftArm].detached +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.leftArm].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.leftArm].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.leftArm].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.leftArm].blistered +
             "\r\n RightArm=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.rightArm].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.rightArm].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.rightArm].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.rightArm].crippled +
             "\r\n  Detached? " + statusBools[(int)SkeletalSections.rightArm].detached +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.rightArm].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.rightArm].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.rightArm].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.rightArm].blistered +
             "\r\n LeftLeg=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.leftLeg].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.leftLeg].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.leftLeg].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.leftLeg].crippled +
             "\r\n  Detached? " + statusBools[(int)SkeletalSections.leftLeg].detached +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.leftLeg].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.leftLeg].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.leftLeg].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.leftLeg].blistered +
             "\r\n RightLeg=> \r\n  Brute: " + limbHealthInfo[(int)SkeletalSections.rightLeg].brute +
             "\r\n  Bruised? " + statusBools[(int)SkeletalSections.rightLeg].bruised +
             "\r\n  Bleeding? " + statusBools[(int)SkeletalSections.rightLeg].bleeding +
             "\r\n  Crippled? " + statusBools[(int)SkeletalSections.rightLeg].crippled +
             "\r\n  Detached? " + statusBools[(int)SkeletalSections.rightLeg].detached +
             "\r\n  Burn: " + limbHealthInfo[(int)SkeletalSections.rightLeg].burn +
             "\r\n  Burned? " + statusBools[(int)SkeletalSections.rightLeg].burned +
             "\r\n  Numb? " + statusBools[(int)SkeletalSections.rightLeg].numb +
             "\r\n  Blistered? " + statusBools[(int)SkeletalSections.rightLeg].blistered +
             "\r\n \r\n Crit Level: " + critLevel);
           
    }

}
