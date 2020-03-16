using Mirror;

namespace SS3D.Engine.Tiles
{
    /**
     * Handles making requests on behalf of the player to the TileManager
     * TODO: This isn't particularly ideal. Need more discussion over alternatives, particularly
     *       networked interactions.
     *       Especially given the power this gives the client to call arbitrary code.
     */
    public class PlayerTileManagerClient : NetworkBehaviour
    {
        public void Awake()
        {
            tileManager = FindObjectOfType<TileManager>();
        }

        // Note: If getting rid of this class, this code should probably somehow be moved into
        // the NetworkManager's SpawnPlayer
        public override void OnStartLocalPlayer()
        {
            if(!isServer)
                CmdRequestServerSendTilesToClient();
        }

        public void CreateTile(TileObject tile, TileDefinition definition)
        {
            var pos = tileManager.GetIndexAt(tile.transform.position);
            CmdCreateTile(pos.x, pos.y, definition);
        }

        public void UpdateTile(TileObject tile, TileDefinition definition)
        {
            var pos = tileManager.GetIndexAt(tile.transform.position);
            CmdUpdateTile(pos.x, pos.y, definition);
        }

        public void DestroyTile(TileObject tile)
        {
            var pos = tileManager.GetIndexAt(tile.transform.position);
            CmdDestroyTile(pos.x, pos.y);
        }

        [Command]
        private void CmdCreateTile(int x, int y, TileDefinition definition) => tileManager.CreateTile(x, y, definition);

        [Command]
        private void CmdUpdateTile(int x, int y, TileDefinition definition) => tileManager.UpdateTile(x, y, definition);

        [Command]
        private void CmdDestroyTile(int x, int y) => tileManager.DestroyTile(x, y);

        // Purposefully stupid name to encourage me to update this.
        [Command]
        private void CmdRequestServerSendTilesToClient()
        {
            tileManager.SendTilesToClient(connectionToClient);
        }

        TileManager tileManager;
    }
}