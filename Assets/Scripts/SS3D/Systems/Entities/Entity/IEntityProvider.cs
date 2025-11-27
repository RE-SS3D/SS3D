using SS3D.Systems.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    public interface IEntityProvider
    {
        Entity Entity { get; }
    }
}
