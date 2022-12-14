using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Logging;
using SS3D.Tilemaps.Adjacency;
using SS3D.Tilemaps.Enums;
using SS3D.Tilemaps.Objects;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static UnityEditor.PlayerSettings;
using Random = UnityEngine.Random;

namespace SS3D.Tilemaps.Adjacency
{
    public class TileAdjacencySystem : NetworkedSystem
    {
        private TileSystem _tileSystem;

        protected override void OnStart()
        {
            base.OnStart();

            _tileSystem = GameSystems.Get<TileSystem>();
        }

        private void Update()
        {
            // Just for debugging connection checking
            if (Input.GetKeyDown(KeyCode.Space))
            {
                int _bitmask = 0;
                string txt = "";

                var posf = GameObject.Find("TileHint").transform.position;
                var pos = new Vector3Int((int)posf.x, (int)posf.y, (int)posf.z);

                _tileSystem.GetTile(pos).Objects.TryGetValue(TileObjectLayer.Turf, out TileObject tileObject);

                TileObjectLayer objectLayer = tileObject._objectLayer;
                TileObjects type = tileObject.Id;

                if (IsTileType(new Vector3Int(pos.x + 1, pos.y, pos.z), type, objectLayer))
                {
                    _bitmask += (int)AdjacencyDirection.Right;
                    txt += "R-";
                }
                if (IsTileType(new Vector3Int(pos.x - 1, pos.y, pos.z), type, objectLayer))
                {
                    _bitmask += (int)AdjacencyDirection.Left;
                    txt += "L-";
                }
                if (IsTileType(new Vector3Int(pos.x, pos.y, pos.z + 1), type, objectLayer))
                {
                    _bitmask += (int)AdjacencyDirection.Top;
                    txt += "T-";
                }
                if (IsTileType(new Vector3Int(pos.x, pos.y, pos.z - 1), type, objectLayer))
                {
                    _bitmask += (int)AdjacencyDirection.Bottom;
                    txt += "B-";
                }
                if (IsTileType(new Vector3Int(pos.x + 1, pos.y, pos.z + 1), type, objectLayer)) {
                    _bitmask += (int)AdjacencyDirection.TopRight;
                    txt += "TR-";
                }
                if (IsTileType(new Vector3Int(pos.x + 1, pos.y, pos.z - 1), type, objectLayer))
                {
                    _bitmask += (int)AdjacencyDirection.BottomRight;
                    txt += "BR-";
                }
                if (IsTileType(new Vector3Int(pos.x - 1, pos.y, pos.z + 1), type, objectLayer))
                {
                    _bitmask += (int)AdjacencyDirection.TopLeft;
                    txt += "TL-";
                }
                if (IsTileType(new Vector3Int(pos.x - 1, pos.y, pos.z - 1), type, objectLayer))
                {
                    _bitmask += (int)AdjacencyDirection.BottomLeft;
                    txt += "BL-";
                }

                Debug.Log("Bitmask: " + _bitmask);
                Debug.Log(txt);
            }
        }

