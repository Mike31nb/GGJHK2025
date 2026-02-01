using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int targetSurvivalTicks = 60; 
    private int currentTicksPassed = 0;
    
    // --- 新增：下一关的名字 ---
    [Header("Level Flow")]
    [Tooltip("填入下一关的场景名字。Level1填Level2的名字，Level2填Level1的名字")]
    public string nextLevelName; 

    [Header("UI References")]
    public TMP_Text timerText;          
    
    [Header("Game Over UI")]
    public GameObject gameOverCanvas;   
    public Image loseBackgroundPanel;   
    public TMP_Text killerText;         
    public Button loseRestartButton;    // 失败按钮：保持重开当前关

    [Header("Victory UI")]
    public GameObject winCanvas;        
    public Image winBackgroundPanel;    
    public Button winRestartButton;     // 胜利按钮：改为前往下一关

    void Awake()
    {
        Instance = this;
        
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
        if (winCanvas != null) winCanvas.SetActive(false);
        
        // --- 绑定逻辑修改 ---
        // 失败：重玩当前关 (RestartGame)
        if (loseRestartButton != null) loseRestartButton.onClick.AddListener(RestartGame);
        
        // 胜利：去下一关 (LoadNextLevel)
        if (winRestartButton != null) winRestartButton.onClick.AddListener(LoadNextLevel);
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

    public void TriggerGameOver(string killerName)
    {
        if (TickManager.Instance.IsPaused) return; 
        
        Debug.Log("Game Over!");
        TickManager.Instance.IsPaused = true; 

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            if(loseRestartButton != null) loseRestartButton.gameObject.SetActive(false);

            if (killerText != null)
            {
                killerText.text = $"HUNTED BY\n<size=150%><color=red>{killerName.ToUpper()}</color></size>";
            }
            
            StartCoroutine(FadeInUI(loseBackgroundPanel, 0.9f, loseRestartButton));
        }
    }

    public void TriggerVictory()
    {
        if (TickManager.Instance.IsPaused) return;

        Debug.Log("Victory!");
        TickManager.Instance.IsPaused = true; 

        if (winCanvas != null)
        {
            winCanvas.SetActive(true);
            if(winRestartButton != null) winRestartButton.gameObject.SetActive(false);

            // 修改文字提示 (可选)
            // 你可以在这里把按钮上的文字改成 "Next Level"
            
            StartCoroutine(FadeInUI(winBackgroundPanel, 0.9f, winRestartButton));
        }
    }

    IEnumerator FadeInUI(Image panel, float targetAlpha, Button buttonToActivate)
    {
        if (panel == null) yield break;

        panel.gameObject.SetActive(true); 

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

        if (buttonToActivate != null)
        {
            buttonToActivate.gameObject.SetActive(true);
        }
    }

    // --- 失败时调用：重开当前场景 ---
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (TickManager.Instance != null) TickManager.Instance.IsPaused = false;
    }

    // --- 胜利时调用：去下一关 ---
    public void LoadNextLevel()
    {
        if (string.IsNullOrEmpty(nextLevelName))
        {
            Debug.LogError("【错误】你忘了在 Inspector 里填 Next Level Name！");
            // 如果忘了填，就重开当前关保底
            RestartGame(); 
            return;
        }

        SceneManager.LoadScene(nextLevelName);
        if (TickManager.Instance != null) TickManager.Instance.IsPaused = false;
    }
}