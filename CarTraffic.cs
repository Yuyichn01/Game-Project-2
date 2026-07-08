using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 交通灯状态枚举
// ============================================================
public enum TrafficLightState
{
    Green,
    Yellow,
    Red
}

// ============================================================
// 交通灯控制类 - 管理单个交通灯的状态和显示
// ============================================================
[System.Serializable]
public class TrafficLightController
{
    [Header("材质槽位")]
    public Material greenMaterial;
    public Material yellowMaterial;
    public Material redMaterial;
    public Material offMaterial;

    [Header("Plane设置")]
    public GameObject lightPlane;
    public Vector3 planePosition = new Vector3(0, 2f, 0);
    public Vector3 planeScale = new Vector3(1f, 1f, 0.5f);
    public Vector3 planeRotation = new Vector3(-90f, 0, 0);

    [Header("时间设置")]
    public float interval = 20f;
    public float flashSpeed = 0.5f;
    public bool enableFlash = true;

    [Header("初始状态")]
    public bool startWithGreen = true;

    [Header("关联的等待点")]
    public int associatedWaitPointIndex = 0;

    private Renderer planeRenderer;
    private GameObject planeObject;
    private TrafficLightState currentState = TrafficLightState.Green;
    private bool isInitialized = false;
    private Transform waitPointTransform;
    private bool isCycling = false;
    private Material originalMaterial;

    public TrafficLightState CurrentState => currentState;
    public int AssociatedWaitPointIndex => associatedWaitPointIndex;
    public bool IsInitialized => isInitialized;
    public GameObject PlaneObject => planeObject;

    public void Initialize(Transform waitPoint)
    {
        waitPointTransform = waitPoint;

        if (waitPoint == null)
        {
            Debug.LogError("等待点Transform为空！");
            return;
        }

        if (lightPlane == null)
        {
            Transform existingPlane = waitPoint.Find("TrafficLightPlane");
            if (existingPlane != null)
            {
                lightPlane = existingPlane.gameObject;
                Debug.Log($"在等待点下找到交通灯Plane: {lightPlane.name}");
            }
            else
            {
                Debug.LogError($"未指定交通灯Plane，且在等待点 {associatedWaitPointIndex} 下未找到 TrafficLightPlane！");
                return;
            }
        }

        planeObject = lightPlane;
        planeRenderer = planeObject.GetComponent<Renderer>();
        if (planeRenderer == null)
        {
            Debug.LogError($"Plane {planeObject.name} 没有Renderer组件！");
            return;
        }

        if (planeRenderer.material != null)
        {
            originalMaterial = planeRenderer.material;
        }

        ValidateMaterials();

        TrafficLightState startState = startWithGreen ? TrafficLightState.Green : TrafficLightState.Red;
        currentState = startState;
        SetLightMaterial(startState);

        isInitialized = true;

        Debug.Log($"交通灯初始化完成！状态: {startState}, 关联到等待点 {associatedWaitPointIndex}, Plane: {planeObject.name}");
    }

    private void ValidateMaterials()
    {
        if (greenMaterial == null)
        {
            greenMaterial = CreateDefaultMaterial(Color.green);
            Debug.LogWarning($"绿灯材质为空，已创建默认材质");
        }
        if (yellowMaterial == null)
        {
            yellowMaterial = CreateDefaultMaterial(Color.yellow);
            Debug.LogWarning($"黄灯材质为空，已创建默认材质");
        }
        if (redMaterial == null)
        {
            redMaterial = CreateDefaultMaterial(Color.red);
            Debug.LogWarning($"红灯材质为空，已创建默认材质");
        }
        if (offMaterial == null)
        {
            offMaterial = CreateDefaultMaterial(Color.gray);
            Debug.LogWarning($"关闭材质为空，已创建默认材质");
        }
    }

