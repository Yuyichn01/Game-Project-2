using UnityEngine;
using System.Collections;

public class TrainMovement : MonoBehaviour
{
    [Header("移动设置")]
    public float speed = 5f;
    public float moveDistance = 20f;
    public float waitTime = 3f;

    public bool moveLeft = false;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        StartCoroutine(MoveLoop());
    }

    IEnumerator MoveLoop()
    {
        while (true)
        {
            // === 向右移动 ===
            float traveled = 0f;
            while (traveled < moveDistance)
            {
                float step = speed * Time.deltaTime;
                if (!moveLeft)
                {
                    transform.Translate(Vector3.right * step);
                }
                else
                {
                    transform.Translate(Vector3.left * step);
                }
                traveled += step;
                yield return null;
            }

            // === 等待 ===
            yield return new WaitForSeconds(waitTime);

            // === 瞬间回到起点 ===
            transform.position = startPosition;

            // 可选：如果翻转了朝向，恢复
            // transform.localScale = new Vector3(1, 1, 1);

            // === 再等待一下再出发 ===
            yield return new WaitForSeconds(waitTime);
        }
    }

    void OnDrawGizmos()
    {
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.right * moveDistance;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(start, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(end, 0.5f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
    }
}