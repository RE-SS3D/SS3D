using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilePipeManager : MonoBehaviour
{
    public bool hasDisposal = false;
    public byte hasDisposal_NESW = 0;
    public byte disposalConfig = 0;

    public GameObject Disposal = null;
 
    public void InitTilePipeManager(){}

    public void BuildDisposal(int config){
        //Update Bool
        hasDisposal = true;
        UpdateDisposal();

        if(config == -1){
            disposalConfig = hasDisposal_NESW;
        }else{
            disposalConfig = (byte) config;
        }
        UpdateDisposalModel();


        //Call Update of NESW
        Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        if (tileN != null){
            tileN.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(config);
        }
        if (tileE != null){
            tileE.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(config);
        }
        if (tileS != null){
            tileS.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(config);
        }
        if (tileW != null){
            tileW.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(config);
        }
    }

    public void DeleteDisposal(){
        //Update Bool
        hasDisposal = false;
        //Call Update of NESW
        Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        if (tileN != null){
            tileN.gameObject.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        if (tileE != null){
            tileE.gameObject.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        if (tileS != null){
            tileS.gameObject.GetComponent<TilePipeManager>().UpdateDisposal();
        }
        if (tileW != null){
            tileW.gameObject.GetComponent<TilePipeManager>().UpdateDisposal();
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
    
    public void UpdateDisposal(int config = 0){
        if(hasDisposal){
            //Update Bools
            Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
            Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
            Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
            Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
            hasDisposal_NESW = 0;
            if (tileN != null){
                if(tileN.gameObject.GetComponent<TilePipeManager>().hasDisposal){
                    hasDisposal_NESW ^= 8;
                }
            }
            if (tileE != null){
                if(tileE.gameObject.GetComponent<TilePipeManager>().hasDisposal){
                    hasDisposal_NESW ^= 4;
                }
            }
            if (tileS != null){
                if(tileS.gameObject.GetComponent<TilePipeManager>().hasDisposal){
                    hasDisposal_NESW ^= 2;
                }
            }
            if (tileW != null){
                if(tileW.gameObject.GetComponent<TilePipeManager>().hasDisposal){
                    hasDisposal_NESW ^= 1;
                }
            }
            if (config == -1){
                disposalConfig = hasDisposal_NESW;
                UpdateDisposalModel();
            }
        }
    }

    private void UpdateDisposalModel(){
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

        switch(disposalConfig){
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
