# 从零开始理解塔防游戏 — 手把手代码讲解

> 假设你会 C# 基础语法和 Unity 基本操作（GameObject、Transform、MonoBehaviour）。
> 我们按**游戏运行的时间顺序**，一步一步看代码做了什么。

---

## 第0步：先理解塔防游戏在玩什么

玩家在地图上建炮塔 → 敌人从起点沿着路走向终点 → 炮塔自动攻击范围内的敌人 → 打死敌人赚金币 → 用金币建更多炮塔/升级 → 敌人走到终点扣生命 → 生命归零游戏失败，打完所有波次游戏胜利。

就这么简单。下面看代码是怎么实现的。

---

## 第1步：游戏启动 — 一切从 Bootstrapper 开始

### 1.1 入口在哪？

场景里挂了一个叫 `GameBootstrapper` 的空物体，上面挂着 `GameBootstrapper.cs` 脚本。

```csharp
public class GameBootstrapper : Singleton<GameBootstrapper>
{
    private void Start()
    {
        BootstrapGame();  // 游戏启动！
    }
}
```

> **什么是 Singleton？** 就是"全局只有一个实例"。
> 比如 `GameManager.Instance` 任何地方都能拿到，不用你手动传引用。
> 好处：方便。坏处：模块之间耦合，不好测试（我们的单元测试就是在跟这个作斗争）。

### 1.2 BootstrapGame() 干了什么？

打开 `GameBootstrapper.cs` 的 `BootstrapGame()` 方法，它按顺序做了这些事：

```
1. SetupCamera()          → 创建摄像机，摆好位置
2. 创建 SpriteManager      → 程序化生成所有图片素材（不用美术！）
3. 创建 EffectsPool        → 爆炸/闪光特效的对象池
4. 创建 AudioManager       → 背景音乐+音效
5. 创建 ObjectPool         → 敌人、子弹的对象池
6. 创建 EnemyRegistry      → 敌人注册表（空间网格）
7. 创建 WaveSystem         → 波次系统
8. 创建 EconomySystem      → 经济系统
9. 创建 GameManager        → 游戏主管理器（金币、生命、状态）
10. SetupMap()             → 画地图：草地、路、障碍物
11. SetupPathPoints()      → 定敌人走的路线（路径点）
12. SetupTowerData()       → 创建6种炮塔的数据配置
13. SetupEnemyData()       → 创建敌人的数据配置
14. SetupWaves()           → 配好每一波出几个什么敌人
15. SetupTowersAndUI()     → 创建炮塔放置器、商店UI、HUD
16. StartFirstWaveCountdown() → 倒计时5秒，然后开始第一波
```

> **为什么要这么多"创建XX"？**
> 因为这是程序化生成的游戏——场景里什么都没有，全靠代码 new 出来。
> 好处：改关卡只要改代码参数，不用手动摆场景。

---

## 第2步：地图和路径是怎么画的？

### 2.1 地图是什么？

就是一片绿色的棋盘格 + 一条棕色的路。

代码在 `SetupMap()` 里：
- 用 `SpriteManager` 生成一张绿色的草地 Sprite
- 铺成 20×12 的格子
- 在上面画一条 S 形的棕色路

### 2.2 敌人走哪条路？

路不是画上去的线条，而是**一串路径点（Path Points）**。

```csharp
private void SetupPathPoints()
{
    // 敌人从左上角进来，沿着这些点一个个走过去，最后从右下角出去
    _pathPoints = new Transform[]
    {
        new GameObject("Path_0").transform,  // 起点
        new GameObject("Path_1").transform,
        new GameObject("Path_2").transform,
        // ... 一串点
        new GameObject("Path_N").transform   // 终点
    };
}
```

> **关键点**：敌人不是"沿着路走"，而是"沿着一串点走"。
> 路的贴图只是给玩家看的视觉效果，实际逻辑就是"从点A走到点B，再走到点C..."。

---

## 第3步：敌人是怎么跑起来的？

### 3.1 敌人的数据长什么样？

`EnemyData.cs` 是一个 ScriptableObject（可在 Inspector 里配的配置文件）：

