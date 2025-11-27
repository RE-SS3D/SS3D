using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics
{
    public class PipeNet
    {
        private readonly List<IAtmosPipe> _pipes = new();

        private float4 _gassesToAdd = float4.zero;
        private float4 _gassesToRemove = float4.zero;

        public List<IAtmosPipe> GetPipes(TileLayer layerType)
        {
            return _pipes.Where(x => x.PlacedTileObject.Layer == layerType).ToList();
        }

        public void AddPipe(IAtmosPipe pipe)
        {
            _pipes.Add(pipe);
        }

        public void RemovePipe(IAtmosPipe pipe)
        {
            _pipes.Remove(pipe);
        }

        public void Equalize()
        {
            float4 gasses = float4.zero;

            foreach (IAtmosPipe pipe in _pipes)
            {
                gasses += pipe.AtmosObject.CoreGasses;
            }

            float4 gassesPerPipe = gasses / _pipes.Count;

            foreach (IAtmosPipe pipe in _pipes)
            {
                AtmosObject atmos = pipe.AtmosObject;
                atmos.ClearCoreGasses();
                atmos.AddCoreGasses(gassesPerPipe);
                pipe.AtmosObject = atmos;
            }
        }

        public void AddCoreGasses(float4 gasses)
        {
            _gassesToAdd += gasses;
        }

        public void RemoveCoreGasses(float4 gasses)
        {
            _gassesToRemove += gasses;
        }

        public void ApplyGasses()
        {
            AddGasses();
            RemoveGasses();
        }

        private void AddGasses()
        {
            float4 gassesPerPipe = _gassesToAdd / _pipes.Count;

            foreach (IAtmosPipe pipe in _pipes)
            {
                AtmosObject atmos = pipe.AtmosObject;
                atmos.AddCoreGasses(gassesPerPipe);
                pipe.AtmosObject = atmos;
            }

            _gassesToAdd = 0f;
        }

        private void RemoveGasses()
        {
            float4 gassesPerPipe = _gassesToRemove / _pipes.Count;

            foreach (IAtmosPipe pipe in _pipes)
            {
                AtmosObject atmos = pipe.AtmosObject;
                atmos.RemoveCoreGasses(gassesPerPipe);
                pipe.AtmosObject = atmos;
            }

            _gassesToRemove = 0f;
        }
    }
}
