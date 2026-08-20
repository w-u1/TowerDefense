using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// 程序化塔外观工厂。用代码生成128x128的精致塔精灵，
    /// 每种塔有独特的基座颜色和炮塔造型，彻底替代外部素材。
    /// </summary>
    public static class TowerVisualFactory
    {
        private static Sprite _archer;
        private static Sprite _cannon;
        private static Sprite _frost;
        private static Sprite _laser;
        private static Sprite _poison;
        private static Sprite _support;

        private const int TexSize = 128;
        private const float PPU = 100f;

        public static Sprite GetTowerSprite(TowerType type)
        {
            switch (type)
            {
                case TowerType.Archer: return _archer ??= GenerateArcher();
                case TowerType.Cannon: return _cannon ??= GenerateCannon();
                case TowerType.Frost: return _frost ??= GenerateFrost();
                case TowerType.Laser: return _laser ??= GenerateLaser();
                case TowerType.Poison: return _poison ??= GeneratePoison();
                case TowerType.Support: return _support ??= GenerateSupport();
            }
            return _archer;
        }

        // ============================================================
        //  六种塔的生成
        // ============================================================

        /// <summary>箭塔：蓝色基座 + 十字弩造型</summary>
        private static Sprite GenerateArcher()
        {
            var tex = NewTex();
            // 基座
            DrawBase(tex, new Color(0.25f, 0.5f, 0.9f), new Color(0.12f, 0.28f, 0.6f));
            // 十字弩：水平弓臂
            FillRect(tex, 32, 66, 64, 10, new Color(0.55f, 0.35f, 0.15f));
            FillRect(tex, 36, 62, 56, 6, new Color(0.7f, 0.5f, 0.25f));
            // 竖直箭身
            FillRect(tex, 61, 50, 6, 28, new Color(0.85f, 0.85f, 0.7f));
            // 箭头
            FillTriangle(tex, 64, 48, 12, 10, new Color(0.9f, 0.9f, 0.95f));
            // 弓弦
            DrawLine(tex, 38, 66, 90, 66, new Color(0.9f, 0.9f, 0.9f), 1);
            return Finalize(tex, "ArcherTower");
        }

        /// <summary>炮塔：棕色基座 + 粗大炮管</summary>
        private static Sprite GenerateCannon()
        {
            var tex = NewTex();
            DrawBase(tex, new Color(0.6f, 0.38f, 0.18f), new Color(0.35f, 0.2f, 0.08f));
            // 炮管底座
            FillCircle(tex, 64, 60, 22, new Color(0.3f, 0.3f, 0.32f));
            FillCircle(tex, 64, 60, 16, new Color(0.45f, 0.45f, 0.48f));
            // 粗大炮管
            FillRect(tex, 54, 28, 20, 38, new Color(0.25f, 0.25f, 0.28f));
            FillRect(tex, 57, 28, 14, 38, new Color(0.4f, 0.4f, 0.43f));
            // 炮口
            FillCircle(tex, 64, 30, 11, new Color(0.15f, 0.15f, 0.18f));
            FillCircle(tex, 64, 30, 6, new Color(0.08f, 0.08f, 0.1f));
            return Finalize(tex, "CannonTower");
        }

        /// <summary>冰塔：浅蓝基座 + 蓝色水晶</summary>
        private static Sprite GenerateFrost()
        {
            var tex = NewTex();
            DrawBase(tex, new Color(0.3f, 0.65f, 0.85f), new Color(0.15f, 0.4f, 0.65f));
            // 水晶底座
            FillCircle(tex, 64, 62, 18, new Color(0.5f, 0.8f, 0.95f));
            // 大水晶（菱形）
            FillDiamond(tex, 64, 44, 28, 40, new Color(0.4f, 0.75f, 1f));
            FillDiamond(tex, 64, 44, 18, 28, new Color(0.6f, 0.88f, 1f));
            // 水晶高光
            FillDiamond(tex, 58, 38, 8, 14, new Color(0.85f, 0.97f, 1f));
            // 小冰晶
            FillDiamond(tex, 44, 58, 8, 12, new Color(0.55f, 0.82f, 1f));
            FillDiamond(tex, 84, 60, 7, 10, new Color(0.55f, 0.82f, 1f));
            return Finalize(tex, "FrostTower");
        }

        /// <summary>激光塔：紫色基座 + 双管能量炮</summary>
        private static Sprite GenerateLaser()
        {
            var tex = NewTex();
            DrawBase(tex, new Color(0.65f, 0.25f, 0.75f), new Color(0.4f, 0.12f, 0.5f));
            // 能量核心
            FillCircle(tex, 64, 62, 20, new Color(0.4f, 0.15f, 0.5f));
            FillCircle(tex, 64, 62, 13, new Color(0.85f, 0.4f, 1f));
            FillCircle(tex, 64, 62, 7, new Color(1f, 0.85f, 1f));
            // 双管
            FillRect(tex, 46, 30, 12, 34, new Color(0.3f, 0.3f, 0.35f));
            FillRect(tex, 70, 30, 12, 34, new Color(0.3f, 0.3f, 0.35f));
            FillRect(tex, 49, 30, 6, 34, new Color(0.5f, 0.5f, 0.55f));
            FillRect(tex, 73, 30, 6, 34, new Color(0.5f, 0.5f, 0.55f));
            // 管口能量
            FillCircle(tex, 52, 32, 5, new Color(0.9f, 0.5f, 1f));
            FillCircle(tex, 76, 32, 5, new Color(0.9f, 0.5f, 1f));
            return Finalize(tex, "LaserTower");
        }

        /// <summary>毒塔：绿色基座 + 毒气瓶</summary>
        private static Sprite GeneratePoison()
        {
            var tex = NewTex();
            DrawBase(tex, new Color(0.3f, 0.65f, 0.25f), new Color(0.15f, 0.4f, 0.1f));
            // 瓶身
            FillCircle(tex, 64, 58, 22, new Color(0.2f, 0.5f, 0.15f));
            FillCircle(tex, 64, 58, 16, new Color(0.4f, 0.8f, 0.3f));
            // 瓶口
            FillRect(tex, 57, 34, 14, 14, new Color(0.25f, 0.55f, 0.2f));
            FillRect(tex, 59, 30, 10, 6, new Color(0.4f, 0.4f, 0.4f));
            // 毒液高光
            FillCircle(tex, 57, 52, 5, new Color(0.7f, 1f, 0.6f));
            // 毒气泡
            FillCircle(tex, 44, 48, 5, new Color(0.5f, 0.9f, 0.4f, 0.8f));
            FillCircle(tex, 86, 52, 4, new Color(0.5f, 0.9f, 0.4f, 0.7f));
            FillCircle(tex, 50, 38, 3, new Color(0.6f, 1f, 0.5f, 0.6f));
            return Finalize(tex, "PoisonTower");
        }

        /// <summary>辅助塔：金色基座 + 旗帜</summary>
        private static Sprite GenerateSupport()
        {
            var tex = NewTex();
            DrawBase(tex, new Color(0.85f, 0.7f, 0.25f), new Color(0.55f, 0.42f, 0.1f));
            // 旗杆底座
            FillCircle(tex, 64, 62, 16, new Color(0.6f, 0.5f, 0.2f));
            // 旗杆
            FillRect(tex, 61, 24, 6, 42, new Color(0.4f, 0.35f, 0.2f));
            // 旗帜
            FillTriangle(tex, 64, 30, 32, 22, new Color(0.95f, 0.8f, 0.3f));
            FillTriangle(tex, 64, 30, 24, 16, new Color(1f, 0.9f, 0.5f));
            // 旗帜上的星星/增益标记
            FillDiamond(tex, 64, 32, 8, 10, new Color(1f, 1f, 0.8f));
            // 金色光环
            DrawCircle(tex, 64, 62, 22, new Color(1f, 0.9f, 0.4f, 0.6f), 2);
            return Finalize(tex, "SupportTower");
        }

        // ============================================================
        //  绘制辅助方法
        // ============================================================

        private static Texture2D NewTex()
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            var clear = new Color[TexSize * TexSize];
            tex.SetPixels(clear);
            return tex;
        }

        /// <summary>绘制八边形基座（带渐变和边框）</summary>
        private static void DrawBase(Texture2D tex, Color top, Color bottom)
        {
            int cx = TexSize / 2;
            int cy = 44; // 基座中心偏下
            int r = 38;

            // 基座主体（八边形）
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= TexSize || y < 0 || y >= TexSize) continue;
                    float dx = Mathf.Abs(x - cx);
                    float dy = Mathf.Abs(y - cy);
                    // 八边形判定：切角正方形
                    float d = Mathf.Max(dx, dy, (dx + dy) * 0.75f);
                    if (d <= r)
                    {
                        float t = (float)(y - (cy - r)) / (2f * r);
                        Color c = Color.Lerp(bottom, top, Mathf.Clamp01(t));
                        // 边缘抗锯齿
                        float edge = r - d;
                        if (edge < 2f) c.a *= Mathf.Clamp01(edge * 0.5f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // 基座顶部高光
            FillCircle(tex, cx, cy - 8, 20, new Color(1f, 1f, 1f, 0.15f));

            // 基座底部阴影
            FillCircle(tex, cx, cy + 14, 26, new Color(0f, 0f, 0f, 0.2f));
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            for (int py = y; py < y + h; py++)
            {
                for (int px = x; px < x + w; px++)
                {
                    if (px >= 0 && px < TexSize && py >= 0 && py < TexSize)
                        tex.SetPixel(px, py, c);
                }
            }
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= TexSize || y < 0 || y >= TexSize) continue;
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
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color c, int thickness)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= TexSize || y < 0 || y >= TexSize) continue;
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d <= r && d >= r - thickness)
                        tex.SetPixel(x, y, c);
                }
            }
        }

        /// <summary>绘制菱形（旋转45度的正方形）</summary>
        private static void FillDiamond(Texture2D tex, int cx, int cy, int halfW, int halfH, Color c)
        {
            for (int y = cy - halfH; y <= cy + halfH; y++)
            {
                for (int x = cx - halfW; x <= cx + halfW; x++)
                {
                    if (x < 0 || x >= TexSize || y < 0 || y >= TexSize) continue;
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
        }

        /// <summary>绘制等腰三角形（顶点在上）</summary>
        private static void FillTriangle(Texture2D tex, int cx, int cy, int halfW, int h, Color c)
        {
            for (int y = cy; y < cy + h; y++)
            {
                float t = (float)(y - cy) / h;
                int w = Mathf.RoundToInt(halfW * (1f - t));
                for (int x = cx - w; x <= cx + w; x++)
                {
                    if (x >= 0 && x < TexSize && y >= 0 && y < TexSize)
                        tex.SetPixel(x, y, c);
                }
            }
        }

        private static void DrawLine(Texture2D tex, int x1, int y1, int x2, int y2, Color c, int thickness)
        {
            int steps = Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x1, x2, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y1, y2, t));
                FillRect(tex, x - thickness / 2, y - thickness / 2, thickness, thickness, c);
            }
        }

        private static Sprite Finalize(Texture2D tex, string name)
        {
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize),
                new Vector2(0.5f, 0.4f), PPU);
            sprite.name = name;
            return sprite;
        }
    }
}
