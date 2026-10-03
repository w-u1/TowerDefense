# 塔防大作战

一款基于 Unity 2022.3 开发的 2D 塔防游戏。

## 游戏特色

- **6种炮塔**：箭塔、炮塔、冰塔、激光塔、毒塔、辅助塔，各有独特攻击方式和升级路线
- **5个关卡**：草原、沙漠、冰雪、火山、森林，每关独立地图配色和障碍物
- **多种敌人**：普通、快速、坦克、Boss、精英，含飞行和隐身单位
- **障碍物系统**：可选中障碍物，炮塔优先攻击，摧毁后获得金币
- **完整UI**：主界面、关卡选择、暂停菜单、游戏结束面板、塔信息面板
- **自动波次**：每波间隔5秒自动倒计时，支持1x/2x/3x倍速
- **程序化美术**：炮塔、敌人、子弹、特效全部程序化生成，无需外部素材依赖
- **音效系统**：背景音乐 + 攻击/建造/升级/金币等全部音效

## 操作说明

- **左键点击商店炮塔** → 进入放置模式，再点地图放置
- **左键点击已放置炮塔** → 查看信息、升级、出售
- **左键点击障碍物** → 选中障碍物，炮塔优先攻击
- **右键 / ESC** → 取消放置 / 取消选中
- **倍速按钮** → 切换1x/2x/3x游戏速度
- **菜单按钮** → 暂停游戏

## 技术架构

- **空间网格**：EnemyRegistry 使用空间分桶，避免每塔遍历全部敌人
- **对象池**：EffectsPool 管理死亡/命中/炮口特效
- **事件总线**：EventBus 静态事件系统，解耦各模块
- **存档系统**：SaveSystem 基于 PlayerPrefs + JSON
- **程序化生成**：TowerVisualFactory / ProjectileVisualFactory / EffectsPool

## 开发环境

- Unity 2022.3.62f1
- .NET Standard 2.1
- 2D URP（内置渲染管线）

## 项目结构

```
Assets/
├── Scripts/
│   ├── Core/          # 核心系统（GameManager、GameBootstrapper、Obstacle等）
│   ├── Towers/        # 炮塔系统（Tower、TowerPlacer、TowerVisualFactory等）
│   ├── Enemies/       # 敌人系统（Enemy、EnemySpawner等）
│   ├── Systems/       # 系统模块（WaveSystem、AudioManager、EffectsPool等）
│   └── UI/            # UI面板（HUD、商店、主菜单、暂停菜单等）
├── Scenes/            # 场景文件
├── Resources/         # 运行时加载资源
└── Editor/            # 编辑器工具
```

## 版本

v1.0 正式版
