using UnityEngine;

public class BuildingRedLight : MonoBehaviour
{
    public float fadeSpeed = 2f; // Speed of the pulse effect

    public float minIntensity = 0.2f; // Minimum opacity

    public float maxIntensity = 1f; // Maximum opacity

    private SpriteRenderer spriteRenderer;

    private float timer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug
                .LogError("No SpriteRenderer found! Attach this script to a GameObject with a SpriteRenderer.");
            enabled = false;
        }
    }

    void Update()
    {
        timer += Time.deltaTime * fadeSpeed;

        // Use Mathf.Floor to create a sharp blink effect
        float alpha =
            (Mathf.Floor(timer) % 2 == 0) ? maxIntensity : minIntensity;

        // Apply the new color
        Color newColor = spriteRenderer.color;
        newColor.a = alpha;
        spriteRenderer.color = newColor;
    }
}
