using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int targetSurvivalTicks = 60; // 目标：坚持多少个Tick
    private int currentTicksPassed = 0;

    [Header("UI References")]
    public TMP_Text timerText;          // 倒计时文本
    
    [Header("Game Over UI")]
    public GameObject gameOverCanvas;   // 失败界面父物体
    public Image loseBackgroundPanel;   // 失败背景
    public TMP_Text killerText;         // "Killed by X"
    public Button loseRestartButton;    // 失败重开按钮

    [Header("Victory UI")]
    public GameObject winCanvas;        // 胜利界面父物体
    public Image winBackgroundPanel;    // 胜利背景
    public Button winRestartButton;     // 胜利重开按钮

    void Awake()
    {
        Instance = this;
        // 游戏开始时隐藏所有结算 UI
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
        if (winCanvas != null) winCanvas.SetActive(false);
        
        // 绑定按钮事件
        if (loseRestartButton != null) loseRestartButton.onClick.AddListener(RestartGame);
        if (winRestartButton != null) winRestartButton.onClick.AddListener(RestartGame);
    }

    void Start()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnPlayerTick += OnTick;
        }
        
        UpdateTimerUI();
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnPlayerTick -= OnTick;
        }
    }

    void OnTick()
    {
        if (TickManager.Instance.IsPaused) return;

        currentTicksPassed++;
        UpdateTimerUI();

        // 检查胜利条件
        if (currentTicksPassed >= targetSurvivalTicks)
        {
            TriggerVictory();
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int ticksLeft = Mathf.Max(0, targetSurvivalTicks - currentTicksPassed);
            timerText.text = $"SURVIVE: {ticksLeft}";
            
            if (ticksLeft <= 10) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }

    // --- 失败逻辑 ---
    public void TriggerGameOver(string killerName)
    {
        if (TickManager.Instance.IsPaused) return; 
        
        Debug.Log("Game Over!");
        TickManager.Instance.IsPaused = true; 

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            
            // 先隐藏按钮，等待渐变结束
            if(loseRestartButton != null) loseRestartButton.gameObject.SetActive(false);

            if (killerText != null)
            {
                killerText.text = $"HUNTED BY\n<size=150%><color=red>{killerName.ToUpper()}</color></size>";
            }
            
            // 【关键】把 loseRestartButton 传给协程，让协程最后负责显示它
            StartCoroutine(FadeInUI(loseBackgroundPanel, 0.9f, loseRestartButton));
        }
    }

    // --- 胜利逻辑 ---
    public void TriggerVictory()
    {
        if (TickManager.Instance.IsPaused) return;

        Debug.Log("Victory!");
        TickManager.Instance.IsPaused = true; 

        if (winCanvas != null)
        {
            winCanvas.SetActive(true);

            // 先隐藏按钮
            if(winRestartButton != null) winRestartButton.gameObject.SetActive(false);

            // 【关键】把 winRestartButton 传给协程
            StartCoroutine(FadeInUI(winBackgroundPanel, 0.9f, winRestartButton));
        }
    }

    // --- 通用 UI 渐变 ---
    IEnumerator FadeInUI(Image panel, float targetAlpha, Button buttonToActivate)
    {
        if (panel == null) yield break;

        panel.gameObject.SetActive(true); // 确保Panel是开着的

        Color c = panel.color;
        c.a = 0;
        panel.color = c;

        float duration = 1.0f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; 
            float alpha = Mathf.Lerp(0f, targetAlpha, timer / duration);
            c.a = alpha;
            panel.color = c;
            yield return null;
        }
        
        c.a = targetAlpha;
        panel.color = c;

        // 渐变结束，这里负责把对应的按钮打开！
        if (buttonToActivate != null)
        {
            buttonToActivate.gameObject.SetActive(true);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (TickManager.Instance != null) TickManager.Instance.IsPaused = false;
    }
}