    private Material CreateDefaultMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }

    public IEnumerator TrafficLightCycle()
    {
        if (!isInitialized)
        {
            Debug.LogError("交通灯未初始化，无法开始循环！");
            yield break;
        }

        if (isCycling)
        {
            Debug.LogWarning("交通灯已经在循环中");
            yield break;
        }

        isCycling = true;

        if (!startWithGreen)
        {
            currentState = TrafficLightState.Red;
            SetLightMaterial(TrafficLightState.Red);
            Debug.Log($"交通灯循环开始 - 初始状态: 红灯");
            yield return new WaitForSeconds(1f);
        }

        while (true)
        {
            currentState = TrafficLightState.Green;
            yield return ShowLightWithFlash(TrafficLightState.Green, interval);

            currentState = TrafficLightState.Yellow;
            yield return ShowLightWithFlash(TrafficLightState.Yellow, 3f);

            currentState = TrafficLightState.Red;
            yield return ShowLightWithFlash(TrafficLightState.Red, interval);

            currentState = TrafficLightState.Yellow;
            yield return ShowLightWithFlash(TrafficLightState.Yellow, 3f);
        }
    }

    private IEnumerator ShowLightWithFlash(TrafficLightState state, float duration)
    {
        float timer = 0f;
        bool isOn = true;

        while (timer < duration)
        {
            if (enableFlash)
            {
                if (isOn)
                    SetLightMaterial(state);
                else
                    SetLightMaterial(null);

                yield return new WaitForSeconds(flashSpeed);
                isOn = !isOn;
                timer += flashSpeed;
            }
            else
            {
                SetLightMaterial(state);
                yield return new WaitForSeconds(duration - timer);
                timer = duration;
            }
        }
        SetLightMaterial(state);
    }

    private void SetLightMaterial(TrafficLightState? state)
    {
        if (planeRenderer == null) return;

        Material targetMaterial = null;

        switch (state)
        {
            case TrafficLightState.Green:
                targetMaterial = greenMaterial;
                break;
            case TrafficLightState.Yellow:
                targetMaterial = yellowMaterial;
                break;
            case TrafficLightState.Red:
                targetMaterial = redMaterial;
                break;
            case null:
                targetMaterial = offMaterial;
                break;
        }

        if (targetMaterial != null)
        {
            planeRenderer.material = targetMaterial;
        }
    }

    public void ForceState(TrafficLightState state)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("交通灯未初始化，无法强制设置状态");
            return;
        }

        currentState = state;
        SetLightMaterial(state);
        Debug.Log($"强制设置交通灯状态: {state}");
    }

    public void RestoreOriginalMaterial()
    {
        if (planeRenderer != null && originalMaterial != null)
        {
            planeRenderer.material = originalMaterial;
        }
    }

    public Color GetCurrentColor()
    {
        switch (currentState)
        {
            case TrafficLightState.Green:
                return Color.green;
            case TrafficLightState.Yellow:
                return Color.yellow;
            case TrafficLightState.Red:
                return Color.red;
            default:
                return Color.white;
        }
    }

    public void StopCycle()
    {
        isCycling = false;
    }
}

// ============================================================
// 主交通系统类 - 管理车辆和交通灯
// ============================================================
public class CarTraffic : MonoBehaviour
{
    [Header("Traffic Settings")]
    public List<GameObject> carPrefabs;
    public Transform startPoint;
    public Transform endPoint;
    public List<Transform> waitPoints;

    [Header("交通灯设置")]
    public List<TrafficLightController> trafficLights = new List<TrafficLightController>();
    public bool useTrafficLights = true;

    [Header("车辆设置")]
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
    public float phaseOffset = 0f;

    [Header("Car Following Settings")]
    public float followingDistance = 5f;
    public float stopDistance = 2f;
    public float detectionAngle = 30f;

    [Header("转向平滑设置")]
    [Tooltip("转向平滑速度，值越大转向越快")]
    public float rotationSmoothSpeed = 5f;

    private List<GameObject> activeCars = new List<GameObject>();
    private Dictionary<GameObject, int> carWaitPointIndex = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, bool> carIsWaiting = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, float> carCurrentSpeed = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, Transform> carCurrentTarget = new Dictionary<GameObject, Transform>();
    private Dictionary<GameObject, bool> carIsStarting = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, float> carStartTime = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, TrafficLightState> carLastLightState = new Dictionary<GameObject, TrafficLightState>();

    private bool isWaitingPhase = false;
    private float cycleTimer = 0f;
    private bool isInitialized = false;

    private GlobalTrafficManager trafficManager;
    private Coroutine lightManagerCoroutine;
    private List<Coroutine> lightCycleCoroutines = new List<Coroutine>();

    void Start()
    {
        if (waitPoints == null || waitPoints.Count == 0)
        {
            Debug.LogError("请至少指定一个等待点！");
            return;
        }

        trafficManager = GlobalTrafficManager.Instance;
        if (trafficManager == null)
        {
            Debug.LogError("场景中未找到 GlobalTrafficManager！");
        }

        InitializeTrafficLights();
        StartCoroutine(DelayedStart());
    }