        public void GetTileObjectMesh(Vector3Int pos, TileObject tileObject)
        {
            uint _bitmask = 0;

            TileObjectLayer objectLayer = tileObject._objectLayer;
            TileObjects type = tileObject.Id;

            if (IsTileType(new Vector3Int(pos.x + 1, pos.y, pos.z), type, objectLayer))
                _bitmask += (int)AdjacencyDirection.Right;

            if (IsTileType(new Vector3Int(pos.x - 1, pos.y, pos.z), type, objectLayer))
                _bitmask += (int)AdjacencyDirection.Left;

            if (IsTileType(new Vector3Int(pos.x, pos.y, pos.z + 1), type, objectLayer))
                _bitmask += (int)AdjacencyDirection.Top;

            if (IsTileType(new Vector3Int(pos.x, pos.y, pos.z - 1), type, objectLayer))
                _bitmask += (int)AdjacencyDirection.Bottom;

            // Only checks for diagonals if horizontal and vertical positions are filled
            int mask;

            mask = (int)AdjacencyDirection.Top | (int)AdjacencyDirection.Right;
            if ((IsTileType(new Vector3Int(pos.x + 1, pos.y, pos.z + 1), type, objectLayer))
                & ((_bitmask & mask) == mask))
                _bitmask += (int)AdjacencyDirection.TopRight;

            mask = (int)AdjacencyDirection.Bottom | (int)AdjacencyDirection.Right;
            if ((IsTileType(new Vector3Int(pos.x + 1, pos.y, pos.z - 1), type, objectLayer))
                &((_bitmask & mask) == mask))
                _bitmask += (int)AdjacencyDirection.BottomRight;

            mask = (int)AdjacencyDirection.Top | (int)AdjacencyDirection.Left;
            if ((IsTileType(new Vector3Int(pos.x - 1, pos.y, pos.z + 1), type, objectLayer))
                &((_bitmask & mask) == mask))
                _bitmask += (int)AdjacencyDirection.TopLeft;

            mask = (int)AdjacencyDirection.Bottom | (int)AdjacencyDirection.Left;
            if ((IsTileType(new Vector3Int(pos.x - 1, pos.y, pos.z - 1), type, objectLayer))
                &((_bitmask & mask) == mask))
                _bitmask += (int)AdjacencyDirection.BottomLeft;

            UpdateTileObjectMesh(tileObject, _bitmask);
            UpdateTileRotation(tileObject, _bitmask);
        }

        private bool IsTileType(Vector3Int pos, TileObjects type, TileObjectLayer objectLayer)
        {
            _tileSystem.GetTile(pos).Objects.TryGetValue(objectLayer, out TileObject tile);
            if (tile == null)
                return false;

            return (tile.Id == type);
        }

        private void UpdateTileObjectMesh(TileObject tileObject, uint bitmask)
        {
            Adjacencies _adjacencies = tileObject.Adjacencies;
            if (_adjacencies == null)
                return;

            if (_adjacencies is AdvancedAdjacencies)
                GetAdvancedMesh(tileObject, bitmask, _adjacencies);
        }

        private void UpdateTileRotation(TileObject tileObject, uint bitmask)
        {
            Transform transform = tileObject.transform;
            switch (bitmask)
            {
                case 1:
                case 3:
                case 19:
                    transform.rotation = Quaternion.Euler(new Vector3(0f, 90f, 0f));
                    break;
                case 2:
                case 6:
                case 38:
                    transform.rotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
                    break;
                case 4:
                case 12:
                case 76:
                    transform.rotation = Quaternion.Euler(new Vector3(0f, 270f, 0f));
                    break;
            }
        }

        private void GetAdvancedMesh(TileObject tileObject, uint bitmask, Adjacencies _adjacency)
        {
            AdvancedAdjacencies meshes = (AdvancedAdjacencies)_adjacency;
            MeshFilter _filter = tileObject.GetComponent<MeshFilter>();

            if (meshes && _filter.mesh)
            {
                switch (bitmask)
                {
                    case 0:
                        _filter.mesh = meshes.O;
                        break;
                    case 1:
                    case 2:
                    case 4:
                    case 8:
                        _filter.mesh = meshes.U;
                        break;
                    case 5:
                    case 10:
                        _filter.mesh = meshes.I;
                        break;
                    case 3:
                    case 6:
                    case 9:
                    case 12:
                        _filter.mesh = meshes.L1;
                        break;
                    case 19:
                    case 38:
                    case 76:
                    case 137:
                        _filter.mesh = meshes.L2;
                        break;
                    case 7:
                    case 11:
                    case 13:
                    case 14:
                        _filter.mesh = meshes.T1;
                        break;
                    case 27:
                    case 39:
                    case 78:
                    case 141:
                        _filter.mesh = meshes.T2;
                        break;
                    case 23:
                    case 46:
                    case 77:
                    case 139:
                        _filter.mesh = meshes.T3;
                        break;
                    case 55:
                    case 110:
                    case 155:
                    case 205:
                        _filter.mesh = meshes.T4;
                        break;
                    default:
                        _filter.mesh = meshes.O;
                        break;
                }

                Debug.Log(_filter.mesh.name + " " + bitmask);
            }
        }
    }
}