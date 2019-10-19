using System;
using Mirror;
using UnityEngine;

// Connection managers will handle calculating an object's interactions with neighboring objects that are defined as connectible, such as walls and tables.

public class ConnectionManager : Component
{
    GameObject parent;
    int[] connections;
}