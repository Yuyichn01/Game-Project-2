using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleFadeIn : MonoBehaviour
{
    public float fadeDuration = 2.0f; // Duration of the fade-in effect in seconds

    private Image titleImage;

    private void Start()
    {
        titleImage = GetComponent<Image>(); // Automatically get the Image component attached to this GameObject

        if (titleImage != null)
        {
            // Set the initial alpha to 0
            Color initialColor = titleImage.color;
            initialColor.a = 0f;
            titleImage.color = initialColor;

            StartCoroutine(FadeInImage());
        }
        else
        {
            Debug.LogError("No Image component found on the GameObject.");
        }
    }

    private IEnumerator FadeInImage()
    {
        Color color = titleImage.color;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            titleImage.color = color;
            yield return null;
        }

        // Ensure the final alpha is exactly 1
        color.a = 1f;
        titleImage.color = color;
    }
}
