using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;
using UnityEngine.TestTools;

namespace EditorTests
{
    public class ElectricitySystemTests
    {
        /// <summary>
        /// </summary>
        [Test]
        public void BatteriesChargeAtEqualRatesOrGetFull()
        {
            ElectricitySystem system = CreateElectricSystem();

            system.HandleCircuitsUpdate();
        }

        private static ElectricitySystem CreateElectricSystem()
        {
            GameObject systemGo = new GameObject();
            systemGo.AddComponent<ElectricitySystem>();
            ElectricitySystem system = systemGo.GetComponent<ElectricitySystem>();
            system.OnStartServer();
            return system;
        }
    }
}
