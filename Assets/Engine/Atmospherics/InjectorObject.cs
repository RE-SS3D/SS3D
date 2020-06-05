using SS3D.Engine.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class InjectorObject : MonoBehaviour, IAtmosStep
    {
        private TileObject tileNeighbour;
        private PipeObject atmosNeighbour;
        private TileManager tileManager;
        TileObject tile;

        // Start is called before the first frame update
        void Start()
        {
            //// Get a connected pipe
            tile = GetComponentInParent<TileObject>();
            //string[] coords = tile.name.Split(',');
            //int x = Int32.Parse(coords[0].Replace("[", ""));
            //int y = Int32.Parse(coords[1].Replace("]", ""));

            //tileManager = FindObjectOfType<TileManager>();
            //Tuple<int, int> tileCoordinates = DirectionHelper.ToCardinalVector(Direction.South);
            //TileObject tileNeighbour = tileManager.GetTile(tileCoordinates.Item1 + x, tileCoordinates.Item2 + y);
            //this.tileNeighbour = tileNeighbour;

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void setTileNeighbour(TileObject neighbour)
        {
            tileNeighbour = neighbour;
        }

        public void setPipeNeighbour()
        {
            atmosNeighbour = tileNeighbour.transform.GetComponentInChildren<PipeObject>();
        }

        public void Init()
        {
            setPipeNeighbour();
        }

        public void Step()
        {
            if (atmosNeighbour)
            {
                if (atmosNeighbour.GetPressure() > 0f)
                {
                    float[] tileGas = tile.atmos.GetAtmosContainer().GetGasses();
                    for (int i = 0; i < Gas.numOfGases; i++)
                    {
                        tileGas[i] += atmosNeighbour.GetAtmosContainer().GetGasses()[i];
                        atmosNeighbour.GetAtmosContainer().GetGasses()[i] = 0f;
                        atmosNeighbour.SetStateActive();
                        tile.atmos.SetBlocked(false);
                    }
                }
            }
        }
    }
}