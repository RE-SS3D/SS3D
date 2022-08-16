using System;
using FishNet.Broadcast;

namespace SS3D.Systems.Rounds.Messages
{
   [Serializable]
   public struct RoundTickUpdatedMessage : IBroadcast
   {
      public readonly int Seconds;

      public RoundTickUpdatedMessage(int seconds)
      {
         Seconds = seconds;
      } 
   }
}