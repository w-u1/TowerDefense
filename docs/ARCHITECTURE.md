# 塔防大作战 — 代码模块架构解析

> 基于 Unity 2022.3 / C# 编写，共 35 个脚本，分为 6 大模块。
> 核心设计模式：**事件总线解耦 + 单例管理 + 对象池 + 状态机 + 空间网格**。

---

## 模块总览

```
Assets/Scripts/
├── Core/        (7)  核心架构：单例、事件总线、状态机、游戏管理器、障碍物
├── Enemies/     (3)  敌人系统：敌人实体、数据配置、生成器
├── Systems/    (10)  通用系统：对象池、空间网格、波次、经济、音频、特效、存档
├── Towers/      (6)  炮塔系统：炮塔实体、数据、放置、子弹、视觉工厂
├── UI/         (10)  界面系统：主菜单、HUD、商店、面板管理
└── Utils/       (1)  工具类：扩展方法
```

---

## 一、核心架构模块 (Core)

### 1.1 Singleton.cs — 泛型单例基类

**作用**：所有管理器类的基类，提供全局唯一实例访问。

**核心逻辑**：
- `Instance` 属性：懒加载，找不到时自动创建 GameObject 挂载
- `Awake()`：防止重复实例，多余对象自动销毁
- `OnSingletonAwake()`：子类钩子方法，替代 Awake 避免破坏单例逻辑
- 支持 `DontDestroyOnLoad`（场景切换不销毁）

**继承者**：GameManager、EnemyRegistry、WaveSystem、AudioManager、UIManager、SaveSystem

---

### 1.2 EventBus.cs — 轻量级事件总线

**作用**：解耦各系统间的直接引用，发布/订阅模式。

**核心设计**：
- 静态类，`Dictionary<Type, Delegate>` 存储每个事件类型的订阅者
- `Subscribe<T>()` / `Unsubscribe<T>()` / `Publish<T>()` 泛型方法
- 事件参数全部用 **struct**（值类型，避免 GC 分配）
- Publish 时 try-catch 包裹，单个订阅者异常不影响其他订阅者

**预定义事件（12个）**：

| 事件 | 用途 | 发布者 | 订阅者 |
|---|---|---|---|
| GoldChangedEvent | 金币变化 | GameManager | HUD、商店 |
| LivesChangedEvent | 生命变化 | GameManager | HUD |
| WaveStartedEvent | 波次开始 | WaveSystem | HUD |
| WaveCompletedEvent | 波次结束 | WaveSystem | GameManager |
| EnemyKilledEvent | 敌人被击杀 | Enemy | WaveSystem、经济 |
| EnemyReachedEndEvent | 敌人到终点 | Enemy | GameManager |
| TowerBuiltEvent | 炮塔建造 | TowerPlacer | 音效 |
| GameStateChangedEvent | 状态切换 | GameManager | HUD、暂停 |
| GameVictoryEvent / DefeatEvent | 胜负 | GameManager | 结算面板 |
| EnemiesRemainingChangedEvent | 剩余敌人 | WaveSystem | HUD |
| GameResetEvent | 游戏重置 | 启动器 | 各系统 |

---

### 1.3 GameState.cs — 游戏状态枚举

**7个状态**：
```
Initializing → Preparation → InWave ⇄ BetweenWaves
                              ↓
                    Victory / Defeat
                              ↓
                           Paused
```

---

### 1.4 GameManager.cs — 游戏主管理器

**继承**：Singleton<GameManager>

**职责**：
- 全局配置：初始金币 300、初始生命 10、总波数、波次间隔 5 秒
- 状态机：`ChangeState()` 切换状态并发布 GameStateChangedEvent
- 经济操作：`AddGold()` / `TrySpendGold()`（余额不足返回 false）
- 生命操作：`LoseLives()`（归零时触发 Defeat）
- 子系统注册：持有 ObjectPool、EconomySystem、WaveSystem 引用

---

### 1.5 GameBootstrapper.cs — 游戏启动器

**作用**：场景入口，负责初始化整个游戏世界。

**职责**：
- 程序化生成地图（草地、道路、障碍物）
- 按关卡配置生成路径点
- 创建并初始化所有系统（ObjectPool、WaveSystem、EnemyRegistry 等）
- 调用各系统的 Setup 方法
- 处理关卡加载/重置

---

### 1.6 Obstacle.cs — 障碍物实体

**作用**：地图上可攻击的障碍物（树、石头、仙人掌等）。

**特性**：
- 有血量，炮塔可优先攻击选中的障碍物
- 被选中时显示黄色圆环标记 + 血条
- 摧毁后给玩家金币奖励
- 阻挡炮塔放置（占用地格）
- 左击选中（单选），右击取消选中

---

### 1.7 ClickableReward.cs — 可点击奖励

**作用**：地图上随机出现的金币奖励，玩家点击后获得。

---

## 二、敌人模块 (Enemies)

### 2.1 EnemyData.cs — 敌人数据配置（ScriptableObject）

