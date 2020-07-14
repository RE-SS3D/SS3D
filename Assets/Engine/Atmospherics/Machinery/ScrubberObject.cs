using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class ScrubberObject : MonoBehaviour, IAtmosLoop
    {
        public enum OperatingMode
        {
            Off,
            Scrubbing,
            Siphoning
        }

        public enum Range
        {
            Normal,
            Extended
        }

        public float MaxPressure = 4500f;
        public float TargetPressure = 101f;

        public bool filterOxygen = false;
        public bool filterNitrogen = false;
        public bool filterCarbonDioxide = false;
        public bool filterPlasma = false;

        public bool filterActive = false;
        public PipeLayer pipeLayer;

        private float _targetPressure;
        private PipeObject connectedPipe;

        void Update()
        {
            if (_targetPressure != TargetPressure)
            {
                _targetPressure = Mathf.Clamp(TargetPressure, 0, MaxPressure);
                TargetPressure = _targetPressure;
            }
        }

        public void Initialize()
        {
            // We only check the pipes that are on our own tile
            TileObject tileObject = GetComponentInParent<TileObject>();
            PipeObject[] pipes = tileObject.GetComponentsInChildren<PipeObject>();

            foreach (PipeObject pipe in pipes)
            {
                // Only take the pipe which matches the seleced layer
                if (pipe.layer == pipeLayer)
                {
                    connectedPipe = pipe;
                }
            }
        }

        public void Step()
        {
            AtmosObject input = GetComponentInParent<TileObject>().atmos;
            PipeObject output = connectedPipe;
            AtmosContainer outputContainer = output.GetAtmosContainer();

            if (input == null || input.GetTotalMoles() == 0 || output == null)
                return;

            // If the output pressure is acceptable
            if (output.GetPressure() <= TargetPressure)
            {

            }
        }

        public void SetTileNeighbour(TileObject tile, int index)
        {
            return;
        }

        public void SetAtmosNeighbours()
        {
            return;
        }
    }
}