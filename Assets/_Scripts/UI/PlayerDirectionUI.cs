using UnityEngine;
using System.Collections.Generic;

public class PlayerDirectionUI : MonoBehaviour
{
    [Header("绑定 Player")]
    public PlayerController player;

    [System.Serializable]
    public struct ArrowSet
    {
        [Header("场景里的物体 (SpriteRenderer)")]
        public SpriteRenderer targetRenderer; // 拖入场景里主角身边的那个箭头物体

        [Header("三张状态图")]
        public Sprite spriteLevel0; // 强度0：不走 (可以是空图，或者半透明的灰图)
        public Sprite spriteLevel1; // 强度1：走1格 (短箭头)
        public Sprite spriteLevel2; // 强度2：走2格及以上 (长箭头)
    }

    [Header("四个方向的配置")]
    public ArrowSet upArrow;
    public ArrowSet downArrow;
    public ArrowSet leftArrow;
    public ArrowSet rightArrow;

    void Update()
    {
        if (player == null) return;

        // 1. 获取预测路径
        List<Vector2Int> path = player.GetCurrentPath();

        // 2. 计算总位移 (Total Displacement)
        // 比如 Fox 往右上跳: path可能是 [(0,1), (0,1), (1,0)] -> 总和 (1, 2)
        // 意味着 X轴强度1，Y轴强度2
        Vector2Int totalDelta = Vector2Int.zero;
        if (path != null)
        {
            foreach (var step in path)
            {
                totalDelta += step;
            }
        }

        // 3. 根据位移量，更新四个方向的图片
        // Y轴 (上下)
        UpdateArrowSprite(upArrow, totalDelta.y);       // 正数是上
        UpdateArrowSprite(downArrow, -totalDelta.y);    // 负数是下 (传进去转正)

        // X轴 (左右)
        UpdateArrowSprite(rightArrow, totalDelta.x);    // 正数是右
        UpdateArrowSprite(leftArrow, -totalDelta.x);    // 负数是左
    }

    // 统一处理函数
    void UpdateArrowSprite(ArrowSet arrowSet, int value)
    {
        if (arrowSet.targetRenderer == null) return;

        if (value <= 0)
        {
            // 没动静
            arrowSet.targetRenderer.sprite = arrowSet.spriteLevel0;
            // 如果你的 Level0 是空图，记得把颜色设为白色(如果不透明)或者透明
            // 这里假设你 Level0 可能是一张灰色的底图
        }
        else if (value == 1)
        {
            // 走1格
            arrowSet.targetRenderer.sprite = arrowSet.spriteLevel1;
        }
        else
        {
            // 走 >= 2格
            arrowSet.targetRenderer.sprite = arrowSet.spriteLevel2;
        }
    }
}