using UnityEngine;

namespace TowerDefense.Utils
{
    /// <summary>
    /// 通用扩展方法集合。
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// 将Vector3的z轴归零（2D游戏常用）。
        /// </summary>
        public static Vector3 To2D(this Vector3 v)
        {
            return new Vector3(v.x, v.y, 0f);
        }

        /// <summary>
        /// 获取Vector3的2D距离（忽略z轴）。
        /// </summary>
        public static float Distance2D(this Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y));
        }

        /// <summary>
        /// 将float限制在指定范围内。
        /// </summary>
        public static float Clamp(this float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// 将int限制在指定范围内。
        /// </summary>
        public static int Clamp(this int value, int min, int max)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// 获取Transform的子物体（按名称），如果不存在则创建。
        /// </summary>
        public static Transform GetOrCreateChild(this Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                child = go.transform;
            }
            return child;
        }

        /// <summary>
        /// 获取或添加组件。
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        /// <summary>
        /// 打乱列表（Fisher-Yates洗牌算法）。
        /// </summary>
        public static void Shuffle<T>(this System.Collections.Generic.IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// 颜色转十六进制字符串。
        /// </summary>
        public static string ToHex(this Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        }
    }
}