```csharp
public class EnemyData : ScriptableObject
{
    public int MaxHealth = 100;      // 血量
    public float MoveSpeed = 2f;      // 移动速度
    public int Damage = 1;            // 到终点扣多少生命
    public int RewardGold = 10;       // 打死给多少金币
    public Color BodyColor = Color.red;
}
```

> **ScriptableObject 是什么？**
> 就是一个"纯数据文件"，可以在 Project 窗口里创建多个，
> 比如做一个"小兵"配置、一个"坦克"配置，改数值不用改代码。

### 3.2 敌人实体怎么移动？

打开 `Enemy.cs`，看 `Update()` 方法（每帧调用）：

```csharp
private void Update()
{
    if (!_isActive) return;  // 没激活就不动

    MoveAlongPath();  // 沿路径走
}
```

`MoveAlongPath()` 是核心：

```csharp
private void MoveAlongPath()
{
    // 1. 拿到当前要去的目标点
    Vector3 target = _pathPoints[_currentPathIndex].position;
    
    // 2. 算方向
    Vector3 direction = target - transform.position;
    float distance = direction.magnitude;
    
    // 3. 已经走到点了，切下一个点
    if (distance < 0.1f)
    {
        _currentPathIndex++;
        return;
    }
    
    // 4. 朝目标走一步
    float step = _data.MoveSpeed * Time.deltaTime;  // 速度 × 每帧时间 = 这帧走多远
    transform.position += direction.normalized * step;
}
```

> **Time.deltaTime 是什么？**
> 上一帧到这一帧用了多少秒。
> 比如帧率是 60 FPS，deltaTime 就是 1/60 ≈ 0.0167 秒。
> 用速度 × deltaTime，不管帧率多少，移动速度都是一样的（每秒走2米）。

### 3.3 敌人到终点会怎样？

```csharp
private void ReachEnd()
{
    GameManager.Instance.LoseLives(_data.Damage);  // 扣玩家生命
    EnemyRegistry.Instance.Unregister(this);       // 从注册表移除
    gameObject.SetActive(false);                    // 回收到对象池
}
```

---

## 第4步：炮塔是怎么放的？

### 4.1 商店点一下，怎么就放了？

`TowerShopPanel.cs` 里，每个按钮绑定了一个点击事件：

```csharp
public void OnTowerButtonClicked(int towerIndex)
{
    TowerPlacer.Instance.BeginPlacement(towerIndex);
}
```

然后 `TowerPlacer.cs` 进入"放置模式"：
- 鼠标移动时，在鼠标位置显示一个半透明的预览炮塔
- 绿色 = 可以放，红色 = 不能放（在路上/障碍物上/其他炮塔上）
- 鼠标左键点击 → 真的放一个炮塔
- 右键 → 取消放置

### 4.2 炮塔放下去之后长什么样？

`Tower.cs` 的 `Initialize()` 方法：

```csharp
public void Initialize(TowerData data)
{
    _data = data;
    _currentLevel = 0;
    _currentDamage = data.Damage;      // 初始伤害
    _currentRange = data.Range;        // 攻击范围
    _isPlaced = true;
    
    ApplyVisuals();   // 画炮塔的样子
}
```

---

## 第5步：炮塔是怎么打敌人的？

这是最核心的部分。打开 `Tower.cs` 的 `Update()`：

```csharp
private void Update()
{
    if (!_isPlaced) return;
    
    UpdateTarget();    // 第一步：找敌人
    UpdateAttack();    // 第二步：攻击
}
```

### 5.1 第一步：找敌人

```csharp
private void UpdateTarget()
{
    _enemiesInRange.Clear();
    
    // 问注册表：我这个位置，半径3米内有哪些敌人？
    EnemyRegistry.Instance.GetEnemiesInRange(
        transform.position, 
        _currentRange, 
        _enemiesInRange
    );
    
    // 按策略选一个：最靠近终点的 / 最近的 / 血最多的...
    _currentTarget = SelectTarget(_enemiesInRange);
}
```

> **为什么要问注册表？**
> 因为场景里可能有几百个敌人，炮塔不可能每帧遍历所有敌人。
> 注册表用了"空间网格"优化——把地图分成小格子，
> 炮塔只查自己周围几个格子里的敌人，速度快很多。

### 5.2 第二步：攻击

