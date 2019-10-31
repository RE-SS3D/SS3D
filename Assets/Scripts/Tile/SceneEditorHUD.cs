using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneEditorHUD : MonoBehaviour
{
    private bool buildMode;
    private int y_select = 0;

    [SerializeField]
    private Camera camera;
    [SerializeField]
    private GameObject tileManager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        buildMode = gameObject.transform.Find("Toggle").GetComponent<UnityEngine.UI.Toggle>().isOn;
        if(buildMode && Input.GetMouseButtonDown(0)){
            Vector3 selectedCell = GetCell();
            print(selectedCell);
            GameObject selectedTile = tileManager.transform.Find(string.Format("tile_{0}_{1}",selectedCell.x,selectedCell.z)).gameObject;
            if(selectedTile.GetComponent<Mirror.TileNetworkManager>().n_lowerTurf == 2){
                selectedTile.GetComponent<Mirror.TileNetworkManager>().n_lowerTurf = 1; //set network component;
                selectedTile.GetComponent<Mirror.TileNetworkManager>().n_upperTurf = 0;
            }else{
                selectedTile.GetComponent<Mirror.TileNetworkManager>().n_lowerTurf = 2; //set network component;
                selectedTile.GetComponent<Mirror.TileNetworkManager>().n_upperTurf = 2;
            }
            selectedTile.GetComponent<Mirror.TileNetworkManager>().SetAllTileData(); //now LOCK IN that data;
            // set dirtyBit to trigger clients to update this TileData
            selectedTile.GetComponent<Mirror.TileNetworkManager>().SetDirtyBit(0xFF);

            //now update server graphics manually, as it doesnt get the trigger through the updatePacket
        }
    }

    private Vector3 GetCell(){
        // Get the mouse position at y = 0
        Ray mouse_ray = camera.ScreenPointToRay(Input.mousePosition);
        Vector3 mousePos = mouse_ray.origin - mouse_ray.direction * (mouse_ray.origin.y / mouse_ray.direction.y);

        // Get the corresponding cell on our virtual grid
        Vector3 cell = new Vector3(Mathf.RoundToInt(mousePos.x), y_select ,Mathf.RoundToInt(mousePos.z));
        //Debug.Log(mousePos);
        //Debug.Log(cell);
        return cell;
    }
}
