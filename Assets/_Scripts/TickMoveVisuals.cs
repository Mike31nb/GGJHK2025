using UnityEngine;

public class TickMoveVisuals : MonoBehaviour
{
    [Header("Trail")]
    public bool enableTrail = true;
    public float trailTime = 0.28f;
    public float trailStartWidth = 0.42f;
    public float trailEndWidth = 0.02f;

    [Header("Particles")]
    public bool enableParticles = true;
    public int burstCount = 8;
    public float particleLifetime = 0.28f;
    public float particleSpeed = 1.2f;

    private TrailRenderer trail;
    private ParticleSystem moveParticles;
    private ParticleSystem.MainModule particleMain;
    private ParticleSystemRenderer particleRenderer;

    void Awake()
    {
        SetupTrail();
        SetupParticles();
    }

    public void Play(MaskType maskType, Vector3 direction)
    {
        Color color = GetColor(maskType);

        if (trail != null)
        {
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.Clear();
        }

        if (moveParticles != null)
        {
            particleMain.startColor = color;
            moveParticles.Emit(burstCount);
        }
    }

    public static Color GetColor(MaskType maskType)
    {
        switch (maskType)
        {
            case MaskType.Turtle:
                return new Color(0.18f, 0.95f, 0.35f, 0.85f);
            case MaskType.Ox:
                return new Color(1f, 0.45f, 0.12f, 0.85f);
            case MaskType.Hawk:
                return new Color(0.25f, 0.75f, 1f, 0.85f);
            case MaskType.Fox:
                return new Color(1f, 0.28f, 0.55f, 0.85f);
            case MaskType.Dragon:
                return new Color(0.95f, 0.05f, 0.05f, 0.9f);
            default:
                return new Color(1f, 1f, 1f, 0.65f);
        }
    }

    void SetupTrail()
    {
        if (!enableTrail) return;

        trail = GetComponent<TrailRenderer>();
        if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();

        trail.time = trailTime;
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;
        trail.autodestruct = false;
        trail.emitting = true;
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;
        trail.sortingOrder = 2;

        Material material = new Material(Shader.Find("Sprites/Default"));
        material.name = "Generated Move Trail Material";
        trail.material = material;
        trail.Clear();
    }

    void SetupParticles()
    {
        if (!enableParticles) return;

        moveParticles = GetComponent<ParticleSystem>();
        if (moveParticles == null) moveParticles = gameObject.AddComponent<ParticleSystem>();

        particleMain = moveParticles.main;
        particleMain.loop = false;
        particleMain.playOnAwake = false;
        particleMain.startLifetime = particleLifetime;
        particleMain.startSpeed = particleSpeed;
        particleMain.startSize = 0.16f;
        particleMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = moveParticles.emission;
        emission.enabled = false;

        var shape = moveParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;

        var velocity = moveParticles.velocityOverLifetime;
        velocity.enabled = false;

        particleRenderer = moveParticles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingOrder = 3;
        if (particleRenderer.sharedMaterial == null)
        {
            particleRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }
    }
}
