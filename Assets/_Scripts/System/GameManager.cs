using UnityEngine;
using UnityEngine.UI;
using TMPro; // 记得引用 TextMeshPro
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public GameObject gameOverCanvas;   // 整个 GameOver UI 的父物体
    public Image backgroundPanel;       // 那个黑色的背景板 (用来变暗)
    public TMP_Text killerText;         // 显示 "Killed by X" 的文本
    public Button restartButton;        // 重启按钮

    void Awake()
    {
        Instance = this;
        // 游戏开始时隐藏 UI
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
    }

    public void TriggerGameOver(string killerName)
    {
        // 1. 停止游戏逻辑
        if (TickManager.Instance != null)
        {
            TickManager.Instance.IsPaused = true;
        }

        // 2. 激活 UI
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            
            // 设置文本
            if (killerText != null)
            {
                killerText.text = $"YOU WERE HUNTED BY\n<size=150%><color=red>{killerName.ToUpper()}</color></size>";
            }

            // 3. 执行渐变变暗效果 (Coroutine)
            StartCoroutine(FadeInGameOverSequence());
        }
    }

    IEnumerator FadeInGameOverSequence()
    {
        // 初始设为全透明
        Color panelColor = backgroundPanel.color;
        panelColor.a = 0;
        backgroundPanel.color = panelColor;

        float duration = 1.0f; // 1秒变暗
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // 使用 unscaled 防止受 TimeScale 影响
            float alpha = Mathf.Lerp(0f, 0.85f, timer / duration); // 目标透明度 0.85
            
            panelColor.a = alpha;
            backgroundPanel.color = panelColor;
            
            yield return null;
        }
        
        // 确保最后显示的按钮可以点击
        if(restartButton != null) restartButton.gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
        // 恢复 Tick
        if (TickManager.Instance != null) TickManager.Instance.IsPaused = false;
    }
}