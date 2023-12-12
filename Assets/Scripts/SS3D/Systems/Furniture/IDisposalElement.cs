using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// A simple interface for all disposal furniture. It helps with abstracting over the type 
    /// of furnitures, when they all need to be treated the same. e.g : disposal pipes
    /// wants to connect with disposal elements, they don't care about their specificity.
    /// </summary>
    public interface IDisposalElement : IGameObjectProvider
    {

    }
}
