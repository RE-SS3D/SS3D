using System;
using Mirror;

namespace SS3D.Core.Rounds.Messages
{
   [Serializable]
   public struct OnTickMessage : NetworkMessage
   {
      public readonly int Seconds;

      public OnTickMessage(int seconds)
      {
         Seconds = seconds;
      } 
   }
}