using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Systems.Examine
{
    public class SimpleExaminable : Actor, IExaminable
    {
        [SerializeField] private ExamineData key;

        public ExamineData GetData()
        {
            return key;
        }
    }
}