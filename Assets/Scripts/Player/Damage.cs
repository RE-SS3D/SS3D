using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This code is used for testing the Health of limbs and such resulting from specific types of damage
//In order to change the targetted location use 'Q' and 'E'
//In order to change the damage tpye use 'A' and 'D'
//In order to change the amount of damage being inflicted use 'O' and 'P'
//In order to deal damage and get an output, press 'W'.
//The target of this script sould be the player being attacked

public class Damage : MonoBehaviour
{
    public GameObject playerTarget;
    Health attack;

    //used for testing
    public static int currentTarget = 0;
    public static int currentDmgType = 0;
    public static float dmg = 0.0f;

    public enum DamageType
    {
        brute,
        burn,
        toxic,
        suffocation
    }

    enum SectionTarget
    {
        torso,
        head,
        leftArm,
        rightArm,
        leftLeg,
        rightLeg
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && currentTarget > 0)
        {
            currentTarget--;
            Debug.Log("Traget ID: " + currentTarget);
        }
        else if (Input.GetKeyDown(KeyCode.Q) && currentTarget == 0)
        {
            currentTarget = 5;
            Debug.Log("Traget ID: " + currentTarget);
        }

        if (Input.GetKeyDown(KeyCode.E) && currentTarget < 5)
        {
            currentTarget++;
            Debug.Log("Traget ID: " + currentTarget);
        }
        else if (Input.GetKeyDown(KeyCode.E) && currentTarget == 5)
        {
            currentTarget = 0;
            Debug.Log("Traget ID: " + currentTarget);
        }

        if (Input.GetKeyDown(KeyCode.A) && currentDmgType > 0)
        {
            currentDmgType--;
            Debug.Log("Damage Type: " + currentDmgType);
        }
        else if(Input.GetKeyDown(KeyCode.A) && currentDmgType == 0)
        {
            currentDmgType = 3;
            Debug.Log("Damage Type: " + currentDmgType);
        }

        if (Input.GetKeyDown(KeyCode.D) && currentDmgType < 3)
        {
            currentDmgType++;
            Debug.Log("Damage Type: " + currentDmgType);
        }
        else if (Input.GetKeyDown(KeyCode.D) && currentDmgType == 3)
        {
            currentDmgType = 0;
            Debug.Log("Damage Type: " + currentDmgType);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            dmg --;
            Debug.Log(dmg);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            dmg ++;
            Debug.Log(dmg);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            attack = playerTarget.GetComponent<Health>();
            attack.dmgUpdate(currentTarget, currentDmgType, dmg);
        }
    }
}
