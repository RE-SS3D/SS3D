using System;
using SS3D.Engine.Examine;
using UnityEngine;

namespace SS3D.Engine.Inventory
{
    [RequireComponent(typeof(Item))]
    public class Stackable : MonoBehaviour, IExaminable
    {
        public int maxStack;
        public int amountInStack;

        private void OnValidate()
        {
            if (maxStack < 2)
            {
                maxStack = 2;
            }

            if (amountInStack < 1)
            {
                amountInStack = 1;
            }
            else if (amountInStack > maxStack)
            {
                amountInStack = maxStack;
            }
            
        }

        public bool CanExamine(GameObject _) => true;

        public string GetDescription(GameObject _)
        {
            return $"{amountInStack} in stack";
        }
    }
}
