using UnityEngine;
using System;

public class TickManager : MonoBehaviour
{
    public static TickManager Instance;

    public float timeBetweenTicks = 0.5f;
    private float timer;

    // 死亡时的暂停按钮
    public bool IsPaused = false;
    
    [Header("Audio")]
    public AudioSource tickSource; // 拖入挂在物体上的 AudioSource
    public AudioClip tickClip;     // 拖入你找好的声音
    private AudioClip generatedTickClip;
    
    // --- 核心生成代码 ---
    AudioClip GenerateSoftClick()
    {
        // 配置：想要声音更低沉？把 frequency 改小 (比如 600)
        // 想要声音更短？把 duration 改小 (比如 0.05f)
        float frequency = 800f; 
        float duration = 0.08f; 
        int sampleRate = 44100;
        
        int sampleCount = (int)(sampleRate * duration);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            
            // 1. 基础波形：正弦波 (Sine Wave) 是最圆润的，完全没有棱角
            float wave = Mathf.Sin(2 * Mathf.PI * frequency * t);

            // 2. 音量包络 (Envelope)：让声音迅速衰减，而不是一直响
            // 线性衰减还不够，用平方衰减会让尾巴收得更干净，像 "Click" 而不是 "Beep"
            float progress = (float)i / sampleCount;
            float envelope = 1f - progress; 
            envelope = envelope * envelope; // 平方曲线

            data[i] = wave * envelope; 
        }

        // 创建 Clip
        AudioClip clip = AudioClip.Create("ProceduralTick", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // 分成两个阶段
    public event Action OnPlayerTick; // 阶段1：玩家先动
    public event Action OnEnemyTick;  // 阶段2：敌人后动，并进行收割判定
    
    void Awake()
    {
        Instance = this;
    
        // --- 新增代码开始 ---
        // 如果没有在面板上拖 AudioSource，代码自动找；如果找不到，代码自动加一个。
        if (tickSource == null)
        {
            tickSource = GetComponent<AudioSource>();
            if (tickSource == null)
            {
                tickSource = gameObject.AddComponent<AudioSource>();
            }
        }
        // --- 新增代码结束 ---
    
        generatedTickClip = GenerateSoftClick(); 
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
        PlayTickSound();
        
        
        // 1. 先让玩家走，更新完 GridNode 的占位信息
        OnPlayerTick?.Invoke();
        
        
        // 2. 再让敌人走，基于玩家最新的位置进行移动和击杀判定
        OnEnemyTick?.Invoke();
        
        
      
        
    }
    
    void PlayTickSound()
    {
        if (tickSource != null && generatedTickClip != null)
        {
            tickSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            // 音量给小点，0.5 左右
            tickSource.PlayOneShot(generatedTickClip, 0.5f); 
        }
    }
}