using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilePipeManager : MonoBehaviour
{
    public bool hasDisposal = false;
    public byte hasDisposal_NESW = 0;

    public GameObject Disposal = null;
 
    public void InitTilePipeManager(){}

    public void BuildDisposal(){
        //Update Bool
        hasDisposal = true;
        UpdateDisposal();
        //Call Update of NESW
        GameObject tileN = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        GameObject tileE = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        GameObject tileS = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        GameObject tileW = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        if (tileN != null){
            tileN.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        if (tileE != null){
            tileE.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        if (tileS != null){
            tileS.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        if (tileW != null){
            tileW.GetComponent<TilePipeManager>().UpdateDisposal();
        }
    }

    public void DeleteDisposal(){
        //Update Bool
        hasDisposal = false;
        //Call Update of NESW
        GameObject tileN = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        GameObject tileE = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        GameObject tileS = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        GameObject tileW = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        if (tileN != null){
            tileN.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        if (tileE != null){
            tileE.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        if (tileS != null){
            tileS.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        if (tileW != null){
            tileW.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        //Delete Model
        if (Disposal != null){
            #if UNITY_EDITOR
            DestroyImmediate(Disposal);
            #else
            Destroy(Disposal);
            #endif
        }
    }
    
    public void UpdateDisposal(){
        //Update Bools
        GameObject tileN = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        GameObject tileE = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        GameObject tileS = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        GameObject tileW = GameObject.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        hasDisposal_NESW = 0;
        if (tileN != null){
            if(tileN.GetComponent<TilePipeManager>().hasDisposal){
                hasDisposal_NESW ^= 8;
            }
        }
        if (tileE != null){
            if(tileE.GetComponent<TilePipeManager>().hasDisposal){
                hasDisposal_NESW ^= 4;
            }
        }
        if (tileS != null){
            if(tileS.GetComponent<TilePipeManager>().hasDisposal){
                hasDisposal_NESW ^= 2;
            }
        }
        if (tileW != null){
            if(tileW.GetComponent<TilePipeManager>().hasDisposal){
                hasDisposal_NESW ^= 1;
            }
        }
        UpdateDisposalModel();
    }

    private void UpdateDisposalModel(){
        //Delete Model
        if (Disposal != null){
            #if UNITY_EDITOR
            DestroyImmediate(Disposal);
            #else
            Destroy(Disposal);
            #endif
        }

        //Create Model
        // N = Z+
        // E = X+
        // N E S W
        // 0 0 0 0
        // 8 4 2 1

        // 1 = W
        // 2 = S
        // 3 = SW
        // 4 = E
        // 5 = EW
        // 6 = ES
        // 7 = ESW
        // 8 = N
        // 9 = NW
        // 10 = NS
        // 11 = NSW
        // 12 = NE
        // 13 = NEW
        // 14 = NES
        // 15 = NESW

        if( hasDisposal == true){
            switch(hasDisposal_NESW){
                case(0):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_I"),transform.position, Quaternion.identity, transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(1):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_I"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(2):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_I"),transform.position, Quaternion.identity, transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(3):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_L"),transform.position, Quaternion.Euler(0,180,0), transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(4):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_I"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(5):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_I"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(6):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_L"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(7):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_T"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(8):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_I"),transform.position, Quaternion.identity, transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(9):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_L"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(10):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_I"),transform.position, Quaternion.identity, transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(11):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_T"),transform.position, Quaternion.Euler(0,180,0), transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(12):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_L"),transform.position, Quaternion.identity, transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(13):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_T"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(14):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_T"),transform.position, Quaternion.identity, transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
                case(15):
                    Disposal = Instantiate(Resources.Load("Pipes/Disposal_X"),transform.position, Quaternion.identity, transform) as GameObject;
                    Disposal.name = "Disposal_pipe";
                    break;
            }
        }

    }


}
