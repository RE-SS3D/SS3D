using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Life
{
    public float MaxLife { get; private set; }
    public float CurrentLife { get; private set; }

    public bool IsAlive => CurrentLife > 0;

    public void RemoveLife(float amount)
    {
        CurrentLife -= amount;
    }

    public void AddLife(float amount)
    {
        if (CurrentLife + amount < MaxLife)
            CurrentLife += amount;
        else
            CurrentLife = MaxLife;
    }

}
