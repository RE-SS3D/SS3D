using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror{
    public class TileNetworkManager : NetworkBehaviour {
        // A Tile Networking Packet is defined as follows!
        // Bitmask is from LSB to MSB
        //[0] byte TileType (cast to enum)
        //[1] byte lowerTurf status
        //[2] byte upperTurf status
        //[3] byte hasDisposal
        //[4] byte DisposalConfig
        //[5] byte hasBlue
        //[6] byte BlueConfig
        //[7] byte hasRed
        //[8] byte RedConfig
        public byte n_TileType;
        public byte n_lowerTurf;
        public byte n_upperTurf;
        public bool n_hasDisposal;
        public byte n_disposalConfig;
        public bool n_hasBlue;
        public byte n_blueConfig;
        public bool n_hasRed;
        public byte n_redConfig;

        private Tile myTile;
        
        private void Start() {
            myTile = gameObject.GetComponent<Tile>();
        }


        public void GetAllTileData(){
            myTile = gameObject.GetComponent<Tile>();
            //Debug.Log(" GETTING TILE DATA" );
            this.n_TileType = (byte) myTile.TileDescriptor;
            this.n_lowerTurf = (byte) myTile.turf.lowerState;
            this.n_upperTurf = (byte) myTile.turf.upperState;
            this.n_hasDisposal = myTile.pipeManager.hasDisposal;
            this.n_disposalConfig = (byte)myTile.pipeManager.disposalConfig;
            this.n_hasBlue = myTile.pipeManager.hasBluePipe;
            this.n_blueConfig = (byte)myTile.pipeManager.bluePipeConfig;
            this.n_hasRed = myTile.pipeManager.hasRedPipe;
            this.n_redConfig = (byte)myTile.pipeManager.redPipeConfig;
        }


        public void SetAllTileData(){
            myTile = gameObject.GetComponent<Tile>();
            //Debug.Log(" TILE DES" );
            UpdateTile();
            //Debug.Log(" TILE TURF " );
            UpdateTurf();
            //Debug.Log(" TILE PIPE " );
            UpdateUnderPipes(true);
            //Debug.Log("NETWORK UPDATE FINISHED");
        }

        public void SetTurf(int lowerState = -1, int upperState = -1){
            if(lowerState != -1){
                n_lowerTurf = (byte) lowerState;
            }
            if(upperState != -1){
                n_upperTurf = (byte) upperState;
            }
            // set dirtyBit Trigger 001<Tile>  010<Turf> 100<Pipe>
            SetDirtyBit(base.syncVarDirtyBits ^ 0b010);
        }

        public void UpdateTurf(){
            myTile.turf.lowerState = this.n_lowerTurf;
            myTile.turf.upperState = this.n_upperTurf;
            myTile.turf.UpdateTurf();
        }

        public void SetTile(Tile.TileTypes tileType){
            this.n_TileType = (byte) tileType;
            // set dirtyBit Trigger 001<Tile>  010<Turf> 100<Pipe>
            SetDirtyBit(base.syncVarDirtyBits ^ 0b001);
        }

        public void UpdateTile(){
            myTile.TileDescriptor = (Tile.TileTypes) this.n_TileType;
            myTile.UpdateTile();
        }

        public void SetUnderPipes(int hasDisposal = -1, int disposalConfig = -1,int hasBlue = -1, int blueConfig = -1,
                                    int hasRed = -1, int redConfig = -1, bool autoBuild = false)
        {
            //Debug.Log("SET UnderPipes");
            if (hasDisposal != -1){
                //Debug.Log("     hasDisposal: " + (hasDisposal > 0));
                this.n_hasDisposal = (hasDisposal > 0);
                this.n_disposalConfig = (byte) disposalConfig;
            }
            if (hasBlue != -1){
                //Debug.Log("     BlueDisposal: " + (hasBlue > 0));
                this.n_hasBlue = (hasBlue > 0);
                this.n_blueConfig = (byte) blueConfig;
                
            }
            if (hasRed != -1){
                //Debug.Log("     RedDisposal: " + (hasRed > 0));
                this.n_hasRed = (hasRed > 0);
                this.n_redConfig = (byte) redConfig;
                
            }
            // set dirtyBit Trigger 001<Tile>  010<Turf> 100<Pipe>
            SetDirtyBit(base.syncVarDirtyBits ^ 0b100);
        }

        public void UpdateUnderPipes(bool autoBuild = false){
            //Debug.Log("UpdateUnderPipes");
            if(n_hasDisposal){
                //Debug.Log("     Update disposal");
                myTile.pipeManager.BuildDisposal(this.n_disposalConfig, autoBuild);  //AUTOBUILD IS ON, DELETE LATER WHEN PROPER BUILDING IS REQUIRED
            }else{
                myTile.pipeManager.DeleteDisposal(this.n_disposalConfig, autoBuild);
            }
            if(n_hasBlue){
                //Debug.Log("     Update Blue");
                myTile.pipeManager.BuildBlue(this.n_blueConfig, autoBuild);  //AUTOBUILD IS ON, DELETE LATER WHEN PROPER BUILDING IS REQUIRED
            }else{
                myTile.pipeManager.DeleteBlue(this.n_disposalConfig, autoBuild);
            }
            if(n_hasRed){
                //Debug.Log("     Update Red");
                myTile.pipeManager.BuildRed(this.n_redConfig, autoBuild);  //AUTOBUILD IS ON, DELETE LATER WHEN PROPER BUILDING IS REQUIRED
            }else{
                myTile.pipeManager.DeleteRed(this.n_disposalConfig, autoBuild);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState){
            if (initialState){ 
                //This gets excecuted ONLY on server whenever a client connects for the first time to obtain the 'full' set of data
                GetAllTileData();
                //Debug.Log("SENDING INITIAL TILE-INFO: " + gameObject.name);
                writer.Write(this.n_TileType);
                writer.Write(this.n_lowerTurf);
                writer.Write(this.n_upperTurf);
                writer.Write(this.n_hasDisposal);
                writer.Write(this.n_disposalConfig);
                writer.Write(this.n_hasBlue);
                writer.Write(this.n_blueConfig);
                writer.Write(this.n_hasRed);
                writer.Write(this.n_redConfig);
            }else{
                //This gets excecuted ONLY on server whenever a dirtybit is set in the dirtybitmask in order to synchronize data.
                //TODO: rework to put the incremental change information in the bitmask, and send that informaiton to reduce packet size
                Debug.Log("SENDING DELTA TILE-INFO: " + gameObject.name + " :: " + base.syncVarDirtyBits.ToString("X"));
                //Send dirtyBit to the client as well!
                writer.Write(base.syncVarDirtyBits); //dirtyBit Trigger 001<Tile>  010<Turf> 100<Pipe>
                if((base.syncVarDirtyBits & 0b001 ) != 0u){
                    writer.Write(this.n_TileType);
                    if (isServer){
                        UpdateTile();
                    }
                }
                if((base.syncVarDirtyBits & 0b010 ) != 0u){
                    writer.Write(this.n_lowerTurf);
                    writer.Write(this.n_upperTurf);
                    if (isServer){
                        UpdateTurf();
                    }
                }
                if((base.syncVarDirtyBits & 0b100 ) != 0u){
                    writer.Write(this.n_hasDisposal);
                    writer.Write(this.n_disposalConfig);
                    writer.Write(this.n_hasBlue);
                    writer.Write(this.n_blueConfig);
                    writer.Write(this.n_hasRed);
                    writer.Write(this.n_redConfig);
                    if (isServer){
                        UpdateUnderPipes(true);
                    }
                }
            }
            return true;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState){
            if (initialState){
                 //This gets excecuted ONLY on CLIENT whenever a client connects for the first time to obtain the 'full' set of data
                //Debug.Log("RECEIVING INITIAL TILE-INFO: " + gameObject.name);
                this.n_TileType = reader.ReadByte();
                this.n_lowerTurf = reader.ReadByte();
                this.n_upperTurf = reader.ReadByte();
                this.n_hasDisposal = reader.ReadBoolean();
                this.n_disposalConfig = reader.ReadByte();
                this.n_hasBlue = reader.ReadBoolean();
                this.n_blueConfig = reader.ReadByte();
                this.n_hasRed = reader.ReadBoolean();
                this.n_redConfig = reader.ReadByte();
                SetAllTileData();
                //myTile.UpdateFromNetwork();
            }else{
                //This gets excecuted ONLY on client whenever updataPacket is invoked. the dirty bitmask itself is inaccesable
                // and should be send into the write/read buffer if its needed here to 'know' what is gonna change.
                // set dirtyBit Trigger 001<Tile>  010<Turf> 100<Pipe>
                ulong receivedDirtyBits = reader.ReadUInt64();
                Debug.Log("RECEIVING DELTA TILE-INFO: " + gameObject.name + " :: " + receivedDirtyBits.ToString("X"));
                if((receivedDirtyBits & 0b001 ) != 0u){
                    this.n_TileType = reader.ReadByte();
                    UpdateTile();
                }
                if((receivedDirtyBits & 0b010 ) != 0u){
                    this.n_lowerTurf = reader.ReadByte();
                    this.n_upperTurf = reader.ReadByte();
                    UpdateTurf();
                }
                if((receivedDirtyBits & 0b100 ) != 0u){
                    this.n_hasDisposal = reader.ReadBoolean();
                    this.n_disposalConfig = reader.ReadByte();
                    this.n_hasBlue = reader.ReadBoolean();
                    this.n_blueConfig = reader.ReadByte();
                    this.n_hasRed = reader.ReadBoolean();
                    this.n_redConfig = reader.ReadByte();
                    UpdateUnderPipes(true);
                }
            }   
        }
    }
}