using SS3D.Interactions;
using SS3D.Interactions.Interfaces;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Tracks the active delayed interaction on client and server for cancel / end notify.
    /// Combat melee connect scheduling stays on the combat network behaviour / controller.
    /// </summary>
    public sealed class DelayedInteractionTracker
    {
        private int _clientActiveReferenceId = -1;
        private IInteractionSource _clientActiveSource;
        private InteractionReference _serverActiveReference;
        private IInteractionSource _serverActiveSource;

        public bool HasClientActive => _clientActiveReferenceId >= 0;

        public int ClientActiveReferenceId => _clientActiveReferenceId;

        public void TrackServer(IInteractionSource source, InteractionReference reference, IInteraction interaction)
        {
            if (interaction is not IDelayedInteraction)
            {
                return;
            }

            _serverActiveReference = reference;
            _serverActiveSource = source;
        }

        public void SetClientActive(IInteractionSource source, int referenceId)
        {
            _clientActiveSource = source;
            _clientActiveReferenceId = referenceId;
        }

        public void ClearServer()
        {
            _serverActiveReference = null;
            _serverActiveSource = null;
        }

        public void ClearClient()
        {
            _clientActiveReferenceId = -1;
            _clientActiveSource = null;
        }

        public bool TryCancelServer(int referenceId)
        {
            if (_serverActiveReference == null || _serverActiveReference.Id != referenceId || _serverActiveSource == null)
            {
                return false;
            }

            if (_serverActiveSource.HasInteraction(_serverActiveReference))
            {
                _serverActiveSource.CancelInteraction(_serverActiveReference);
            }

            ClearServer();
            return true;
        }

        /// <summary>
        /// Returns true when the server delayed interaction ended this frame (caller should TargetNotify).
        /// </summary>
        public bool TryRefreshServerEnded()
        {
            if (_serverActiveReference == null || _serverActiveSource == null)
            {
                return false;
            }

            // A source destroyed mid-step (the last floor tile spent) is Unity-null but not C#-null through the
            // interface, and its interaction list still holds the reference: count it as ended, or the kneel sticks.
            bool sourceDestroyed = _serverActiveSource is UnityEngine.Object unityObject && unityObject == null;
            if (!sourceDestroyed && _serverActiveSource.HasInteraction(_serverActiveReference))
            {
                return false;
            }

            ClearServer();
            return true;
        }

        public void RefreshClient()
        {
            if (_clientActiveReferenceId < 0 || _clientActiveSource == null)
            {
                return;
            }

            var reference = new InteractionReference(_clientActiveReferenceId);
            if (_clientActiveSource.HasInteraction(reference))
            {
                return;
            }

            // Host: server refresh already TargetNotify'd this frame.
            // Pure client with no client interaction instance: wait for TargetNotify / Cancel.
        }
    }
}
