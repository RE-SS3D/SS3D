using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpaceSlider : Slider
{

    [SerializeField]
    private float _sliderUpdateTime = 0.1f;

    private float _previousUpdateTime;

    private bool _dirtyUpdate;

    public Action<float> ValueTickUpdated;

    public bool Pressed => IsPressed();

    private void Start()
    {
        onValueChanged.AddListener(SetDirty);
    }

    private void SetDirty(float value)
    {
        _dirtyUpdate = true;
    }

    private void Update()
    {
        // using dirty flags is necessary to not send hundreds of syncvar update per seconds.
        if (_dirtyUpdate && Time.time - _previousUpdateTime > _sliderUpdateTime)
        {
            ValueTickUpdated?.Invoke(value);
            _dirtyUpdate = false;
            _previousUpdateTime = Time.time;
        }
    }

}