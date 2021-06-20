using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public class TestTileMap : MonoBehaviour
    {

        [SerializeField] private TileObjectSO tileObjectSO;
        [SerializeField] private TileObjectSO.Dir dir;

        TileManager tileManager;

        void Start()
        {
            tileManager = TileManager.Instance;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G) && tileObjectSO != null)
            {
                Vector3 mousePosition = GetMouseWorldPosition();
                tileManager.SetTileObject(TileLayerType.Plenum, tileObjectSO, mousePosition, dir);
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                tileManager.Save();
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                tileManager.Load();
            }
        }

        private Vector3 GetMouseWorldPosition()
        {
            Ray ray = CameraManager.singleton.examineCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f))
            {
                return ray.origin - (ray.origin.y / ray.direction.y) * ray.direction;
            }
            else
            {
                return Vector3.zero;
            }
        }
    }
}