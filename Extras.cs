using UnityEngine;

namespace FraggleExpansion
{
    public struct DataRisingSlime
    {
        public static float SlimeHeightPercentage = 100;
        public static float SlimeSpeedPercentage = 100;
    }

    public struct MiscData
    {
        public static GameObject CurrentPositionDisplay = null;
    }

    internal static class Tools
    {
        public static GameObject FindChild(this GameObject Parent, string Name)
        {
            foreach (Transform Transform in Parent.GetComponentsInChildren<Transform>(true))
                if (Transform.name == Name) return Transform.gameObject;
            return null;
        }
    }
}
