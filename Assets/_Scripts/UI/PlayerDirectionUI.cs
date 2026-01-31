using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerDirectionUI : MonoBehaviour
{
    [Header("绑定")]
    public PlayerController player;
    
    // 请在Inspector里把4个方向的箭头Sprite/Image拖进去
    // 建议用 World Space Canvas 或者直接作为 Player 的子物体 SpriteRenderer
    public GameObject arrowUp;
    public GameObject arrowDown;
    public GameObject arrowLeft;
    public GameObject arrowRight;

    [Header("颜色设置")]
    public Color activeColor = new Color(1, 1, 1, 1f); // 高亮颜色
    public Color inactiveColor = new Color(1, 1, 1, 0.2f); // 变暗颜色

    void Update()
    {
        if (player == null) return;

        // 重置所有箭头颜色
        SetArrowColor(arrowUp, inactiveColor);
        SetArrowColor(arrowDown, inactiveColor);
        SetArrowColor(arrowLeft, inactiveColor);
        SetArrowColor(arrowRight, inactiveColor);

        // 获取玩家当前的预测路径
        List<Vector2Int> path = player.GetCurrentPath();
        
        if (path != null && path.Count > 0)
        {
            // 获取第一步的方向（通常这就够了，或者你可以遍历显示所有步）
            Vector2Int firstStep = path[0];

            if (firstStep == Vector2Int.up) SetArrowColor(arrowUp, activeColor);
            else if (firstStep == Vector2Int.down) SetArrowColor(arrowDown, activeColor);
            else if (firstStep == Vector2Int.left) SetArrowColor(arrowLeft, activeColor);
            else if (firstStep == Vector2Int.right) SetArrowColor(arrowRight, activeColor);
        }
    }

    void SetArrowColor(GameObject arrowObj, Color col)
    {
        if (arrowObj == null) return;
        
        // 如果是用 SpriteRenderer (世界坐标物体)
        var sprite = arrowObj.GetComponent<SpriteRenderer>();
        if (sprite != null) { sprite.color = col; return; }

        // 如果是用 UI Image (Canvas)
        var img = arrowObj.GetComponent<Image>();
        if (img != null) { img.color = col; }
    }
}