**字段**：
- 基础属性：MaxHealth、Speed、Damage（对玩家造成的伤害）、RewardGold（击杀金币）
- 视觉：BodyColor、Size
- 特殊类型：IsStealth（隐身）、IsFlying（飞行）
- 等级分类：普通、快速、坦克、Boss

---

### 2.2 Enemy.cs — 敌人实体（MonoBehaviour）

**核心逻辑**：
- **初始化**：`Initialize(data, pathPoints)` 设置血量、速度、激活状态
- **移动**：沿路径点数组 Waypoints 移动，到达终点扣玩家生命
- **受击**：`TakeDamage()` 扣血，归零则 Die()
- **死亡**：发布 EnemyKilledEvent，播放死亡特效，回收到对象池
- **减速**：冰塔减速效果，有持续时间和强度
- **血条**：世界空间 Sprite 血条，跟随敌人头顶
- **注册**：初始化时自动注册到 EnemyRegistry，死亡时注销

---

### 2.3 EnemySpawner.cs — 敌人生成器

**作用**：从对象池取出敌人，放置到路径起点。

**职责**：
- 根据 WaveSystem 的指令生成敌人
- 设置敌人类型、路径点
- 处理生成间隔和延迟

---

## 三、系统模块 (Systems)

### 3.1 ObjectPool.cs — 通用对象池

**设计**：
- `Dictionary<int, Queue<GameObject>>` 按预制体 InstanceID 分池
- `InitializePool(prefab, size)` 预生成 N 个对象
- `Get(prefab, pos, rot)` 取出对象，池空自动扩容
- `Release(prefab, obj)` 回收对象
- 每种池有独立的父节点 Container，保持 Hierarchy 整洁

**用途**：敌人、子弹、爆炸特效、命中特效 —— 避免频繁 Instantiate/Destroy 的 GC 开销。

---

### 3.2 EnemyRegistry.cs — 敌人注册表 + 空间网格

**作用**：维护所有活跃敌人，提供 O(1) 范围查询。

**两层数据结构**：
1. **总列表** `_activeEnemies`：所有活跃敌人
2. **空间网格** `_gridBuckets`：把地图分成 3×3 的格子，每个格子存该区域的敌人

**查询流程**：
```
GetEnemiesInRange(center, radius):
  1. 计算查询覆盖哪些格子 (minCol~maxCol, minRow~maxRow)
  2. 遍历这些格子中的敌人
  3. 精确距离判断 (sqrMagnitude <= radius²)
  4. 返回结果列表
```

**性能提升**：从 O(n²)（每个炮塔遍历所有敌人）降到 O(k)（k 是附近格子的敌人数）。

---

### 3.3 WaveSystem.cs — 波次系统

**状态**：
- `_isWaveActive`：当前是否有波次进行中
- `_countdownActive`：是否在倒计时
- `_waveCountdown`：倒计时剩余时间

**流程**：
```
游戏开始 → StartFirstWaveCountdown()（5秒倒计时）
    ↓ 倒计时结束
StartWave(0) → 启动协程 SpawnWaveCoroutine
    ↓ 按间隔逐个生成敌人
敌人全部死亡 → CheckWaveComplete() → CompleteWave()
    ↓
5秒倒计时 → 下一波...
    ↓ 所有波次完成
GameVictory
```

**波次配置**：`WaveConfig` 包含多个 `EnemySpawnGroup`（敌人类型+数量+间隔+延迟）。

---

### 3.4 EconomySystem.cs — 经济系统

**职责**：
- `CanAfford(cost)`：判断是否买得起
- `FormatGold(n)`：格式化金币显示（100 → "100"，10000 → "10.0K"，1000000 → "1.0M"）

> 实际金币增减在 GameManager 中，这里主要负责展示格式化。

---

### 3.5 AudioManager.cs — 音频管理器

**特性**：
- **程序化 BGM**：用 AudioClip.Create 实时生成 PCM 数据，无需外部音乐文件
- **音效系统**：点击按钮、炮塔攻击、敌人死亡、升级等音效
- 音乐和音效分开音量控制
- 通过 SaveSystem 持久化音量设置

---

### 3.6 EffectsPool.cs — 特效对象池

**专用对象池**：管理爆炸、命中闪光、环形扩散等特效的生成和回收。

---

### 3.7 SpriteManager.cs — 精灵素材管理

**作用**：程序化生成所有 2D 精灵素材（炮塔、敌人、障碍物、子弹），无需外部美术资源。

---

### 3.8 SaveSystem.cs — 存档系统

**功能**：
- 保存/加载：金币、生命、音量设置、关卡进度
- 用 PlayerPrefs 持久化
- 启动时读取设置应用到 AudioManager

---

### 3.9 NewGameDataConfig.cs — 新游戏数据配置

**作用**：定义每关的初始金币、生命、波数等配置数据。

---

## 四、炮塔模块 (Towers)

### 4.1 TowerData.cs — 炮塔数据配置（ScriptableObject）

**6种炮塔**：