    private void InitializeTrafficLights()
    {
        Debug.Log($"开始初始化 {trafficLights.Count} 个交通灯...");

        for (int i = 0; i < trafficLights.Count; i++)
        {
            var light = trafficLights[i];
            if (light == null)
            {
                Debug.LogWarning($"交通灯 {i} 为空");
                continue;
            }

            if (light.associatedWaitPointIndex >= waitPoints.Count)
            {
                Debug.LogWarning($"交通灯 {i} 关联的等待点索引超出范围");
                continue;
            }

            Transform waitPoint = waitPoints[light.associatedWaitPointIndex];
            if (waitPoint == null)
            {
                Debug.LogWarning($"等待点 {light.associatedWaitPointIndex} 为空");
                continue;
            }

            light.Initialize(waitPoint);

            if (showDebugInfo)
                Debug.Log($"初始化交通灯 {i} 关联到等待点 {light.associatedWaitPointIndex}");
        }
    }

    private IEnumerator DelayedStart()
    {
        if (phaseOffset > 0)
        {
            if (showDebugInfo)
                Debug.Log($"{gameObject.name} 延迟 {phaseOffset}s 启动...");
            yield return new WaitForSeconds(phaseOffset);
        }

        isInitialized = true;
        if (showDebugInfo)
            Debug.Log($"{gameObject.name} 启动！");

        if (useTrafficLights && trafficLights.Count > 0)
        {
            lightManagerCoroutine = StartCoroutine(TrafficLightManager());
        }

        StartCoroutine(TrafficCycle());
    }

    private IEnumerator TrafficLightManager()
    {
        Debug.Log("启动交通灯管理器...");

        foreach (var light in trafficLights)
        {
            if (light.IsInitialized)
            {
                lightCycleCoroutines.Add(StartCoroutine(light.TrafficLightCycle()));
            }
        }

        while (true)
        {
            foreach (var car in activeCars)
            {
                if (car == null) continue;
                UpdateCarLightState(car);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void UpdateCarLightState(GameObject car)
    {
        if (car == null || !carWaitPointIndex.ContainsKey(car)) return;

        int currentIndex = carWaitPointIndex[car];
        if (currentIndex >= waitPoints.Count) return;

        foreach (var light in trafficLights)
        {
            if (light.AssociatedWaitPointIndex == currentIndex && light.IsInitialized)
            {
                if (!carLastLightState.ContainsKey(car))
                    carLastLightState[car] = light.CurrentState;
                else
                    carLastLightState[car] = light.CurrentState;
                break;
            }
        }
    }

    private bool CanPassLight(GameObject car)
    {
        if (!useTrafficLights) return true;

        if (!carWaitPointIndex.ContainsKey(car)) return true;
        int currentIndex = carWaitPointIndex[car];
        if (currentIndex >= waitPoints.Count) return true;

        foreach (var light in trafficLights)
        {
            if (light.AssociatedWaitPointIndex == currentIndex && light.IsInitialized)
            {
                if (light.CurrentState == TrafficLightState.Red ||
                    light.CurrentState == TrafficLightState.Yellow)
                {
                    return false;
                }
                return true;
            }
        }
        return true;
    }

    void Update()
    {
        if (!isInitialized || trafficManager == null) return;

        foreach (var car in activeCars)
        {
            if (car == null) continue;

            // 如果车辆在等待，保持速度为0
            if (carIsWaiting.ContainsKey(car) && carIsWaiting[car])
            {
                if (carCurrentSpeed.ContainsKey(car))
                    carCurrentSpeed[car] = 0f;
                continue;
            }

            if (!carCurrentTarget.ContainsKey(car) || carCurrentTarget[car] == null)
                continue;

            Transform target = carCurrentTarget[car];
            float distanceToTarget = Vector3.Distance(car.transform.position, target.position);
            float currentSpeed = carCurrentSpeed.ContainsKey(car) ? carCurrentSpeed[car] : 0f;

            // 检查交通灯
            bool canPassLight = CanPassLight(car);
            GameObject frontCar = trafficManager.DetectFrontCar(car, followingDistance, detectionAngle);

            float newSpeed = currentSpeed;

            // 1. 检查是否正在起步
            if (carIsStarting.ContainsKey(car) && carIsStarting[car])
            {
                float startTime = carStartTime.ContainsKey(car) ? carStartTime[car] : 0f;
                float elapsedTime = Time.time - startTime;
                float progress = Mathf.Clamp01(elapsedTime / 3f);
                float smoothProgress = progress * progress * (3f - 2f * progress);
                newSpeed = Mathf.Lerp(0f, speed, smoothProgress);

                if (progress >= 1f)
                {
                    newSpeed = speed;
                    carIsStarting[car] = false;
                }
            }
            // 2. 检查前方是否有车（优先级最高）
            else if (frontCar != null)
            {
                float distanceToFront = Vector3.Distance(car.transform.position, frontCar.transform.position);

                if (distanceToFront <= stopDistance)
                {
                    newSpeed = 0f;
                }
                else if (distanceToFront <= followingDistance)
                {
                    float speedFactor = Mathf.Clamp01((distanceToFront - stopDistance) / (followingDistance - stopDistance));
                    float targetSpeed = Mathf.Lerp(0f, speed, speedFactor);
                    newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * accelerationRate * 1.5f);
                }
                else
                {
                    newSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * accelerationRate);
                }
            }
            // 3. 检查是否需要停车（到达等待点前开始减速）
            else
            {

                if (isWaitingPhase && !canPassLight)
                {// 在减速距离内开始平滑减速
                    if (distanceToTarget < decelerationDistance)
                    {
                        // 计算速度因子：距离越近速度越慢
                        float speedFactor = Mathf.Clamp01(distanceToTarget / decelerationDistance);
                        // 使用平滑曲线，让减速更自然
                        float smoothFactor = speedFactor * speedFactor * (3f - 2f * speedFactor);
                        float targetSpeed = Mathf.Lerp(0f, speed, smoothFactor);

                        // 使用平滑插值，但降低速度因子
                        float lerpSpeed = Mathf.Clamp01(Time.deltaTime * accelerationRate * 0.5f);
                        newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, lerpSpeed);


                        if (showDebugInfo && Time.frameCount % 30 == 0)
                        {
                            Debug.Log($"{car.name} 减速中 - 距离: {distanceToTarget:F2}, 速度: {newSpeed:F2}, 目标速度: {targetSpeed:F2}");
                        }
                    }
                    else
                    {
                        // 还没到减速区，保持速度
                        newSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * accelerationRate);
                    }
                }
                else
                {
                    // 不需要停车，正常行驶
                    newSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * accelerationRate);
                }
            }

