using UnityEngine;

public class MoveTargetMarker : MonoBehaviour
{
    private const int TextureSize = 32;
    private static Sprite markerSprite;

    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private Vector3 baseScale;
    private float pulseTimer;

    public static MoveTargetMarker Create(string markerName, int sortingOrder)
    {
        GameObject markerObject = new GameObject(markerName);
        MoveTargetMarker marker = markerObject.AddComponent<MoveTargetMarker>();
        marker.SetSortingOrder(sortingOrder);
        marker.Hide();
        return marker;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = GetMarkerSprite();
        spriteRenderer.sortingOrder = 4;
        baseColor = Color.white;
        baseScale = Vector3.one * 0.85f;
    }

    void Update()
    {
        pulseTimer += Time.deltaTime * 7f;
        float pulse = 0.9f + Mathf.Sin(pulseTimer) * 0.06f;
        transform.localScale = baseScale * pulse;

        Color color = baseColor;
        color.a *= 0.75f + Mathf.Sin(pulseTimer) * 0.15f;
        spriteRenderer.color = color;
    }

    public void SetSortingOrder(int sortingOrder)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingOrder = sortingOrder;
    }

    public void Show(Vector3 worldPos, Color color, float scale)
    {
        worldPos.z = -0.15f;
        transform.position = worldPos;
        baseColor = color;
        baseScale = Vector3.one * scale;
        pulseTimer = 0f;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    static Sprite GetMarkerSprite()
    {
        if (markerSprite != null) return markerSprite;

        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.name = "Generated Move Target Marker";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = new Color(1f, 1f, 1f, 0.22f);
        Color border = new Color(1f, 1f, 1f, 0.95f);

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float nx = Mathf.Abs((x + 0.5f) / TextureSize - 0.5f) * 2f;
                float ny = Mathf.Abs((y + 0.5f) / TextureSize - 0.5f) * 2f;
                float d = Mathf.Max(nx, ny);

                Color color = clear;
                if (d <= 0.72f) color = fill;
                if (d >= 0.62f && d <= 0.82f) color = border;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, true);
        markerSprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        return markerSprite;
    }
}
