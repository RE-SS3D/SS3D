using System;
using FishNet.Broadcast;

namespace SS3D.Core.Rounds.Messages
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