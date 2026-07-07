using System;
using Unity.Entities;

namespace SS3D.Systems.Atmospherics.ECS
{
    /// <summary>
    /// Server-side ECS world used by turf and pipe simulation jobs.
    /// </summary>
    public sealed class AtmosWorld : IDisposable
    {
        public World World { get; }

        public static AtmosWorld Create(string worldName)
        {
            var world = new World(worldName, WorldFlags.Game);
            return new AtmosWorld(world);
        }

        private AtmosWorld(World world)
        {
            World = world;
        }

        public void Dispose()
        {
            if (World.IsCreated)
                World.Dispose();
        }
    }
}
