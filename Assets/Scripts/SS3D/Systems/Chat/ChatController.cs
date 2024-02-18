using SS3D.Core;
using SS3D.Core.Behaviours;

namespace SS3D.Engine.Chat
{
    public class ChatController : NetworkActor
    {
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                return;
            }

            foreach (ChatWindow chatWindow in ViewLocator.Get<ChatWindow>())
            {
                if (chatWindow.requiresChatController)
                {
                    chatWindow.Initialize();
                }
            }
        }
    }
}