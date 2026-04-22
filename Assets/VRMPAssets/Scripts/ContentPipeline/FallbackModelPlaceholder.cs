using UnityEngine;

namespace XRMultiplayer.ContentPipeline
{
    public static class FallbackModelPlaceholder
    {
        public static Texture2D CreateFallbackThumbnail(int size = 64)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var colorA = new Color32(180, 180, 180, 255);
            var colorB = new Color32(110, 110, 110, 255);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var checker = ((x / 8) + (y / 8)) % 2 == 0;
                    texture.SetPixel(x, y, checker ? colorA : colorB);
                }
            }

            texture.Apply();
            return texture;
        }

        public static GameObject CreateRuntimePlaceholder(string name = "MissingModelPlaceholder")
        {
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = name;
            placeholder.transform.localScale = Vector3.one * 0.25f;
            return placeholder;
        }
    }
}
