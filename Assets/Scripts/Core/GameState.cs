namespace TowerDefense.Core
{
    /// <summary>
    /// 游戏状态枚举。状态机驱动游戏流程。
    /// </summary>
    public enum GameState
    {
        /// <summary>初始化中</summary>
        Initializing,
        /// <summary>准备阶段（玩家可建造防御塔）</summary>
        Preparation,
        /// <summary>战斗阶段（敌人正在进攻）</summary>
        InWave,
        /// <summary>波次间隙</summary>
        BetweenWaves,
        /// <summary>游戏胜利</summary>
        Victory,
        /// <summary>游戏失败</summary>
        Defeat,
        /// <summary>游戏暂停</summary>
        Paused
    }
}