            newSpeed = Mathf.Clamp(newSpeed, 0f, speed);

            if (carCurrentSpeed.ContainsKey(car))
                carCurrentSpeed[car] = newSpeed;
        }
    }
    private IEnumerator TrafficCycle()
    {
        while (true)
        {
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
                Debug.Log($"{gameObject.name}: 运行阶段结束，进入等待阶段...");

            isWaitingPhase = true;
            cycleTimer = 0f;

            while (cycleTimer < waitDuration)
            {
                cycleTimer += Time.deltaTime;
                yield return null;
            }

            if (showDebugInfo)
                Debug.Log($"{gameObject.name}: 等待阶段结束，恢复车辆...");

            isWaitingPhase = false;

            List<GameObject> carsToResume = new List<GameObject>(activeCars);
            foreach (GameObject car in carsToResume)
            {
                if (car != null && carIsWaiting.ContainsKey(car) && carIsWaiting[car])
                {
                    carIsWaiting[car] = false;
                    carCurrentSpeed[car] = 0f;
                    carIsStarting[car] = true;
                    carStartTime[car] = Time.time;

                    if (showDebugInfo)
                        Debug.Log($"{car.name} 恢复行驶，起步加速...");

                    StartCoroutine(ContinueToNextPoint(car));
                }
            }

            while (activeCars.Count > 0)
            {
                yield return null;
            }

            if (showDebugInfo)
                Debug.Log($"{gameObject.name}: 所有车辆到达终点，重新开始循环");

            yield return new WaitForSeconds(1f);
        }
    }

    // ============================================================
    // 修复后的车辆移动方法 - 平滑转向
    // ============================================================

    private IEnumerator ContinueToNextPoint(GameObject car)
    {
        if (car == null || isWaitingPhase || !CanPassLight(car)) yield break;

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

        // 预转向：在开始移动前先转向目标方向
        Vector3 initialDirection = (targetPoint.position - car.transform.position).normalized;
        if (initialDirection != Vector3.zero && Vector3.Angle(car.transform.forward, initialDirection) > 5f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(initialDirection);
            car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, 0.5f);
        }

        while (true)
        {
            if (car == null || isWaitingPhase || !CanPassLight(car)) yield break;

            float currentSpeed = carCurrentSpeed.ContainsKey(car) ? carCurrentSpeed[car] : speed;
            float distanceToTarget = Vector3.Distance(car.transform.position, targetPoint.position);

            // 到达目标点
            if (distanceToTarget < 0.05f)
            {
                carCurrentSpeed[car] = 0f;
                carCurrentTarget[car] = null;
                carIsStarting[car] = false;

                if (currentIndex < waitPoints.Count)
                {
                    carWaitPointIndex[car] = currentIndex + 1;

                    if (isWaitingPhase)
                    {
                        carIsWaiting[car] = true;
                        yield break;
                    }
                    else
                    {
                        StartCoroutine(ContinueToNextPoint(car));
                        yield break;
                    }
                }
                yield break;
            }

            // 计算方向
            Vector3 direction = (targetPoint.position - car.transform.position).normalized;

            // 平滑旋转 - 使用动态旋转速度
            if (direction != Vector3.zero && currentSpeed > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // 动态旋转速度：速度越快，转向越快，但不会过猛
                float speedFactor = Mathf.Clamp01(currentSpeed / speed);
                float dynamicRotationSpeed = Mathf.Lerp(3f, 8f, speedFactor);

                car.transform.rotation = Quaternion.Slerp(
                    car.transform.rotation,
                    targetRotation,
                    Time.deltaTime * dynamicRotationSpeed
                );
            }

            // 移动
            float step = currentSpeed * Time.deltaTime;
            if (step >= distanceToTarget)
            {
                car.transform.position = targetPoint.position;
            }
            else
            {
                car.transform.position += direction * step;
            }

            yield return null;
        }
    }

    private IEnumerator ContinueToNextPoint_Original(GameObject car)
    {
        // 保留原方法作为备份
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

                    if (isWaitingPhase)
                    {
                        carIsWaiting[car] = true;
                        yield break;
                    }
                    else
                    {
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
                car.transform.rotation = Quaternion.Slerp(
                    car.transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSmoothSpeed
                );
            }

            float step = currentSpeed * Time.deltaTime;
            if (step >= distanceToTarget)
            {
                car.transform.position = targetPoint.position;
            }
            else
            {
                car.transform.position += direction * step;
            }

            yield return null;
        }
    }

    private IEnumerator DriveToEndWithStart(GameObject car)
    {
        if (car == null) yield break;

        carWaitPointIndex[car] = waitPoints.Count + 1;
        carIsWaiting[car] = false;
        carCurrentTarget[car] = endPoint;
        carIsStarting[car] = true;
        carStartTime[car] = Time.time;
        carCurrentSpeed[car] = 0f;

        // 预转向终点方向
        Vector3 initialDirection = (endPoint.position - car.transform.position).normalized;
        if (initialDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(initialDirection);
            car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, 0.3f);
        }

        while (true)
        {
            if (car == null) yield break;

            float currentSpeed = carCurrentSpeed.ContainsKey(car) ? carCurrentSpeed[car] : speed;
            float distanceToEnd = Vector3.Distance(car.transform.position, endPoint.position);

            if (distanceToEnd < 0.1f)
            {
                if (trafficManager != null)
                    trafficManager.UnregisterVehicle(car);

                CleanupCar(car);
                yield break;
            }

            Vector3 direction = (endPoint.position - car.transform.position).normalized;

            if (direction != Vector3.zero && currentSpeed > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                float speedFactor = Mathf.Clamp01(currentSpeed / speed);
                float dynamicRotationSpeed = Mathf.Lerp(3f, 8f, speedFactor);

                car.transform.rotation = Quaternion.Slerp(
                    car.transform.rotation,
                    targetRotation,
                    Time.deltaTime * dynamicRotationSpeed
                );
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
            Debug.LogWarning("缺少车辆预制体或路径点");
            return;
        }

        GameObject selectedCar = carPrefabs[Random.Range(0, carPrefabs.Count)];
        GameObject car = Instantiate(selectedCar, startPoint.position, Quaternion.identity);
        activeCars.Add(car);

        if (trafficManager != null)
            trafficManager.RegisterVehicle(car);

        carWaitPointIndex[car] = 0;
        carIsWaiting[car] = false;
        carCurrentSpeed[car] = speed;
        carIsStarting[car] = false;
        carStartTime[car] = Time.time;

        StartCoroutine(MoveToNextPoint(car));
    }

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
        carCurrentSpeed[car] = speed;

        // 预转向目标方向
        Vector3 initialDirection = (targetPoint.position - car.transform.position).normalized;
        if (initialDirection != Vector3.zero && Vector3.Angle(car.transform.forward, initialDirection) > 5f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(initialDirection);
            car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, 0.5f);
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

                    if (isWaitingPhase)
                    {
                        carIsWaiting[car] = true;
                        yield break;
                    }
                    else
                    {
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
                float speedFactor = Mathf.Clamp01(currentSpeed / speed);
                float dynamicRotationSpeed = Mathf.Lerp(3f, 8f, speedFactor);

                car.transform.rotation = Quaternion.Slerp(
                    car.transform.rotation,
                    targetRotation,
                    Time.deltaTime * dynamicRotationSpeed
                );
            }

            float step = currentSpeed * Time.deltaTime;
            if (step >= distanceToTarget)
            {
                car.transform.position = targetPoint.position;
            }
            else
            {
                car.transform.position += direction * step;
            }

            yield return null;
        }
    }

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
                if (trafficManager != null)
                    trafficManager.UnregisterVehicle(car);

                CleanupCar(car);
                yield break;
            }

            Vector3 direction = (endPoint.position - car.transform.position).normalized;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                car.transform.rotation = Quaternion.Slerp(
                    car.transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationSmoothSpeed
                );
            }

            float step = speed * Time.deltaTime;
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

    private void CleanupCar(GameObject car)
    {
        activeCars.Remove(car);
        carWaitPointIndex.Remove(car);
        carIsWaiting.Remove(car);
        carCurrentSpeed.Remove(car);
        carCurrentTarget.Remove(car);
        carIsStarting.Remove(car);
        carStartTime.Remove(car);
        carLastLightState.Remove(car);
        Destroy(car);
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
                    bool hasLight = false;
                    TrafficLightController associatedLight = null;
                    foreach (var light in trafficLights)
                    {
                        if (light.associatedWaitPointIndex == i && light.IsInitialized)
                        {
                            hasLight = true;
                            associatedLight = light;
                            break;
                        }
                    }

                    Gizmos.color = hasLight ? Color.blue : Color.yellow;
                    Gizmos.DrawWireSphere(waitPoints[i].position, 0.5f);

                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                    Gizmos.DrawWireSphere(waitPoints[i].position, decelerationDistance);

#if UNITY_EDITOR
                    string label = "WP " + (i + 1);
                    if (hasLight && associatedLight != null)
                    {
                        label += $" 🚦";
                        UnityEditor.Handles.Label(waitPoints[i].position + Vector3.up * 1.5f,
                            associatedLight.CurrentState.ToString(),
                            new GUIStyle()
                            {
                                normal = new GUIStyleState() { textColor = associatedLight.GetCurrentColor() },
                                fontSize = 14,
                                fontStyle = FontStyle.Bold
                            });
                    }
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

    private void OnDestroy()
    {
        if (lightManagerCoroutine != null)
            StopCoroutine(lightManagerCoroutine);

        foreach (var coroutine in lightCycleCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        lightCycleCoroutines.Clear();

        foreach (var light in trafficLights)
        {
            light.StopCycle();
            light.RestoreOriginalMaterial();
        }

        foreach (var car in activeCars)
        {
            if (car != null)
            {
                if (trafficManager != null)
                    trafficManager.UnregisterVehicle(car);
                Destroy(car);
            }
        }
        activeCars.Clear();
    }
}

// ============================================================
// GlobalTrafficManager
// ============================================================
public class GlobalTrafficManager : MonoBehaviour
{
    private static GlobalTrafficManager _instance;
    public static GlobalTrafficManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GlobalTrafficManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GlobalTrafficManager");
                    _instance = go.AddComponent<GlobalTrafficManager>();
                }
            }
            return _instance;
        }
    }

    private List<GameObject> allVehicles = new List<GameObject>();

    public void RegisterVehicle(GameObject vehicle)
    {
        if (!allVehicles.Contains(vehicle))
            allVehicles.Add(vehicle);
    }

    public void UnregisterVehicle(GameObject vehicle)
    {
        allVehicles.Remove(vehicle);
    }

    public GameObject DetectFrontCar(GameObject car, float distance, float angle)
    {
        GameObject nearestCar = null;
        float nearestDistance = distance;

        foreach (var other in allVehicles)
        {
            if (other == null || other == car) continue;

            Vector3 toOther = other.transform.position - car.transform.position;
            float dist = toOther.magnitude;

            if (dist > distance) continue;

            float angleToOther = Vector3.Angle(car.transform.forward, toOther.normalized);
            if (angleToOther > angle * 0.5f) continue;

            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearestCar = other;
            }
        }

        return nearestCar;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}