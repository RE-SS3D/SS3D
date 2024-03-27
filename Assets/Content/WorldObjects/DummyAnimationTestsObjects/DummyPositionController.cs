using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyPositionController : MonoBehaviour
{
    private void Start()
    {
        Position = PositionType.Standing;
    }

    public PositionType Position { get; set; }
}
