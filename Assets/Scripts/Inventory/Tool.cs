using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A tool is something capable of interacting with something else.
 */
public interface Tool
{
    void Interact(RaycastHit hit, bool secondary = false);
}
