using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// 程序化子弹视觉工厂。为每种塔生成独特的子弹精灵，
    /// 所有精灵水平朝右（箭头朝右），由 Projectile 组件旋转朝向目标。
    /// </summary>
    public static class ProjectileVisualFactory
    {
        private static Sprite _arrow, _cannon, _frost, _laser, _poison;

        public static Sprite GetProjectileSprite(TowerType type)
        {
            switch (type)
            {
                case TowerType.Archer: return _arrow ??= GenerateArrow();
                case TowerType.Cannon: return _cannon ??= GenerateCannonBall();
                case TowerType.Frost: return _frost ??= GenerateFrostShard();
                case TowerType.Laser: return _laser ??= GenerateLaserOrb();
                case TowerType.Poison: return _poison ??= GeneratePoisonOrb();
                case TowerType.Support: return _cannon ??= GenerateCannonBall();
            }
            return _arrow ??= GenerateArrow();
        }

        /// <summary>箭矢：箭头+箭身+羽毛，64x32，朝右</summary>
        private static Sprite GenerateArrow()
        {
            int w = 64, h = 32;
            var tex = NewTex(w, h);

            // 箭身（水平矩形）
            FillRect(tex, 8, 14, 40, 4, new Color(0.6f, 0.45f, 0.2f));
            FillRect(tex, 8, 15, 40, 2, new Color(0.75f, 0.6f, 0.35f));

            // 箭头（三角形，朝右）
            for (int y = 6; y <= 26; y++)
            {
                int distFromCenter = Mathf.Abs(y - 16);
                int arrowWidth = Mathf.RoundToInt(10 * (1f - distFromCenter / 11f));
                for (int x = 48; x < 48 + arrowWidth; x++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                        tex.SetPixel(x, y, new Color(0.85f, 0.85f, 0.9f));
                }
            }
            // 箭头高光
            FillRect(tex, 50, 14, 6, 2, new Color(1f, 1f, 1f));

            // 羽毛（左侧V形）
            for (int y = 8; y <= 24; y++)
            {
                int distFromCenter = Mathf.Abs(y - 16);
                int featherLen = Mathf.RoundToInt(8 * (1f - distFromCenter / 9f));
                for (int x = 4; x < 4 + featherLen; x++)
                {
                    if (x >= 0 && x < w && y >= 0 && y < h)
                        tex.SetPixel(x, y, new Color(0.9f, 0.3f, 0.3f));
                }
            }

            return Finalize(tex, "Arrow", w, h, new Vector2(0.3f, 0.5f));
        }

        /// <summary>炮弹：金属圆球+高光，32x32</summary>
        private static Sprite GenerateCannonBall()
        {
            int s = 32;
            var tex = NewTex(s, s);
            FillCircle(tex, 16, 16, 12, new Color(0.3f, 0.3f, 0.35f));
            FillCircle(tex, 16, 16, 9, new Color(0.45f, 0.45f, 0.5f));
            // 高光
            FillCircle(tex, 12, 12, 4, new Color(0.7f, 0.7f, 0.75f));
            FillCircle(tex, 11, 11, 2, new Color(0.9f, 0.9f, 0.95f));
            // 底部阴影
            FillCircle(tex, 19, 20, 5, new Color(0.15f, 0.15f, 0.18f, 0.5f));
            return Finalize(tex, "CannonBall", s, s, new Vector2(0.5f, 0.5f));
        }

        /// <summary>冰晶：菱形+高光+尖角，32x32</summary>
        private static Sprite GenerateFrostShard()
        {
            int s = 32;
            var tex = NewTex(s, s);
            // 大菱形
            FillDiamond(tex, 16, 16, 12, 14, new Color(0.4f, 0.75f, 1f));
            FillDiamond(tex, 16, 16, 8, 10, new Color(0.6f, 0.88f, 1f));
            // 高光
            FillDiamond(tex, 13, 12, 4, 6, new Color(0.85f, 0.97f, 1f));
            // 尖角
            FillRect(tex, 15, 1, 2, 6, new Color(0.5f, 0.8f, 1f));
            FillRect(tex, 15, 25, 2, 6, new Color(0.5f, 0.8f, 1f));
            return Finalize(tex, "FrostShard", s, s, new Vector2(0.5f, 0.5f));
        }

        /// <summary>激光能量球：发光圆+光晕+核心，32x32</summary>
        private static Sprite GenerateLaserOrb()
        {
            int s = 32;
            var tex = NewTex(s, s);
            // 外光晕
            FillCircle(tex, 16, 16, 14, new Color(0.85f, 0.4f, 1f, 0.3f));
            FillCircle(tex, 16, 16, 11, new Color(0.9f, 0.5f, 1f, 0.5f));
            // 主体
            FillCircle(tex, 16, 16, 8, new Color(0.8f, 0.3f, 0.95f));
            FillCircle(tex, 16, 16, 5, new Color(0.95f, 0.6f, 1f));
            // 核心
            FillCircle(tex, 16, 16, 2, new Color(1f, 0.9f, 1f));
            return Finalize(tex, "LaserOrb", s, s, new Vector2(0.5f, 0.5f));
        }

        /// <summary>毒液球：绿色圆+气泡+高光，32x32</summary>
        private static Sprite GeneratePoisonOrb()
        {
            int s = 32;
            var tex = NewTex(s, s);
            // 主体
            FillCircle(tex, 16, 16, 12, new Color(0.25f, 0.6f, 0.15f));
            FillCircle(tex, 16, 16, 9, new Color(0.4f, 0.8f, 0.25f));
            // 高光
            FillCircle(tex, 12, 12, 3, new Color(0.7f, 1f, 0.6f));
            // 气泡
            FillCircle(tex, 20, 20, 2, new Color(0.6f, 0.95f, 0.4f, 0.8f));
            FillCircle(tex, 22, 14, 1, new Color(0.7f, 1f, 0.5f, 0.7f));
            return Finalize(tex, "PoisonOrb", s, s, new Vector2(0.5f, 0.5f));
        }

        // ============================================================
        //  绘制辅助
        // ============================================================

        private static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(new Color[w * h]);
            return tex;
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            for (int py = y; py < y + h; py++)
                for (int px = x; px < x + w; px++)
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        tex.SetPixel(px, py, c);
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d <= r)
                    {
                        float edge = r - d;
                        Color col = c;
                        if (edge < 1.5f) col.a *= Mathf.Clamp01(edge / 1.5f);
                        tex.SetPixel(x, y, col);
                    }
                }
        }

        private static void FillDiamond(Texture2D tex, int cx, int cy, int halfW, int halfH, Color c)
        {
            for (int y = cy - halfH; y <= cy + halfH; y++)
                for (int x = cx - halfW; x <= cx + halfW; x++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                    float dx = Mathf.Abs(x - cx) / (float)halfW;
                    float dy = Mathf.Abs(y - cy) / (float)halfH;
                    if (dx + dy <= 1f)
                    {
                        float edge = 1f - (dx + dy);
                        Color col = c;
                        if (edge < 0.1f) col.a *= Mathf.Clamp01(edge * 10f);
                        tex.SetPixel(x, y, col);
                    }
                }
        }

        private static Sprite Finalize(Texture2D tex, string name, int w, int h, Vector2 pivot)
        {
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 100f);
            sprite.name = name;
            return sprite;
        }
    }
}
