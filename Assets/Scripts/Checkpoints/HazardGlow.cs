using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Renders a soft, pulsing 9-sliced orange glow around the entire hazard perimeter
/// and drives an optional <see cref="Light2D"/>.
/// Automatically compensates for non-uniform parent scaling so long or tall hazards
/// glow evenly along all edges.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HazardGlow : MonoBehaviour
{
    private const int TextureSize = 64;
    private const int BorderPixels = 20;

    [Header("Glow Appearance")]
    [Tooltip("HDR orange glow color. Values > 1 trigger URP Bloom.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color glowColor = new Color(2.5f, 0.9f, 0.1f, 0.9f);

    [Tooltip("Extra world-space glow padding added around the entire hazard.")]
    [SerializeField] private float worldGlowPadding = 0.45f;

    [SerializeField] private float pulsePaddingAmount = 0.15f;
    [SerializeField] private float pulseSpeed = 3.2f;

    [Header("2D Point Light (Optional)")]
    [SerializeField] private Light2D pointLight;
    [SerializeField] private Color lightColor = new Color(1f, 0.45f, 0.05f, 1f);
    [SerializeField] private float baseLightIntensity = 2.5f;
    [SerializeField] private float pulseLightAmount = 0.6f;

    private static Sprite sharedBoxGlowSprite;
    private static Material sharedUnlitMaterial;

    private SpriteRenderer glowRenderer;
    private float phaseOffset;

    private void Awake()
    {
        glowRenderer = GetComponent<SpriteRenderer>();
        if (pointLight == null)
        {
            pointLight = GetComponent<Light2D>();
        }

        glowRenderer.sprite = GetOrCreateBoxGlowSprite();
        glowRenderer.drawMode = SpriteDrawMode.Sliced;

        Material unlitMat = GetOrCreateUnlitMaterial();
        if (unlitMat != null)
        {
            glowRenderer.sharedMaterial = unlitMat;
        }

        glowRenderer.color = glowColor;
        transform.localScale = Vector3.one;
        UpdateGlowSize(worldGlowPadding);

        if (pointLight != null)
        {
            pointLight.lightType = Light2D.LightType.Point;
            pointLight.color = lightColor;
            pointLight.intensity = baseLightIntensity;
        }

        phaseOffset = (transform.position.x + transform.position.y) * 0.75f;
    }

    private void Update()
    {
        float pulse = Mathf.Sin((Time.time * pulseSpeed) + phaseOffset) * 0.5f + 0.5f;

        UpdateGlowSize(worldGlowPadding + (pulse * pulsePaddingAmount));

        if (glowRenderer != null)
        {
            Color c = glowColor;
            c.a = glowColor.a * Mathf.Lerp(0.75f, 1f, pulse);
            glowRenderer.color = c;
        }

        if (pointLight != null)
        {
            pointLight.intensity = baseLightIntensity + (pulse * pulseLightAmount);
        }
    }

    private void UpdateGlowSize(float totalWorldPadding)
    {
        if (glowRenderer == null)
        {
            return;
        }

        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float safeX = Mathf.Max(0.01f, Mathf.Abs(parentScale.x));
        float safeY = Mathf.Max(0.01f, Mathf.Abs(parentScale.y));

        glowRenderer.size = new Vector2(
            1f + (totalWorldPadding / safeX),
            1f + (totalWorldPadding / safeY)
        );
    }

    private static Sprite GetOrCreateBoxGlowSprite()
    {
        if (sharedBoxGlowSprite != null)
        {
            return sharedBoxGlowSprite;
        }

        Texture2D tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = "HazardGlow_BoxTex",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[TextureSize * TextureSize];

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                // Normalized distance into the soft border [0 = outer edge, 1 = inner core]
                float dx = Mathf.Min(x, (TextureSize - 1) - x) / (float)BorderPixels;
                float dy = Mathf.Min(y, (TextureSize - 1) - y) / (float)BorderPixels;

                float nx = Mathf.Clamp01(1f - dx);
                float ny = Mathf.Clamp01(1f - dy);
                float edgeDist = Mathf.Sqrt((nx * nx) + (ny * ny));

                float alpha = Mathf.Clamp01(1f - edgeDist);
                alpha = alpha * alpha;
                pixels[(y * TextureSize) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);

        sharedBoxGlowSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            TextureSize,
            0,
            SpriteMeshType.FullRect,
            new Vector4(BorderPixels, BorderPixels, BorderPixels, BorderPixels)
        );
        sharedBoxGlowSprite.name = "HazardGlow_BoxSprite";

        return sharedBoxGlowSprite;
    }

    private static Material GetOrCreateUnlitMaterial()
    {
        if (sharedUnlitMaterial != null)
        {
            return sharedUnlitMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader != null)
        {
            sharedUnlitMaterial = new Material(shader)
            {
                name = "HazardGlow_UnlitMaterial"
            };
        }

        return sharedUnlitMaterial;
    }
}
