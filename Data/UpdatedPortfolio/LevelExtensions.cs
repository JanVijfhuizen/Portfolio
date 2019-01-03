using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Generating;

namespace GridExtensions
{
    public static class LevelExtensions
    {
        public static Vector2Int ConvertToGridPosition(this Vector3 pos)
        {
            return new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));
        }

        public static Vector3 ConvertToWorldPosition(this Vector2Int pos, float y)
        {
            return new Vector3(pos.x, y, pos.y);
        }

        public static void GetCircle(this Level level, List<Node> circle, Vector2Int from, int range)
        {
            int threshold = range * range;

            for (int i = -range; i <= range; i++)
                for (int j = -range; j <= range; j++)
                    if (!level.IsOutOfBounds(i + from.x, j + from.y))
                        if (i * i + j * j < threshold)
                            circle.Add(level.nodes[i + from.x, j + from.y]);
        }

        public static void GetSquire(this Level level, List<Node> squire, Vector2Int from, Vector2Int shape)
        {
            for (int x = 0; x < shape.x; x++)
                for (int y = 0; y < shape.y; y++)
                    squire.Add(level.nodes[x + from.x, y + from.y]);
        }

        public static bool IsOutOfBounds(this Level level, int x, int y)
        {
            return x < 0 || y < 0 || x >= level.Size || y >= level.Size;
        }
    }
}