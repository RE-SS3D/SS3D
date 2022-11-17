using Coimbra.Services.Events;
using FishNet.Broadcast;
using FishNet.Connection;

namespace SS3D.Systems.Entities.Messages
{
    public struct RequestJoinAsAdmin : IBroadcast
    {
        public Soul Author;

        public RequestJoinAsAdmin(Soul author)
        {
            Author = author;
        }              
    }
}