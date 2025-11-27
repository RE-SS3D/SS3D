using SS3D.Intents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Intents
{
    public interface IIntentProvider
    {
        IntentType Intent { get; }
    }
}
