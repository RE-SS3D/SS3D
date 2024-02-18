using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Linq;

namespace SS3D.Engine.Chat
{
    public sealed class InGameChatController : NetworkActor
    {
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                return;
            }

            ViewLocator.Get<LobbyChatWindow>().First().Deinitialize();
            ViewLocator.Get<InGameChatWindow>().First().Initialize();
        }
    }
}