```csharp
private void UpdateAttack()
{
    // 没有目标就歇着
    if (_currentTarget == null) return;
    
    // 攻击倒计时减时间
    _attackTimer -= Time.deltaTime;
    
    // 冷却到了，开火！
    if (_attackTimer <= 0f)
    {
        FireProjectile();           // 发子弹
        _attackTimer = _currentAttackInterval;  // 重置冷却
    }
}
```

### 5.3 子弹飞过去打中敌人

`Projectile.cs`：

```csharp
private void Update()
{
    // 朝目标飞
    transform.position = Vector3.MoveTowards(
        transform.position, 
        _target.position, 
        _speed * Time.deltaTime
    );
    
    // 距离够近就算命中
    float dist = Vector3.Distance(transform.position, _target.position);
    if (dist < 0.2f)
    {
        HitTarget();  // 打中了！
    }
}
```

命中后：
```csharp
private void HitTarget()
{
    _target.TakeDamage(_damage);  // 敌人扣血
    gameObject.SetActive(false);  // 子弹回收到池子里
}
```

---

## 第6步：敌人被打死会怎样？

回到 `Enemy.cs`：

```csharp
public void TakeDamage(float damage)
{
    if (!_isActive) return;
    
    _currentHealth -= damage;  // 扣血
    
    if (_currentHealth <= 0)
    {
        Die();  // 死了！
    }
}

private void Die()
{
    // 1. 给玩家金币
    GameManager.Instance.AddGold(_data.RewardGold);
    
    // 2. 发事件（告诉所有人我死了）
    EventBus.Publish(new EnemyKilledEvent { 
        RewardGold = _data.RewardGold, 
        Position = transform.position 
    });
    
    // 3. 播放死亡爆炸特效
    EffectsPool.Instance.PlayExplosion(transform.position);
    
    // 4. 从注册表移除
    EnemyRegistry.Instance.Unregister(this);
    
    // 5. 回收到对象池
    gameObject.SetActive(false);
}
```

---

## 第7步：金币和生命是怎么管理的？

### 7.1 GameManager 是大管家

```csharp
public class GameManager : Singleton<GameManager>
{
    private int _currentGold = 300;   // 初始300金币
    private int _currentLives = 10;   // 初始10条命
    
    public void AddGold(int amount)
    {
        _currentGold += amount;
        EventBus.Publish(new GoldChangedEvent { 
            CurrentGold = _currentGold 
        });
    }
    
    public bool TrySpendGold(int amount)
    {
        if (_currentGold < amount) return false;  // 钱不够
        _currentGold -= amount;                    // 扣钱
        return true;
    }
}
```

### 7.2 UI 怎么知道金币变了？事件总线！

HUD（游戏内界面）启动时订阅事件：

```csharp
private void OnEnable()
{
    EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
}

private void OnGoldChanged(GoldChangedEvent evt)
{
    _goldText.text = evt.CurrentGold.ToString();  // 更新金币显示
}
```

> **事件总线模式**：
> GameManager 只管改金币数字，然后"喊一声"：金币变了！
> 谁关心这个事，自己来听（订阅）。
> GameManager 不需要知道有哪些 UI 在听，UI 也不需要知道 GameManager 是谁。
> 两边完全解耦，改一边不影响另一边。

---

## 第8步：波次是怎么一波一波出怪的？

### 8.1 WaveSystem 的计时器

```csharp
private void Update()
{
    if (_countdownActive)
    {
        _waveCountdown -= Time.deltaTime;  // 倒计时减时间
        
        if (_waveCountdown <= 0)
        {
            StartNextWave();  // 时间到，出怪！
        }
    }
}
```

### 8.2 StartWave 做了什么？

```csharp
private void StartWave(int waveIndex)
{
    _isWaveActive = true;
    
    // 启动协程：按间隔一个个出怪
    StartCoroutine(SpawnWaveCoroutine(wave));
}
```

### 8.3 协程出怪

```csharp
private IEnumerator SpawnWaveCoroutine(WaveConfig wave)
{
    foreach (var group in wave.SpawnGroups)
    {
        for (int i = 0; i < group.Count; i++)
        {
            SpawnEnemy(group.EnemyData);  // 出一个敌人
            yield return new WaitForSeconds(group.SpawnInterval);  // 等1秒再出下一个
        }
    }
}
```

