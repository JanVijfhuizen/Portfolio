using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurveSearchLib
{
    [CreateAssetMenu(fileName = "Flow", menuName = "AI/Flow", order = 1)]
    public class Flow : ScriptableObject
    {
        public AnimationCurve curve;
        public int[] indexes;
        public int importance;
    }
}
