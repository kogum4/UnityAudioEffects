
using UnityEngine;

namespace Assets.Scripts.Misc
{
    public static class Misc
    {
        public static bool NearlyEqual(float a, float b, float res = 1e-10f) => Mathf.Abs(a - b) <= res;
    }
}