using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonShake : MonoBehaviour
{
    [Tooltip("振幅（像素）")]
    public float amplitude = 10f;

    [Tooltip("频率（赫兹：每秒抖动几次）")]
    public float frequency = 2f;

    [Tooltip("是否使用不受Time.timeScale影响的时间（UI常用）")]
    public bool useUnscaledTime = true;

    [Tooltip("多个按钮时随机相位，避免整齐同步")]
    public bool randomizePhase = true;

    private RectTransform rect;

    private Vector2 basePos;

    private float phase;

    private const float TwoPi = 6.283185307179586f;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        basePos = rect.anchoredPosition;
        if (randomizePhase) phase = Random.value * TwoPi; // 仅一次，无GC
    }

    void OnEnable()
    {
        // 若布局系统改过位置，启用时再取一次基准
        basePos = rect.anchoredPosition;
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float y = amplitude * Mathf.Sin(phase + t * frequency * TwoPi);

        // 避免临时向量分配：仅值类型拷贝
        Vector2 p = basePos;
        p.y += y;
        rect.anchoredPosition = p;
    }

    void OnDisable()
    {
        // 关闭效果时还原到基准位置
        rect.anchoredPosition = basePos;
    }

    // 若你在运行时手动移动了按钮，可调用此方法重设基准
    public void Recenter() => basePos = rect.anchoredPosition;
}
