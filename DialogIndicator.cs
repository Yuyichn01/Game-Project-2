using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogIndicator : MonoBehaviour
{
    public RectTransform uiElement;

    public float[] targetHeights = { 80f, 120f, 100f, 140f };

    public float transitionDuration = 0.5f;

    // 缓存变量
    private Vector2 cachedSize;

    private int currentIndex;

    private float currentTime;

    private float startHeight;

    private float targetHeight;

    void Start()
    {
        if (uiElement == null) uiElement = GetComponent<RectTransform>();

        cachedSize = uiElement.sizeDelta;
        SetupNextAnimation();
    }

    void Update()
    {
        currentTime += Time.deltaTime;
        float t = Mathf.Clamp01(currentTime / transitionDuration);

        if (t >= 1f)
        {
            // 当前动画完成，设置下一个
            cachedSize.y = targetHeight;
            uiElement.sizeDelta = cachedSize;

            currentIndex = (currentIndex + 1) % targetHeights.Length;
            SetupNextAnimation();
            return;
        }

        // 使用缓动函数
        float easedT = EaseInOutQuad(t);
        float currentHeight = Mathf.Lerp(startHeight, targetHeight, easedT);

        cachedSize.y = currentHeight;
        uiElement.sizeDelta = cachedSize;
    }

    private void SetupNextAnimation()
    {
        currentTime = 0f;
        startHeight = cachedSize.y;
        targetHeight = targetHeights[currentIndex];
    }

    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    }
}
