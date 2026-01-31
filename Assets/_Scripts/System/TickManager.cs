using UnityEngine;
using System;

public class TickManager : MonoBehaviour
{
    public static TickManager Instance;

    public float timeBetweenTicks = 0.5f;
    private float timer;

    // 死亡时的暂停按钮
    public bool IsPaused = false;
    
    // 分成两个阶段
    public event Action OnPlayerTick; // 阶段1：玩家先动
    public event Action OnEnemyTick;  // 阶段2：敌人后动，并进行收割判定

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (IsPaused) return;
        timer += Time.deltaTime;
        if (timer >= timeBetweenTicks)
        {
            timer -= timeBetweenTicks;
            FireTick();
        }
    }

    void FireTick()
    {
        
        // 1. 先让玩家走，更新完 GridNode 的占位信息
        OnPlayerTick?.Invoke();
        
        
        // 2. 再让敌人走，基于玩家最新的位置进行移动和击杀判定
        OnEnemyTick?.Invoke();
        
    }
}