> **什么是协程（Coroutine）？**
> 就是"分步执行"的函数。`yield return` 表示"这里暂停，等一段时间再继续"。
> 比如要出5个敌人，每个间隔1秒，用协程写就很自然：
> 出一个 → 等1秒 → 出一个 → 等1秒 → ...
> 如果不用协程，就得自己写计时器，很麻烦。

### 8.4 怎么判断一波打完了？

每个敌人死亡时都会通知 WaveSystem：

```csharp
private void OnEnemyKilled(EnemyKilledEvent evt)
{
    _enemiesRemainingInWave--;  // 剩余敌人减1
    
    if (_enemiesRemainingInWave <= 0)
    {
        CompleteWave();  // 这波打完了！
    }
}
```

打完之后：
```csharp
private void CompleteWave()
{
    _isWaveActive = false;
    _waveCountdown = 5f;      // 倒计时5秒
    _countdownActive = true;   // 开始倒计时下一波
}
```

---

## 第9步：对象池 — 为什么不直接 Instantiate？

### 问题

敌人被打死 → Destroy() → 下一波又要 Instantiate() → 又 Destroy() → ...
Unity 的 Instantiate/Destroy 很慢，频繁调用会造成卡顿（掉帧）。

### 解决方案：对象池

提前创建好一堆敌人，放着不用。要出敌人时，从池子里拿一个激活就行。打死了不销毁，放回池子里下次再用。

```csharp
public class ObjectPool : MonoBehaviour
{
    private Queue<GameObject> _pool = new Queue<GameObject>();
    
    // 初始化：提前造10个敌人放着
    public void InitializePool(GameObject prefab, int size)
    {
        for (int i = 0; i < size; i++)
        {
            var obj = Instantiate(prefab);
            obj.SetActive(false);  // 隐藏
            _pool.Enqueue(obj);    // 放进队列
        }
    }
    
    // 拿一个用
    public GameObject Get(Vector3 pos)
    {
        var obj = _pool.Dequeue();  // 从队头拿
        obj.transform.position = pos;
        obj.SetActive(true);        // 激活
        return obj;
    }
    
    // 用完还回来
    public void Release(GameObject obj)
    {
        obj.SetActive(false);       // 隐藏
        _pool.Enqueue(obj);         // 放回队尾
    }
}
```

> **好处**：没有频繁的 Instantiate/Destroy，性能好很多。
> 子弹、敌人、特效都是这么做的。

---

## 第10步：游戏胜利/失败怎么判断？

### 失败条件：生命归零

```csharp
public void LoseLives(int amount)
{
    _currentLives -= amount;
    
    if (_currentLives <= 0)
    {
        ChangeState(GameState.Defeat);  // 游戏失败
    }
}
```

### 胜利条件：打完所有波次

```csharp
private void CompleteWave()
{
    if (_currentWaveIndex >= _waves.Count - 1)
    {
        GameManager.Instance.ChangeState(GameState.Victory);  // 游戏胜利
    }
}
```

状态一变，EventBus 发事件，UI 收到后显示胜利/失败面板。

---

## 总结：一帧游戏发生了什么？

每一帧（约16毫秒）：

```
1. Enemy.Update()         → 敌人沿路径移动一点
2. Tower.Update()        → 炮塔找敌人、攻击倒计时、开火
3. Projectile.Update()    → 子弹朝目标飞
4. WaveSystem.Update()   → 倒计时、出怪
5. GameManager 各方法     → 玩家操作（放塔、升级）
6. EventBus 派发事件      → UI 更新金币、生命显示
```

就这么循环，一帧一帧地跑起来，就是你看到的游戏了。

---

## 你可能会问的几个问题

**Q: 为什么要搞这么多类？一个脚本写完不行吗？**
A: 可以，但后期改bug会疯。分模块后，改敌人移动只动 Enemy.cs，不影响炮塔。

**Q: EventBus 到底好在哪？**
A: 金币变了，GameManager 不用管谁在听。UI 想更新金币显示，订阅事件就行，不用知道 GameManager 存在。

**Q: 为什么敌人不直接 transform.Translate？**
A: 因为要沿路径走，还要检测什么时候到下一个点，还要处理减速、中毒。

**Q: 对象池为什么要用 Queue（队列）？**
A: 先进先出。拿第一个，用完放最后，循环利用。
