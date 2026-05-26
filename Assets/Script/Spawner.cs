using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人生成与胜利判定：Awake 从 Resources 加载预制体；Start 刷第一波；
/// 当 remainEnemies==2 且仍在第一波流程时开门并刷第二波远程怪；
/// remainEnemies==0 时通知 GameManager 胜利。敌人死亡时在 EnemyBase.Dead 里 remainEnemies--。
/// </summary>
public class Spawner : MonoBehaviour
{
    public static Spawner Instance;

    public GameObject enemy1;
    public GameObject enemy2;

    public List<Transform> spawnPosList1 = new List<Transform>();
    public List<Transform> spawnPosList2 = new List<Transform>();

    /// <summary>当前仍存活（未死亡结算）的敌人数量，初始需等于本关敌人总数。</summary>
    public int remainEnemies = 4;

    /// <summary>true 表示尚未触发“第二波+开门”逻辑。</summary>
    public bool isSpawning = true;

    private void Awake()
    {
        Instance = this;
        enemy1 = Resources.Load<GameObject>("Prefab/Enemy1");
        enemy2 = Resources.Load<GameObject>("Prefab/Enemy2");
    }

    void Start()
    {
        SpawnEnemy(spawnPosList1, enemy1);
    }

    void Update()
    {
        // 第一波打到剩 2 只：开门并刷第二波，本分支只执行一次
        if (remainEnemies == 2 && isSpawning)
        {
            Gate.Instance.OpenGate();
            SpawnEnemy(spawnPosList2, enemy2);
            isSpawning = false;
        }

        // 全部敌人死亡
        if (remainEnemies == 0 && !GameManager.Instance.isWin)
        {
            Debug.Log("游戏胜利");
            GameManager.Instance.Win();
        }
    }

    /// <summary>在 spawnList 每个点上各生成一只 enemyType。</summary>
    public void SpawnEnemy(List<Transform> spawnList, GameObject enemyType)
    {
        foreach (var pos in spawnList)
            Instantiate(enemyType, pos);
    }
}
