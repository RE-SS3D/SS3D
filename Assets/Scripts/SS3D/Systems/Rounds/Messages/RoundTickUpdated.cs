using System;
using Coimbra.Services.Events;
using FishNet.Broadcast;

namespace SS3D.Systems.Rounds.Messages
{
   [Serializable]
   public partial struct RoundTickUpdated : IEvent
   {
      public readonly int Seconds;

      public RoundTickUpdated(int seconds)
      {
         Seconds = seconds;
      } 
   }
}