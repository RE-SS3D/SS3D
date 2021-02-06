using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public enum AtmosContainerType
    {
        Tile,
        Tank,
        Pipe
    }

    public class AtmosContainer
    {
        public AtmosContainerType ContainerType { get; set; }
        public float Volume { get; set; } = 2.5f;
        private  float temperature = 293f;
        private float[] gasses = new float[Gas.numOfGases];
        

        public float GetGas(AtmosGasses index)
        {
            return gasses[(int)index];
        }

        public float GetGas(int index)
        {
            return gasses[index];
        }

        public float[] GetGasses()
        {
            return gasses;
        }

        public float GetTemperature()
        {
            return temperature;
        }

        public void SetTemperature(float temperature)
        {
            if (temperature >= 0)
                this.temperature = temperature;
        }

        public void AddGas(AtmosGasses gas, float amount)
        {
            gasses[(int)gas] = Mathf.Max(gasses[(int)gas] + amount, 0);
        }

        public void AddGas(int index, float amount)
        {
            gasses[index] = Mathf.Max(gasses[index] + amount, 0);
        }

        public void RemoveGas(AtmosGasses gas, float amount)
        {
            gasses[(int)gas] = Mathf.Max(gasses[(int)gas] - amount, 0);
        }

        public void RemoveGas(int index, float amount)
        {
            gasses[index] = Mathf.Max(gasses[index] - amount, 0);
        }

        public void SetGasses(float[] amounts)
        {
            for (int i = 0; i < Mathf.Min(amounts.GetLength(0), Gas.numOfGases); ++i)
            {
                gasses[i] = Mathf.Max(amounts[i], 0);
            }
        }

        public void MakeEmpty()
        {
            for (int i = 0; i < Gas.numOfGases; ++i)
            {
                gasses[i] = 0f;
            }
        }

        public void AddHeat(float temp)
        {
            temperature += Mathf.Max(temp - temperature, 0f) / GetSpecificHeat() * (100 / GetTotalMoles()) * Gas.dt;
        }

        public void RemoveHeat(float temp)
        {
            temperature -= Mathf.Max(temp - temperature, 0f) / GetSpecificHeat() * (100 / GetTotalMoles()) * Gas.dt;
            if (temperature < 0f)
            {
                temperature = 0f;
            }
        }

        public float GetTotalMoles()
        {
            float moles = 0f;
            for (int i = 0; i < Gas.numOfGases; ++i)
            {
                moles += gasses[i];
            }
            return moles;
        }

        public float GetPressure()
        {
            return GetTotalMoles() * Gas.gasConstant * temperature / Volume / 1000f;
        }

        public float GetPartialPressure(int index)
        {
            return (gasses[index] * Gas.gasConstant * temperature) / Volume / 1000f;
        }

        public float GetPartialPressure(AtmosGasses gas)
        {
            return (gasses[(int)gas] * Gas.gasConstant * temperature) / Volume / 1000f;
        }

        public float GetSpecificHeat()
        {
            float temp = 0f;
            temp += gasses[(int)AtmosGasses.Oxygen] * 2f;           // Oxygen, 20
            temp += gasses[(int)AtmosGasses.Nitrogen] * 20f;        // Nitrogen, 200
            temp += gasses[(int)AtmosGasses.CarbonDioxide] * 3f;    // Carbon Dioxide, 30
            temp += gasses[(int)AtmosGasses.Plasma] * 1f;           // Plasma, 10
            return temp / GetTotalMoles();
        }

        public float GetMass()
        {
            float mass = 0f;
            mass += gasses[(int)AtmosGasses.Oxygen] * 32f;          // Oxygen
            mass += gasses[(int)AtmosGasses.Nitrogen] * 28f;        // Nitrogen
            mass += gasses[(int)AtmosGasses.CarbonDioxide] * 44f;   // Carbon Dioxide
            mass += gasses[(int)AtmosGasses.Plasma] * 78f;          // Plasma
            return mass;     // Mass in grams
        }
    }
}