using UnityEngine;

namespace Code.Shared
{
    public class Utils
    {
        public static uint EncodeColor(Color color)
        {
            byte r = (byte)(color.r * 255);
            byte g = (byte)(color.g * 255);
            byte b = (byte)(color.b * 255);
            byte a = (byte)(color.a * 255);

            return (uint)((r << 24) | (g << 16) | (b << 8) | a);
        }

        public static Color DecodeColor(uint packedColor)
        {
            float r = ((packedColor >> 24) & 0xFF) / 255f;
            float g = ((packedColor >> 16) & 0xFF) / 255f;
            float b = ((packedColor >> 8) & 0xFF) / 255f;
            float a = (packedColor & 0xFF) / 255f;

            return new Color(r, g, b, a);
        }

    }
}