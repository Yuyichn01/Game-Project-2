using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AppOpenAnimation : MonoBehaviour
{
    public float animationSpeed = 0.3f; // Speed of scaling effect

    public Vector3 startScale = new Vector3(0.5f, 0.5f, 1f); // Scale at the start (small)

    public Vector3 targetScale = new Vector3(1f, 1f, 1f); // Normal scale

    private RectTransform rectTransform; // UI Image or Panel RectTransform

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(1, 0); // Set pivot to bottom-right corner
        rectTransform.localScale = startScale; // Start small
    }

    public void OpenApp()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleApp(targetScale));
    }

    public void CloseApp()
    {
        StopAllCoroutines();
        StartCoroutine(ScaleApp(startScale));
    }

    IEnumerator ScaleApp(Vector3 target)
    {
        float elapsedTime = 0f;
        Vector3 initialScale = rectTransform.localScale;

        while (elapsedTime < animationSpeed)
        {
            rectTransform.localScale =
                Vector3
                    .Lerp(initialScale, target, elapsedTime / animationSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.localScale = target;
    }
}
