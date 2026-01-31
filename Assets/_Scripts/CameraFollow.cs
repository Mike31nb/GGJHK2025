using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("把玩家拖到这里")]
    public Transform target; // 你的 Player

    [Header("平滑度 (数值越大跟得越紧)")]
    public float smoothSpeed = 10f; 

    // 必须用 LateUpdate，确保在玩家动完之后摄像机才动
    // 否则会出现画面抖动
    void LateUpdate()
    {
        if (!target) return;

        // 1. 目标位置：只取玩家的 X 和 Y，保持摄像机原来的 Z (通常是 -10)
        Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z);

        // 2. 平滑移动：从当前位置 Lerp 到 目标位置
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}