| 类型 | 花费 | 特点 |
|---|---|---|
| Archer 箭塔 | 50 | 单体物理，攻速快 |
| Cannon 炮塔 | 100 | 范围伤害，攻速慢 |
| Frost 冰塔 | 75 | 减速敌人，伤害低 |
| Laser 激光塔 | 150 | 持续伤害，高DPS |
| Poison 毒塔 | 80 | DoT持续伤害 |
| Support 辅助塔 | 120 | 增益周围塔攻速+伤害 |

**升级系统**：`TowerUpgradeData[] Upgrades` 每级有花费、伤害倍率、范围加成、攻速加成。
伤害公式：`当前伤害 = 基础伤害 × ∏(每级DamageMultiplier)`（累乘）。

---

### 4.2 Tower.cs — 炮塔实体（MonoBehaviour）

**核心逻辑**：
- **初始化**：`Initialize(data)` 设置等级0、基础伤害、范围
- **升级**：`Upgrade()` 检查等级上限 → 扣金币 → 等级+1 → 重算属性 → 播放音效
- **目标选择**：`UpdateTarget()` 根据策略（First/Closest/Strongest/Weakest）选目标
- **攻击**：`UpdateAttack()` 冷却到了就发射子弹
- **视觉**：升级时变大、加光环、变色
- **辅助塔**：不攻击，给周围塔加 buff

---

### 4.3 TowerPlacer.cs — 炮塔放置器

**职责**：
- 鼠标悬停显示预览（绿色=可放，红色=不可放）
- 点击放置炮塔，扣金币
- 检查放置合法性：不在路上、不在障碍物上、不重叠
- 右键取消选择
- 选中已放置的炮塔显示信息面板

---

### 4.4 Projectile.cs — 子弹实体

**行为**：
- 朝目标飞行，追踪目标位置
- 命中后造成伤害（单体/范围）
- 有飞行速度、伤害、效果（减速/中毒）
- 命中后回收到对象池

---

### 4.5 TowerVisualFactory.cs — 炮塔视觉工厂

**作用**：程序化生成炮塔的 Sprite 素材（不同颜色、形状对应不同炮塔类型和等级）。

---

### 4.6 ProjectileVisualFactory.cs — 子弹视觉工厂

**作用**：程序化生成子弹的 Sprite 素材。

---

## 五、UI模块 (UI)

### 5.1 UIManager.cs — UI管理器（Singleton）

**职责**：管理所有 UI 面板的显示/隐藏切换。

---

### 5.2 HUDController.cs — 游戏内HUD

**显示内容**：
- 左上角：金币数量、生命值
- 顶部中间：波次进度、倒计时
- 右上角：倍速按钮（1x/2x/3x）、菜单按钮
- 右上角：实时FPS显示（每0.5秒刷新）

---

### 5.3 MainMenuPanel.cs — 主菜单

**按钮**：开始游戏、关卡选择、退出游戏。

---

### 5.4 LevelSelectPanel.cs — 关卡选择

**5个关卡卡片**：
1. 草原 — 经典S形路径
2. 沙漠 — 曲折之字形
3. 冰雪 — 环形包围战
4. 火山 — 熔岩地形
5. 森林 — 复杂迷宫

---

### 5.5 TowerShopPanel.cs — 炮塔商店

**布局**：右侧竖排，6个炮塔按钮（图标+名称+价格）。
**交互**：点击选择炮塔类型 → 进入放置模式。

---

### 5.6 TowerInfoPanel.cs — 炮塔信息面板

**显示**：选中炮塔的等级、伤害、范围、攻速、升级按钮、出售按钮。

---

### 5.7 PauseMenuPanel.cs — 暂停菜单

**按钮**：继续游戏、重新开始、返回主界面、音量设置。

---

### 5.8 GameOverPanel.cs — 结算面板

**显示**：胜利/失败、波次成绩、下一关/重玩按钮。

---

### 5.9 FloatingTextManager.cs — 飘字管理器

**作用**：在地图上显示临时浮动文字（如 "+50金币"）。

---

## 六、工具模块 (Utils)

### 6.1 ExtensionMethods.cs — 扩展方法

**作用**：常用的 C# 扩展方法（如安全的集合操作、字符串处理等）。

---

## 架构设计亮点

1. **事件总线解耦**：UI 不需要直接引用 GameManager，只订阅 GoldChangedEvent 就能更新金币显示。系统之间零直接依赖。

2. **对象池优化**：敌人、子弹、特效全部池化，避免运行时 Instantiate/Destroy 造成的 GC 卡顿。

3. **空间网格加速**：炮塔找敌人从 O(n²) 降到 O(k)，敌人多的时候性能差异巨大。

4. **ScriptableObject 配置**：炮塔、敌人的数据全部用 SO 配置，策划可以在 Inspector 里调数值不用改代码。

5. **程序化美术**：所有精灵素材都是代码生成的，项目体积小，不用管理大量美术文件。

6. **状态机驱动**：GameState 枚举 + ChangeState，游戏流程清晰可预测。
