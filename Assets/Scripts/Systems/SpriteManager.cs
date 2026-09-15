using UnityEngine;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 精灵素材管理器。统一加载和管理 Kenney 塔防素材包中的精灵。
    /// 素材位于 Assets/Resources/Sprites/。
    /// 为避免 PNG 导入类型不是 Sprite 导致加载失败，会回退到
    /// 加载 Texture2D 后用 Sprite.Create 动态生成精灵。
    /// </summary>
    public class SpriteManager : MonoBehaviour
    {
        public static SpriteManager Instance { get; private set; }

        // 地形精灵
        public Sprite GrassTile { get; private set; }
        public Sprite PathTile { get; private set; }
        public Sprite GrassDecoration { get; private set; }

        // 塔精灵
        public Sprite TowerBase { get; private set; }
        public Sprite CannonTower { get; private set; }
        public Sprite ArrowTower { get; private set; }
        public Sprite DoubleCannon { get; private set; }
        public Sprite MissileTower { get; private set; }

        // 敌人精灵
        public Sprite EnemyNormal { get; private set; }
        public Sprite EnemyFast { get; private set; }
        public Sprite EnemyTank { get; private set; }

        // 子弹/特效精灵
        public Sprite BulletYellow { get; private set; }
        public Sprite BulletGray { get; private set; }
        public Sprite BulletBrown { get; private set; }
        public Sprite FireSmall { get; private set; }
        public Sprite FireLarge { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadSprites();
        }

        private void LoadSprites()
        {
            // 地形
            GrassTile = LoadSprite("towerDefense_tile024");
            PathTile = LoadSprite("towerDefense_tile060");

            // 敌人（僵尸/机器人原始像素较小，用 52 PPU 放大到与旧敌人相近）
            EnemyFast = LoadSprite("enemy_zombie", 52f);
            EnemyNormal = LoadSprite("enemy_zombie", 52f);
            EnemyTank = LoadSprite("enemy_robot", 52f);

            // 塔
            TowerBase = LoadSprite("towerDefense_tile245");
            ArrowTower = LoadSprite("towerDefense_tile249");
            CannonTower = LoadSprite("towerDefense_tile250");
            DoubleCannon = LoadSprite("towerDefense_tile203");
            MissileTower = LoadSprite("towerDefense_tile205");

            // 子弹
            BulletYellow = LoadSprite("towerDefense_tile272");
            BulletGray = LoadSprite("towerDefense_tile275");
            BulletBrown = LoadSprite("towerDefense_tile274");

            // 特效
            FireSmall = LoadSprite("towerDefense_tile295");
            FireLarge = LoadSprite("towerDefense_tile298");

            // 装饰
            GrassDecoration = LoadSprite("towerDefense_tile130");

        }

        /// <summary>
        /// 加载精灵。先尝试直接加载 Sprite；若该纹理未以 Sprite 类型导入，
        /// 则加载 Texture2D 并用 Sprite.Create 动态生成（pivot 中心）。
        /// </summary>
        private Sprite LoadSprite(string name, float pixelsPerUnit = 100f)
        {
            // 1. 直接加载 Sprite（导入类型正确时）
            var sprite = Resources.Load<Sprite>($"Sprites/{name}");
            if (sprite != null)
            {
                // 若调用方指定了不同 PPU，重新创建以应用放大/缩小
                if (Mathf.Approximately(pixelsPerUnit, 100f))
                    return sprite;
            }

            // 2. 加载 Texture2D，动态创建 Sprite（不依赖导入类型）
            var tex = Resources.Load<Texture2D>($"Sprites/{name}");
            if (tex == null)
            {
                Debug.LogWarning($"[SpriteManager] 找不到素材: {name}");
                return sprite; // 可能为 null
            }

            var pivot = new Vector2(0.5f, 0.5f);
            var rect = new Rect(0, 0, tex.width, tex.height);
            var created = Sprite.Create(tex, rect, pivot, pixelsPerUnit);
            created.name = name;
            return created;
        }

        /// <summary>
        /// 按编号加载精灵。
        /// </summary>
        public Sprite GetTile(int number)
        {
            string name = $"towerDefense_tile{number:D3}";
            return LoadSprite(name);
        }
    }
}
