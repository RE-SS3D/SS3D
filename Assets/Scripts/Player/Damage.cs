using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This code is used for testing the Health of limbs and such resulting from specific types of damage
//In order to deal damage and get an output, press 'W'.
//The target of this script should be the player being attacked

public class Damage : MonoBehaviour
{
    public GameObject playerTarget;
    Health attack;
    public weaponTags[] currentTags;

    public enum weaponTags
    {
        blunt,
        cut,
        stab
    }

    public enum DamageType
    {
        brute,
        burn,
        toxic,
        suffocation
    }

    public enum SectionTarget
    {
        torso,
        head,
        leftArm,
        rightArm,
        leftLeg,
        rightLeg
    }
    public DamageType currentDmgType;
    public SectionTarget currentTarget;
    public float dmgValue;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            attack = playerTarget.GetComponent<Health>();
            attack.dmgUpdate((int)currentTarget, (int)currentDmgType, dmgValue, currentTags);
        }
    }
}
