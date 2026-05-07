using UnityEngine;

public class TickRoarWave : MonoBehaviour
{
    private const int TextureSize = 64;
    private static Sprite ringSprite;

    private SpriteRenderer spriteRenderer;
    private Color waveColor;
    private float duration = 1.25f;
    private float maxScale = 45f;
    private float timer;

    public static void Spawn(Vector3 worldPos, Color color)
    {
        GameObject waveObject = new GameObject("Dragon_Roar_Wave");
        TickRoarWave wave = waveObject.AddComponent<TickRoarWave>();
        wave.Initialize(worldPos, color);
    }

    void Initialize(Vector3 worldPos, Color color)
    {
        worldPos.z = -0.2f;
        transform.position = worldPos;
        transform.localScale = Vector3.one * 0.6f;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetRingSprite();
        spriteRenderer.sortingOrder = 7;

        waveColor = color;
        waveColor.a = 0.85f;
        spriteRenderer.color = waveColor;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float eased = t * t * (3f - 2f * t);

        transform.localScale = Vector3.one * Mathf.Lerp(0.6f, maxScale, eased);

        Color color = waveColor;
        color.a = Mathf.Lerp(0.85f, 0f, t);
        spriteRenderer.color = color;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    static Sprite GetRingSprite()
    {
        if (ringSprite != null) return ringSprite;

        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.name = "Generated Dragon Roar Ring";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color ring = new Color(1f, 1f, 1f, 1f);

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float nx = (x + 0.5f) / TextureSize - 0.5f;
                float ny = (y + 0.5f) / TextureSize - 0.5f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny) * 2f;
                float alpha = Mathf.InverseLerp(0.78f, 0.62f, Mathf.Abs(distance - 0.72f));
                Color color = alpha > 0f ? new Color(ring.r, ring.g, ring.b, alpha) : clear;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, true);
        ringSprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        return ringSprite;
    }
}
