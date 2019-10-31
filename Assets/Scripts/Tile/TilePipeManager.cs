using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilePipeManager : MonoBehaviour
{
    Dictionary<string, byte> directions = new Dictionary<string, byte>(){
        { "N", 8 },
        { "E", 4 },
        { "S", 2 },
        { "W", 1 },
        { "NE", 128},
        { "SE", 64 },
        { "SW", 32 },
        { "NW", 16 }
    };

    public bool hasDisposal = false;
    public byte hasDisposal_NESW = 0;
    public byte disposalConfig = 0;

    public bool hasBluePipe = false;
    public byte hasBluePipe_NESW = 0;
    public byte bluePipeConfig = 0;

    public bool hasRedPipe = false;
    public byte hasRedPipe_NESW = 0;
    public byte redPipeConfig = 0;

    public GameObject Disposal = null;
    public GameObject bluePipe = null;
    public GameObject redPipe = null;
 
    //BUILD PIPE => Update Pipe, Build Pipe, Update Neighbours
    //DELETE PIPE => Update Pipe, Delete Pipe, Update Neighbours
    //UPDATE PIPE => Update Pipe (NESW from neigbours) (if autobuild -> update Model too)

    public void InitTilePipeManager(){}

    public void UpdatePipes(bool autobuild = false){
        UpdateDisposal(autobuild);
        UpdateRed(autobuild);
        UpdateBlue(autobuild);
    }

    public void BuildDisposal(int config = 0, bool autobuild = false){
        //Update Bool
        hasDisposal = true;
        UpdateDisposal();

        if(autobuild){
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
            tileN.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(autobuild);
        }
        if (tileE != null){
            tileE.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(autobuild);
        }
        if (tileS != null){
            tileS.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(autobuild);
        }
        if (tileW != null){
            tileW.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(autobuild);
        }
    }

    public void DeleteDisposal(int config = 0, bool autobuild = false){
        //Update Bool
        hasDisposal = false;
        //Call Update of NESW
        Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        if (tileN != null){
            tileN.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(autobuild);
        }
        if (tileE != null){
            tileE.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(autobuild);
        }
        if (tileS != null){
            tileS.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(autobuild);
        }
        if (tileW != null){
            tileW.gameObject.GetComponent<TilePipeManager>().UpdateDisposal(autobuild);
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
    
    public void UpdateDisposal(bool autobuild = false){
        if(hasDisposal){
            //Update Bools
            Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
            Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
            Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
            Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
            hasDisposal_NESW = 0;
            if (tileN != null){
                if(tileN.gameObject.GetComponent<TilePipeManager>().hasDisposal){
                    hasDisposal_NESW ^= directions["N"];
                }
            }
            if (tileE != null){
                if(tileE.gameObject.GetComponent<TilePipeManager>().hasDisposal){
                    hasDisposal_NESW ^= directions["E"];
                }
            }
            if (tileS != null){
                if(tileS.gameObject.GetComponent<TilePipeManager>().hasDisposal){
                    hasDisposal_NESW ^= directions["S"];
                }
            }
            if (tileW != null){
                if(tileW.gameObject.GetComponent<TilePipeManager>().hasDisposal){
                    hasDisposal_NESW ^= directions["W"];
                }
            }
            if(autobuild){
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
        if(hasDisposal){
            switch(disposalConfig){
                    case(0):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_I"),transform.position, Quaternion.identity, transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(1):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_I"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(2):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_I"),transform.position, Quaternion.identity, transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(3):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_L"),transform.position, Quaternion.Euler(0,180,0), transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(4):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_I"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(5):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_I"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(6):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_L"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(7):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_T"),transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(8):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_I"),transform.position, Quaternion.identity, transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(9):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_L"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(10):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_I"),transform.position, Quaternion.identity, transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(11):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_T"),transform.position, Quaternion.Euler(0,180,0), transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(12):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_L"),transform.position, Quaternion.identity, transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(13):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_T"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(14):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_T"),transform.position, Quaternion.identity, transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
                    case(15):
                        Disposal = Instantiate(Resources.Load("Pipes/Under/Disposal/Disposal_X"),transform.position, Quaternion.identity, transform) as GameObject;
                        Disposal.name = "Disposal_pipe";
                        break;
            }
        }
    }

    public void BuildBlue(int config, bool autobuild = false){
        //Update Bool
        hasBluePipe = true;
        UpdateBlue();

        if(autobuild){
            bluePipeConfig = hasBluePipe_NESW;
        }else{
            bluePipeConfig = (byte) config;
        }
        UpdateBlueModel();


        //Call Update of NESW
        Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        if (tileN != null){
            tileN.gameObject.GetComponent<TilePipeManager>().UpdateBlue(autobuild);
        }
        if (tileE != null){
            tileE.gameObject.GetComponent<TilePipeManager>().UpdateBlue(autobuild);
        }
        if (tileS != null){
            tileS.gameObject.GetComponent<TilePipeManager>().UpdateBlue(autobuild);
        }
        if (tileW != null){
            tileW.gameObject.GetComponent<TilePipeManager>().UpdateBlue(autobuild);
        }
    }
    
    public void DeleteBlue(int config = 0, bool autobuild = false){
        //Update Bool
        hasBluePipe = false;
        //Call Update of NESW
        Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        if (tileN != null){
            tileN.gameObject.GetComponent<TilePipeManager>().UpdateBlue(autobuild);
        }
        if (tileE != null){
            tileE.gameObject.GetComponent<TilePipeManager>().UpdateBlue(autobuild);
        }
        if (tileS != null){
            tileS.gameObject.GetComponent<TilePipeManager>().UpdateBlue(autobuild);
        }
        if (tileW != null){
            tileW.gameObject.GetComponent<TilePipeManager>().UpdateBlue(autobuild);
        }
        //Delete Model
        if (bluePipe != null){
            #if UNITY_EDITOR
            DestroyImmediate(bluePipe);
            #else
            Destroy(bluePipe);
            #endif
        }
    }
    
    public void UpdateBlue(bool autobuild = false){
        if(hasBluePipe){
            //Update Bools
            Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
            Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
            Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
            Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
            hasBluePipe_NESW = 0;
            if (tileN != null){
                if(tileN.gameObject.GetComponent<TilePipeManager>().hasBluePipe){
                    hasBluePipe_NESW ^= directions["N"];
                }
            }
            if (tileE != null){
                if(tileE.gameObject.GetComponent<TilePipeManager>().hasBluePipe){
                    hasBluePipe_NESW ^= directions["E"];
                }
            }
            if (tileS != null){
                if(tileS.gameObject.GetComponent<TilePipeManager>().hasBluePipe){
                    hasBluePipe_NESW ^= directions["S"];
                }
            }
            if (tileW != null){
                if(tileW.gameObject.GetComponent<TilePipeManager>().hasBluePipe){
                    hasBluePipe_NESW ^= directions["W"];
                }
            }
            if(autobuild){
                bluePipeConfig = hasBluePipe_NESW;
                UpdateBlueModel();
            }
        }
    }

    public void UpdateBlueModel(){
        if (bluePipe != null){
            #if UNITY_EDITOR
            DestroyImmediate(bluePipe);
            #else
            Destroy(bluePipe);
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
        if(hasBluePipe){
            switch(bluePipeConfig){
                    case(0)://none
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_I"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(1)://W
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_I"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(2)://S
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_I"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(3)://SW
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_SW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(4)://E
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_I"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(5)://EW
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_I"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(6)://SE
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_SE"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(7)://SEW
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_SEW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(8)://N
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_I"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(9)://NW
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_NW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(10)://NS
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_I"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(11)://NSW
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_NSW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(12)://NE
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_NE"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(13)://NEW
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_NEW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(14)://NES
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_NES"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
                    case(15)://NESW
                        bluePipe = Instantiate(Resources.Load("Pipes/Under/Blue/Blue_X"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        bluePipe.name = "Blue_pipe";
                        break;
            }
        }
    }

   public void BuildRed(int config, bool autobuild = false){
        //Update Bool
        hasRedPipe = true;
        UpdateRed();

        if(autobuild){
            redPipeConfig = hasRedPipe_NESW;
        }else{
            redPipeConfig = (byte) config;
        }
        UpdateRedModel();


        //Call Update of NESW
        Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        if (tileN != null){
            tileN.gameObject.GetComponent<TilePipeManager>().UpdateRed(autobuild);
        }
        if (tileE != null){
            tileE.gameObject.GetComponent<TilePipeManager>().UpdateRed(autobuild);
        }
        if (tileS != null){
            tileS.gameObject.GetComponent<TilePipeManager>().UpdateRed(autobuild);
        }
        if (tileW != null){
            tileW.gameObject.GetComponent<TilePipeManager>().UpdateRed(autobuild);
        }
    }
    
    public void DeleteRed(int config = 0, bool autobuild = false){
        //Update Bool
        hasRedPipe = false;
        //Call Update of NESW
        Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        if (tileN != null){
            tileN.gameObject.GetComponent<TilePipeManager>().UpdateRed(autobuild);
        }
        if (tileE != null){
            tileE.gameObject.GetComponent<TilePipeManager>().UpdateRed(autobuild);
        }
        if (tileS != null){
            tileS.gameObject.GetComponent<TilePipeManager>().UpdateRed(autobuild);
        }
        if (tileW != null){
            tileW.gameObject.GetComponent<TilePipeManager>().UpdateRed(autobuild);
        }
        //Delete Model
        if (redPipe != null){
            #if UNITY_EDITOR
            DestroyImmediate(redPipe);
            #else
            Destroy(redPipe);
            #endif
        }
    }
    
    public void UpdateRed(bool autobuild = false){
        if(hasRedPipe){
            //Update Bools
            Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
            Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
            Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
            Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
            hasRedPipe_NESW = 0;
            if (tileN != null){
                if(tileN.gameObject.GetComponent<TilePipeManager>().hasRedPipe){
                    hasRedPipe_NESW ^= directions["N"];
                }
            }
            if (tileE != null){
                if(tileE.gameObject.GetComponent<TilePipeManager>().hasRedPipe){
                    hasRedPipe_NESW ^= directions["E"];
                }
            }
            if (tileS != null){
                if(tileS.gameObject.GetComponent<TilePipeManager>().hasRedPipe){
                    hasRedPipe_NESW ^= directions["S"];
                }
            }
            if (tileW != null){
                if(tileW.gameObject.GetComponent<TilePipeManager>().hasRedPipe){
                    hasRedPipe_NESW ^= directions["W"];
                }
            }
            if(autobuild){
                redPipeConfig = hasRedPipe_NESW;
                UpdateRedModel();
            }
        }
    }

    public void UpdateRedModel(){
        if (redPipe != null){
            #if UNITY_EDITOR
            DestroyImmediate(redPipe);
            #else
            Destroy(redPipe);
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
        if(hasRedPipe){
            switch(redPipeConfig){
                    case(0)://none
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_I"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(1)://W
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_I"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(2)://S
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_I"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(3)://SW
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_SW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(4)://E
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_I"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(5)://EW
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_I"),transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(6)://SE
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_SE"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(7)://SEW
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_SEW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(8)://N
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_I"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(9)://NW
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_NW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(10)://NS
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_I"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(11)://NSW
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_NSW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(12)://NE
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_NE"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(13)://NEW
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_NEW"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(14)://NES
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_NES"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    case(15)://NESW
                        redPipe = Instantiate(Resources.Load("Pipes/Under/Red/Red_X"),transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                        redPipe.name = "Red_pipe";
                        break;
                    
            }
        }
    }

}
