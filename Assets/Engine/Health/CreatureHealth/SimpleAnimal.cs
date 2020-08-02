using Mirror;

namespace SS3D.Engine.Health
{
    /// <summary>
    ///  Attach to simple animals which do not feature a metabolic system.
    /// </summary>
    public class SimpleAnimal : CreatureHealth
    {
        // Hook so that new players can sync state on start
        [SyncVar(hook = nameof(SyncAliveState))] public bool deadState;

        public override void OnStartClient()
        {
            base.OnStartClient();
            SyncAliveState(deadState, deadState);
        }

        [Server]
        public void SetDeadState(bool isDead)
        {
            deadState = isDead;
        }

        [Server]
        protected override void OnDeathActions()
        {
            deadState = true;
        }

        private void SyncAliveState(bool oldState, bool state)
        {
            deadState = state;

            if (state)
            {
                // Animal is dead; set correct prefab

                // Allow passing through
            }
            else
            {
                // Animal is alive; set correct prefab
            }
        }
    }
}