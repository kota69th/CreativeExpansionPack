using System.Net;
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

        public static Sprite MakeOutAnIcon(string Link, int Width, int Height)
        {
            var WebC = new WebClient();
            byte[] ImageAsByte = WebC.DownloadData(Link);
            Texture2D Texture = new Texture2D(Width, Height, UnityEngine.Experimental.Rendering.DefaultFormat.LDR, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            if (ImageConversion.LoadImage(Texture, ImageAsByte))
            {
                Texture.filterMode = FilterMode.Point;
                return Sprite.Create(Texture, new Rect(0.0f, 0.0f, Texture.width, Texture.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }
    }
}
