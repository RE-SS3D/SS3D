using System;
using Coimbra.Services.Events;

namespace SS3D.Systems.Rounds.Events
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