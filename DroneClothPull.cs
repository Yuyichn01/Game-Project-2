using UnityEngine;

public class DroneClothPull : MonoBehaviour
{
    public RectTransform drone; // 无人机 UI

    public Vector2 startPos = new Vector2(-800, 200); // 起始位置（屏幕外）

    public Vector2 endPos = new Vector2(800, 200); // 结束位置（屏幕外）

    public float flyTime = 2.0f; // 飞行时间（秒）

    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1); // 缓动

    private float timer = 0f;

    private bool flying = false;

    void Start()
    {
        drone.anchoredPosition = startPos;
        flying = true;
    }

    void Update()
    {
        if (!flying) return;
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / flyTime);
        float k = ease.Evaluate(t);
        drone.anchoredPosition = Vector2.Lerp(startPos, endPos, k);

        if (t >= 1f) flying = false; // 飞完停止
    }
}
