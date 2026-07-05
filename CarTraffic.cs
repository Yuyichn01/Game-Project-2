using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarTraffic : MonoBehaviour
{
    [Header("Traffic Settings")]
    public List<GameObject> carPrefabs;

    public Transform startPoint;
    public Transform endPoint;
    public List<Transform> waitPoints;

    public int numberOfCars = 5;
    public float speed = 5f;
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 3f;

    [Header("Speed Control")]
    public float decelerationDistance = 3f;
    public float minSpeed = 0.3f;
    public float accelerationRate = 2f;
    public float startAccelerationRate = 1.5f;
    public bool showDebugInfo = true;

    [Header("Cycle Settings")]
    public float runDuration = 120f;
    public float waitDuration = 120f;

    [Header("Phase Offset")]
    [Tooltip("相位偏移（秒），用于错开多个交通系统")]
    public float phaseOffset = 0f; // 0 = 立即开始, 30 = 延迟30秒开始

    private List<GameObject> activeCars = new List<GameObject>();
    private Dictionary<GameObject, int> carWaitPointIndex = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, bool> carIsWaiting = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, float> carCurrentSpeed = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, Transform> carCurrentTarget = new Dictionary<GameObject, Transform>();
    private Dictionary<GameObject, bool> carIsStarting = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, float> carStartTime = new Dictionary<GameObject, float>();
    private bool isWaitingPhase = false;
    private float cycleTimer = 0f;
    private bool isInitialized = false;

    void Start()
    {
        if (waitPoints == null || waitPoints.Count == 0)
        {
            Debug.LogError("Please assign at least one wait point!");
            return;
        }
        // 延迟启动，应用相位偏移
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        // 等待相位偏移时间
        if (phaseOffset > 0)
        {
            if (showDebugInfo)
                Debug.Log($"{gameObject.name} waiting {phaseOffset}s before starting...");
            yield return new WaitForSeconds(phaseOffset);
        }

        isInitialized = true;
        if (showDebugInfo)
            Debug.Log($"{gameObject.name} started!");

        StartCoroutine(TrafficCycle());
    }

    void Update()
    {
        // 如果还未初始化，不更新速度
        if (!isInitialized) return;

        // 每帧更新所有车辆的速度
        foreach (var car in activeCars)
        {
            if (car == null) continue;

            // 如果车辆在等待，保持速度为0
            if (carIsWaiting.ContainsKey(car) && carIsWaiting[car])
            {
                if (carCurrentSpeed.ContainsKey(car))
                {
                    carCurrentSpeed[car] = 0f;
                }
                continue;
            }

            if (!carCurrentTarget.ContainsKey(car) || carCurrentTarget[car] == null)
                continue;

            Transform target = carCurrentTarget[car];
            float distanceToTarget = Vector3.Distance(car.transform.position, target.position);

            // 获取当前速度
            float currentSpeed = carCurrentSpeed.ContainsKey(car) ? carCurrentSpeed[car] : 0f;

            // === 速度控制逻辑 ===
            float newSpeed = currentSpeed;

            // 1. 检查是否正在起步
            if (carIsStarting.ContainsKey(car) && carIsStarting[car])
            {
                float startTime = carStartTime.ContainsKey(car) ? carStartTime[car] : 0f;
                float elapsedTime = Time.time - startTime;

                float progress = Mathf.Clamp01(elapsedTime / 3f);
                float smoothProgress = progress * progress * (3f - 2f * progress);

                newSpeed = Mathf.Lerp(0f, speed, smoothProgress);

                if (showDebugInfo)
                {
                    Debug.Log($"{gameObject.name}: Car {car.name} starting... Progress: {smoothProgress:F2}, Speed: {newSpeed:F2} / {speed}");
                }

                if (progress >= 1f)
                {
                    newSpeed = speed;
                    carIsStarting[car] = false;
                    if (showDebugInfo)
                        Debug.Log($"{gameObject.name}: Car {car.name} finished starting! Speed: {newSpeed:F2}");
                }
            }
            // 2. 检查是否在减速区域
            else if (distanceToTarget < decelerationDistance)
            {
                float speedFactor = Mathf.Clamp01(distanceToTarget / decelerationDistance);
                float targetSpeed = Mathf.Lerp(minSpeed, speed, speedFactor);
                newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * accelerationRate);
            }
            // 3. 正常行驶
            else
            {
                if (currentSpeed < speed)
                {
                    newSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * accelerationRate);
                }
                else
                {
                    newSpeed = speed;
                }
            }

            newSpeed = Mathf.Min(newSpeed, speed);
            newSpeed = Mathf.Max(newSpeed, 0f);

            if (carCurrentSpeed.ContainsKey(car))
            {
                carCurrentSpeed[car] = newSpeed;
            }
        }
    }

    private IEnumerator TrafficCycle()
    {
        while (true)
        {
            // === 运行阶段（不等待） ===
            isWaitingPhase = false;
            cycleTimer = 0f;

            Coroutine spawnCoroutine = StartCoroutine(SpawnCars());

            while (cycleTimer < runDuration)
            {
                cycleTimer += Time.deltaTime;
                yield return null;
            }

            StopCoroutine(spawnCoroutine);

            if (showDebugInfo)
                Debug.Log($"{gameObject.name}: Run phase ended, entering wait phase...");

            // === 等待阶段（车辆到达waitPoint后停车等待） ===
            isWaitingPhase = true;
            cycleTimer = 0f;

            while (cycleTimer < waitDuration)
            {
                cycleTimer += Time.deltaTime;
                yield return null;
            }

            if (showDebugInfo)
                Debug.Log($"{gameObject.name}: Wait phase ended, resuming cars...");

            // === 等待结束，所有车辆继续前进 ===
            isWaitingPhase = false;

            // 唤醒所有在waitPoint等待的车辆
            List<GameObject> carsToResume = new List<GameObject>(activeCars);

            foreach (GameObject car in carsToResume)
            {
                if (car != null && carIsWaiting.ContainsKey(car) && carIsWaiting[car])
                {
                    carIsWaiting[car] = false;

                    if (carCurrentSpeed.ContainsKey(car))
                    {
                        carCurrentSpeed[car] = 0f;
                    }
                    if (carIsStarting.ContainsKey(car))
                    {
                        carIsStarting[car] = true;
                    }
                    if (carStartTime.ContainsKey(car))
                    {
                        carStartTime[car] = Time.time;
                    }

                    if (showDebugInfo)
                        Debug.Log($"{gameObject.name}: Car {car.name} resumed, starting acceleration...");

                    StartCoroutine(ContinueToNextPoint(car));
                }
            }

            // 等待所有车辆到达终点并销毁
            while (activeCars.Count > 0)
            {
                yield return null;
            }

            if (showDebugInfo)
                Debug.Log($"{gameObject.name}: All cars reached destination, restarting cycle...");

            yield return new WaitForSeconds(1f);
        }
    }

    // 继续到下一个目标点（从waitPoint恢复时调用）
    private IEnumerator ContinueToNextPoint(GameObject car)
    {
        if (car == null) yield break;

        int currentIndex = carWaitPointIndex[car];
        Transform targetPoint;

        if (currentIndex < waitPoints.Count)
        {
            targetPoint = waitPoints[currentIndex];
        }
        else
        {
            StartCoroutine(DriveToEndWithStart(car));
            yield break;
        }

        carCurrentTarget[car] = targetPoint;

        while (true)
        {
            if (car == null) yield break;

            float currentSpeed = carCurrentSpeed.ContainsKey(car) ? carCurrentSpeed[car] : speed;
            float distanceToTarget = Vector3.Distance(car.transform.position, targetPoint.position);

            if (distanceToTarget < 0.05f)
            {
                carCurrentSpeed[car] = 0f;
                carCurrentTarget[car] = null;
                carIsStarting[car] = false;

                if (currentIndex < waitPoints.Count)
                {
                    carWaitPointIndex[car] = currentIndex + 1;

                    // 只有在等待阶段才停车
                    if (isWaitingPhase)
                    {
                        carIsWaiting[car] = true;
                        if (showDebugInfo)
                            Debug.Log($"{gameObject.name}: Car {car.name} arrived at WP {currentIndex}, waiting...");
                        yield break;
                    }
                    else
                    {
                        // 非等待阶段，直接继续前进
                        StartCoroutine(ContinueToNextPoint(car));
                        yield break;
                    }
                }
                yield break;
            }

            Vector3 direction = (targetPoint.position - car.transform.position).normalized;

            if (direction != Vector3.zero && currentSpeed > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            float step = currentSpeed * Time.deltaTime;

            if (step >= distanceToTarget)
            {
                car.transform.position = targetPoint.position;
                carCurrentSpeed[car] = 0f;
            }
            else
            {
                car.transform.position += direction * step;
            }

            yield return null;
        }
    }

    // 从waitPoint到终点（带有起步加速）
    private IEnumerator DriveToEndWithStart(GameObject car)
    {
        if (car == null) yield break;

        carWaitPointIndex[car] = waitPoints.Count + 1;
        carIsWaiting[car] = false;
        carCurrentTarget[car] = endPoint;

        if (carIsStarting.ContainsKey(car))
        {
            carIsStarting[car] = true;
        }
        if (carStartTime.ContainsKey(car))
        {
            carStartTime[car] = Time.time;
        }
        if (carCurrentSpeed.ContainsKey(car))
        {
            carCurrentSpeed[car] = 0f;
        }

        if (showDebugInfo)
            Debug.Log($"{gameObject.name}: Car {car.name} heading to end point with start acceleration...");

        while (true)
        {
            if (car == null) yield break;

            float currentSpeed = carCurrentSpeed.ContainsKey(car) ? carCurrentSpeed[car] : speed;
            float distanceToEnd = Vector3.Distance(car.transform.position, endPoint.position);

            if (distanceToEnd < 0.1f)
            {
                activeCars.Remove(car);
                carWaitPointIndex.Remove(car);
                carIsWaiting.Remove(car);
                carCurrentSpeed.Remove(car);
                carCurrentTarget.Remove(car);
                carIsStarting.Remove(car);
                carStartTime.Remove(car);
                Destroy(car);
                yield break;
            }

            Vector3 direction = (endPoint.position - car.transform.position).normalized;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            float step = currentSpeed * Time.deltaTime;

            if (step >= distanceToEnd)
            {
                car.transform.position = endPoint.position;
            }
            else
            {
                car.transform.position += direction * step;
            }

            yield return null;
        }
    }

    private IEnumerator SpawnCars()
    {
        while (true)
        {
            if (isWaitingPhase)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            if (activeCars.Count < numberOfCars)
            {
                SpawnCar();
            }

            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
        }
    }

    private void SpawnCar()
    {
        if (carPrefabs.Count == 0 || startPoint == null || endPoint == null || waitPoints.Count == 0)
        {
            Debug.LogWarning("Missing car prefabs or points.");
            return;
        }

        GameObject selectedCar = carPrefabs[Random.Range(0, carPrefabs.Count)];
        GameObject car = Instantiate(selectedCar, startPoint.position, Quaternion.identity);
        activeCars.Add(car);

        carWaitPointIndex[car] = 0;
        carIsWaiting[car] = false;
        carCurrentSpeed[car] = speed;
        carIsStarting[car] = false;
        carStartTime[car] = Time.time;

        StartCoroutine(MoveToNextPoint(car));
    }

    // 移动到下一个目标点（从起点出发）
    private IEnumerator MoveToNextPoint(GameObject car)
    {
        if (car == null) yield break;

        int currentIndex = carWaitPointIndex[car];
        Transform targetPoint;

        if (currentIndex < waitPoints.Count)
        {
            targetPoint = waitPoints[currentIndex];
        }
        else
        {
            StartCoroutine(DriveToEnd(car));
            yield break;
        }

        carCurrentTarget[car] = targetPoint;

        if (!carIsStarting.ContainsKey(car) || !carIsStarting[car])
        {
            if (carCurrentSpeed.ContainsKey(car))
            {
                carCurrentSpeed[car] = speed;
            }
        }

        while (true)
        {
            if (car == null) yield break;

            float currentSpeed = carCurrentSpeed.ContainsKey(car) ? carCurrentSpeed[car] : speed;
            float distanceToTarget = Vector3.Distance(car.transform.position, targetPoint.position);

            if (distanceToTarget < 0.05f)
            {
                carCurrentSpeed[car] = 0f;
                carCurrentTarget[car] = null;
                carIsStarting[car] = false;

                if (currentIndex < waitPoints.Count)
                {
                    carWaitPointIndex[car] = currentIndex + 1;

                    // 只有在等待阶段才停车
                    if (isWaitingPhase)
                    {
                        carIsWaiting[car] = true;
                        if (showDebugInfo)
                            Debug.Log($"{gameObject.name}: Car {car.name} arrived at WP {currentIndex}, waiting...");
                        yield break;
                    }
                    else
                    {
                        // 非等待阶段，直接继续前进
                        StartCoroutine(MoveToNextPoint(car));
                        yield break;
                    }
                }
                yield break;
            }

            Vector3 direction = (targetPoint.position - car.transform.position).normalized;

            if (direction != Vector3.zero && currentSpeed > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            float step = currentSpeed * Time.deltaTime;

            if (step >= distanceToTarget)
            {
                car.transform.position = targetPoint.position;
                carCurrentSpeed[car] = 0f;
            }
            else
            {
                car.transform.position += direction * step;
            }

            yield return null;
        }
    }

    // 从起点直接到终点（无起步）
    private IEnumerator DriveToEnd(GameObject car)
    {
        if (car == null) yield break;

        carWaitPointIndex[car] = waitPoints.Count + 1;
        carIsWaiting[car] = false;
        carCurrentSpeed[car] = speed;
        carCurrentTarget[car] = endPoint;
        carIsStarting[car] = false;

        while (true)
        {
            if (car == null) yield break;

            float distanceToEnd = Vector3.Distance(car.transform.position, endPoint.position);

            if (distanceToEnd < 0.1f)
            {
                activeCars.Remove(car);
                carWaitPointIndex.Remove(car);
                carIsWaiting.Remove(car);
                carCurrentSpeed.Remove(car);
                carCurrentTarget.Remove(car);
                carIsStarting.Remove(car);
                carStartTime.Remove(car);
                Destroy(car);
                yield break;
            }

            Vector3 direction = (endPoint.position - car.transform.position).normalized;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            float currentSpeed = speed;
            float step = currentSpeed * Time.deltaTime;

            if (step >= distanceToEnd)
            {
                car.transform.position = endPoint.position;
            }
            else
            {
                car.transform.position += direction * step;
            }

            yield return null;
        }
    }

    private void OnDrawGizmos()
    {
        if (startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, 0.5f);
        }

        if (waitPoints != null)
        {
            for (int i = 0; i < waitPoints.Count; i++)
            {
                if (waitPoints[i] != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(waitPoints[i].position, 0.5f);

                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                    Gizmos.DrawWireSphere(waitPoints[i].position, decelerationDistance);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(waitPoints[i].position + Vector3.up * 1f, "WP " + (i + 1));
                    UnityEditor.Handles.Label(waitPoints[i].position + Vector3.up * 0.5f, $"减速区 {decelerationDistance}m");
#endif
                }
            }
        }

        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint.position, 0.5f);
        }

#if UNITY_EDITOR
        if (startPoint != null && waitPoints != null && waitPoints.Count > 0 && endPoint != null)
        {
            Gizmos.color = Color.cyan;
            Vector3[] pathPoints = new Vector3[waitPoints.Count + 2];
            pathPoints[0] = startPoint.position;
            for (int i = 0; i < waitPoints.Count; i++)
            {
                if (waitPoints[i] != null)
                    pathPoints[i + 1] = waitPoints[i].position;
            }
            pathPoints[waitPoints.Count + 1] = endPoint.position;
            
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
            }
        }
#endif
    }
}