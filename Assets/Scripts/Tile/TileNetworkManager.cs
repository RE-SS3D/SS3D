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
        //[SyncVar(hook = nameof(updateData) )]
        public byte n_upperTurf;

        public bool n_hasDisposal;
        public byte n_disposalConfig;
        public bool n_hasBlue;
        public byte n_blueConfig;
        public bool n_hasRed;
        public byte n_redConfig;
           

        public void GetTileData(){
            //Debug.Log(" GETTING TILE DATA" );
            Tile myTile = gameObject.GetComponent<Tile>();
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


        public void SetTileData(){
            //Debug.Log(" SETTING TILE DATA" );
            Tile myTile = gameObject.GetComponent<Tile>();
            myTile.TileDescriptor = (Tile.TileTypes) this.n_TileType;
            myTile.turf.lowerState  =  this.n_lowerTurf;
            myTile.turf.upperState  =  this.n_upperTurf;

            myTile.pipeManager.hasDisposal = this.n_hasDisposal;
            myTile.pipeManager.disposalConfig = this.n_disposalConfig;
            myTile.pipeManager.hasBluePipe = this.n_hasBlue;
            myTile.pipeManager.bluePipeConfig = this.n_blueConfig;
            myTile.pipeManager.hasRedPipe = this.n_hasRed;
            myTile.pipeManager.redPipeConfig = this.n_redConfig;
            //Debug.Log("NETWORK UPDATE FINISHED");
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState){
            if (initialState){ 
                //This gets excecuted ONLY on server whenever a client connects for the first time to obtain the 'full' set of data
                GetTileData();
                Debug.Log("SENDING INITIAL TILE-INFO: " + gameObject.name);
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
                Debug.Log("SENDING DELTA TILE-INFO: " + gameObject.name);
                writer.Write(this.n_TileType);
                writer.Write(this.n_lowerTurf);
                writer.Write(this.n_upperTurf);

                writer.Write(this.n_hasDisposal);
                writer.Write(this.n_disposalConfig);
                writer.Write(this.n_hasBlue);
                writer.Write(this.n_blueConfig);
                writer.Write(this.n_hasRed);
                writer.Write(this.n_redConfig);
            }
            return true;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState){
            if (initialState){
                 //This gets excecuted ONLY on CLIENT whenever a client connects for the first time to obtain the 'full' set of data
                Debug.Log("RECEIVING INITIAL TILE-INFO: " + gameObject.name);
                this.n_TileType = reader.ReadByte();
                this.n_lowerTurf = reader.ReadByte();
                this.n_upperTurf = reader.ReadByte();

                this.n_hasDisposal = reader.ReadBoolean();
                this.n_disposalConfig = reader.ReadByte();
                this.n_hasBlue = reader.ReadBoolean();
                this.n_blueConfig = reader.ReadByte();
                this.n_hasRed = reader.ReadBoolean();
                this.n_redConfig = reader.ReadByte();
            }else{
                //This gets excecuted ONLY on client whenever updataPacket is invoked. the dirty bitmask itself is inaccesable
                // and should be send into the write/read buffer if its needed here to 'know' what is gonna change.
                Debug.Log("RECEIVING DELTA TILE-INFO: " + gameObject.name);
                this.n_TileType = reader.ReadByte();
                this.n_lowerTurf = reader.ReadByte();
                this.n_upperTurf = reader.ReadByte();

                this.n_hasDisposal = reader.ReadBoolean();
                this.n_disposalConfig = reader.ReadByte();
                this.n_hasBlue = reader.ReadBoolean();
                this.n_blueConfig = reader.ReadByte();
                this.n_hasRed = reader.ReadBoolean();
                this.n_redConfig = reader.ReadByte();
            }
                SetTileData();
                gameObject.GetComponent<Tile>().UpdateFromNetwork();
        }

        // public override bool OnSerialize(NetworkWriter writer, bool initialState){
        //     // Function that gets called on server!
        //     if (initialState){
        //         GetTileData();
        //         Debug.Log(this.gameObject.name + " Serialized as initialState");
        //         writer.Write(this.n_TileType);
        //         writer.Write(this.n_lowerTurf);
        //         writer.Write(this.n_upperTurf);
        //         writer.Write(this.n_hasDisposal);
        //         writer.Write(this.n_disposalConfig);
        //         writer.Write(this.n_hasBlue);
        //         writer.Write(this.n_blueConfig);
        //         writer.Write(this.n_hasRed);
        //         writer.Write(this.n_redConfig);
        //     }else{
        //         Debug.Log(this.gameObject.name + " Serialized as Regular | writing value: " + this.n_TileType.ToString("X"));
        //         writer.Write(this.n_TileType);
        //         writer.Write(this.n_lowerTurf);
        //         writer.Write(this.n_upperTurf);
        //         writer.Write(this.n_hasDisposal);
        //         writer.Write(this.n_disposalConfig);
        //         writer.Write(this.n_hasBlue);
        //         writer.Write(this.n_blueConfig);
        //         writer.Write(this.n_hasRed);
        //         writer.Write(this.n_redConfig);
        //     }
        //     return true;
        // }
        // public override void OnDeserialize(NetworkReader reader, bool initialState){
        //     // Function that gets called on client!
        //     if (initialState){
        //         this.n_TileType = reader.ReadByte();
        //         this.n_TileType = reader.ReadByte();
                
        //         this.n_lowerTurf = reader.ReadByte();
        //         this.n_upperTurf = reader.ReadByte();

        //         this.n_hasDisposal = reader.ReadBoolean();
        //         this.n_disposalConfig = reader.ReadByte();
        //         this.n_hasBlue = reader.ReadBoolean();
        //         this.n_blueConfig = reader.ReadByte();
        //         this.n_hasRed = reader.ReadBoolean();
        //         this.n_redConfig = reader.ReadByte();
        //         Debug.Log(this.gameObject.name + " deserialized as initialState | reading value: " + this.n_TileType.ToString("X"));
        //     }else{
        //         this.n_TileType = reader.ReadByte();
        //         this.n_TileType = reader.ReadByte();
                
        //         this.n_lowerTurf = reader.ReadByte();
        //         this.n_upperTurf = reader.ReadByte();

        //         this.n_hasDisposal = reader.ReadBoolean();
        //         this.n_disposalConfig = reader.ReadByte();
        //         this.n_hasBlue = reader.ReadBoolean();
        //         this.n_blueConfig = reader.ReadByte();
        //         this.n_hasRed = reader.ReadBoolean();
        //         this.n_redConfig = reader.ReadByte();
        //         Debug.Log(this.gameObject.name + " deserialized as Regular | reading value: " + this.n_TileType.ToString("X"));
        //     }
        //     SetTileData();
        // }
    }
}