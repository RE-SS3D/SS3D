using System;
using Mirror;

namespace SS3D.Core.Rounds.Messages
{
   [Serializable]
   public struct RoundTickMessage : NetworkMessage
   {
      public readonly int Seconds;

      public RoundTickMessage(int seconds)
      {
         Seconds = seconds;
      } 
   }
}