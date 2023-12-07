using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small interface for containers that can only store things and/or remove them under special conditions.
/// Think about the jumpsuit that can't be removed when a belt is already worn.
/// </summary>
public interface IStorageCondition
{
    public bool CanStore(AttachedContainer container, Item item);

    public bool CanRemove(AttachedContainer container, Item item);
}
