using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StatusManager : MonoBehaviour, ISaveable
{
    private GameObject UIManager;

    [SerializeField]
    private Texture2D skyboxNight;

    [SerializeField]
    private Texture2D skyboxSunrise;

    [SerializeField]
    private Texture2D skyboxDay;

    [SerializeField]
    private Texture2D skyboxSunset;

    [SerializeField]
    private Gradient graddientNightToSunrise;

    [SerializeField]
    private Gradient graddientSunriseToDay;

    [SerializeField]
    private Gradient graddientDayToSunset;

    [SerializeField]
    private Gradient graddientSunsetToNight;

    [SerializeField]
    private Light globalLight;

    [Header("Time section")]
    public float rotationSpeed = 10f; // Adjust this hour to control the speed

    private int hours = 0;

    private int minutes = 0;

    private int seconds = 0;

    int count = 0;

    [Header("Clock UI")]
    [SerializeField]
    private TMP_Text clockText;

    private float elapsedTime;

    public float InitialTime = 12.0f;

    [Header("Time in a day")]
    [SerializeField]
    private float timeInADay = 86400f;

    [Header("How fast the time pass")]
    [SerializeField]
    public float timeScale = 2.0f;

    [Header("Light section")]
    public float fadeDuration = 20.0f; // Duration of the fade

    private float startIntensity;

    private float targetIntensity;

    [Header("Player value section")]
    public GameObject[] Characters;

    public float decreaseRateStarvation = 1f;

    public float decreaseRateHealth = 1f;

    public float decreaseInterval = 1f;

    private float timer1;

    private float timer2;

    private float timer3;

    private int currentSceneIndex;

    public bool isMenu = false;

    [System.Serializable]
    public class LevelInfo
    {
        public List<GameObject> Object;
    }

    /// <summary>一个 Root 节点 + 其可用的挂载点列表。</summary>
    [System.Serializable]
    public class RoomRoot
    {
        [Tooltip("物品父节点（物品运行时会被移入此节点下）。")]
        public Transform root;

        [Tooltip("该 root 下的挂载点（物品位置）。")]
        public List<Vector3> placementPoints;
    }

    public List<LevelInfo> levelInfo;

    [Header("LLM Classroom Layout")]
    [Tooltip("是否启用 LLM 物品摆放。")]
    public bool useLLMLayout;

    [Tooltip("Root + 挂载点合集。每个 root 有自己的放置点位，物品会挂在指定 root 下、放在指定点位。")]
    public List<RoomRoot> roots;

    [Tooltip("需要摆放的所有物品列表。")]
    public List<GameObject> placeableItems;

    [Tooltip("房间类型（如 classroom、office），影响 LLM 摆放规则。")]
    public string roomType = "classroom";

    [Tooltip("自定义摆放规则（追加到 LLM prompt）。")]
    [TextArea(3, 6)]
    public string placementRules =
        "如果是 classroom 类型，绝对不能放置花盆、植物类物品。桌椅应排列整齐，彼此间距至少 1.5 米。";

    // LLM JSON 解析用
    [Serializable]
    private class LLMLayoutResult
    {
        public LLMLayoutItem[] items;
    }

    [Serializable]
    private class LLMLayoutItem
    {
        public string item;
        public int root;
        public int point;
    }

    public void Update()
    {
        if (rotationSpeed != 0)
        {
            // Calculate the rotation amount for this frame
            float rotationAmount = rotationSpeed * Time.deltaTime;

            // Apply the rotation to the light around the x-axis
            globalLight
                .transform
                .Rotate(Vector3.up, rotationAmount, Space.World);
        }

        elapsedTime += Time.deltaTime * timeScale;
        elapsedTime %= timeInADay;
        OnHoursChange (hours);
        UpdateClockUI();

        if (!isMenu)
        {
            //decrase starvation and fatigue over time
            timer1 += Time.deltaTime;
            timer2 += Time.deltaTime;
            timer3 += Time.deltaTime;

            UpdateDayIndex();

            //decrease starvation
            decreaseStarvation();
            /* UIManager.GetComponent<UIManager>().StarvationBar.value =
                UIManager
                    .GetComponent<UIManager>()
                    .CurrentCharacter
                    .GetComponent<PlayerController>()
                    .Starvation;\*/
        }
    }

    public void Start()
    {
        // Get the currently active scene
        Scene currentScene = SceneManager.GetActiveScene();

        // Get the index of the current scene
        currentSceneIndex = currentScene.buildIndex;

        elapsedTime = InitialTime * 3600f;

        //allign the sky with hour
        skyAllign (InitialTime);

        //If it current scene is not menu
        if (!isMenu)
        {
            //Initialize UI managers
            UIManager = GameObject.FindWithTag("UIManager");
            SetLevel(UIManager.GetComponent<UIManager>().DayIndex);
        }

        if (DataManager.Instance != null)
            DataManager.Instance.Register(this);

        // ── LLM 教室物品摆放 ──
        if (useLLMLayout && roots.Count > 0 && placeableItems.Count > 0)
        {
            // 1. 将所有物品移入第一个 root 下（后续由 LLM 分配到各 root）
            var firstRoot = roots[0].root;
            foreach (var item in placeableItems)
            {
                if (item != null) item.transform.SetParent(firstRoot);
            }
            // 2. 请求 LLM 摆放
            StartCoroutine(RequestLLMLayout());
        }
    }

    private void OnDestroy()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.Unregister(this);
    }

    void UpdateClockUI()
    {
        hours = Mathf.FloorToInt(elapsedTime / 3600f);
        minutes = Mathf.FloorToInt((elapsedTime - hours * 3600f) / 60f);
        seconds =
            Mathf.FloorToInt((elapsedTime - hours * 3600f) - (minutes * 60f));

        string clockString = string.Format("{0:00}:{1:00}", hours, minutes);
        clockText.text = clockString;
    }

    private void OnHoursChange(int hour)
    {
        if (hour == 6 && minutes == 0)
        {
            //initialize day count
            count = 0;
            StartCoroutine(LerpSkybox(skyboxNight, skyboxSunrise, 10f));
            StartCoroutine(LerpLight(graddientNightToSunrise, 10f));
            LerpLightIntensity(1.5f, fadeDuration);
        }
        if (hour == 8 && minutes == 0)
        {
            StartCoroutine(LerpSkybox(skyboxSunrise, skyboxDay, 10f));
            StartCoroutine(LerpLight(graddientSunriseToDay, 10f));
            LerpLightIntensity(4.0f, fadeDuration);
        }
        if (hour == 17 && minutes == 0)
        {
            StartCoroutine(LerpSkybox(skyboxDay, skyboxSunset, 10f));
            StartCoroutine(LerpLight(graddientDayToSunset, 10f));
            LerpLightIntensity(0.5f, fadeDuration);
        }
        if (hour == 20 && minutes == 0)
        {
            StartCoroutine(LerpSkybox(skyboxSunset, skyboxNight, 10f));
            StartCoroutine(LerpLight(graddientSunsetToNight, 10f));
            LerpLightIntensity(0.1f, fadeDuration);
        }
    }

    void skyAllign(float currentTime)
    {
        //skybox
        if (
            currentTime >= 6.0f && currentTime < 8.0f // Sunrise
        )
        {
            RenderSettings.skybox.SetTexture("_Texture1", skyboxSunrise);
            RenderSettings.skybox.SetTexture("_Texture2", skyboxSunrise);
            StartCoroutine(LerpLight(graddientNightToSunrise, 1f));
            globalLight.intensity = 1.5f;
        }
        else if (
            currentTime >= 8.0f && currentTime < 17.0f // Day
        )
        {
            RenderSettings.skybox.SetTexture("_Texture1", skyboxDay);
            RenderSettings.skybox.SetTexture("_Texture2", skyboxDay);
            StartCoroutine(LerpLight(graddientSunriseToDay, 1f));
            globalLight.intensity = 4.0f;
        }
        else if (
            currentTime >= 17.0f && currentTime < 20.0f // Sunset
        )
        {
            RenderSettings.skybox.SetTexture("_Texture1", skyboxSunset);
            RenderSettings.skybox.SetTexture("_Texture2", skyboxSunset);
            StartCoroutine(LerpLight(graddientDayToSunset, 1f));
            globalLight.intensity = 0.5f;
        } // Night (23:00 to 6:00)
        else
        {
            RenderSettings.skybox.SetTexture("_Texture1", skyboxNight);
            RenderSettings.skybox.SetTexture("_Texture2", skyboxNight);
            StartCoroutine(LerpLight(graddientSunsetToNight, 10f));
            globalLight.intensity = 0.1f;
        }
    }

    private void UpdateDayIndex()
    {
        //update day index
        if (hours == 00 && minutes == 00 && seconds > 00)
        {
            if (count == 0)
            {
                UIManager.GetComponent<UIManager>().addOneDay();
                count += 1;

                // 触发自动存档
                AutoSaveManager autoSave = FindObjectOfType<AutoSaveManager>();
                if (autoSave != null)
                    autoSave.OnDayChanged();
            }
        }
    }

    private IEnumerator LerpSkybox(Texture2D a, Texture2D b, float time)
    {
        RenderSettings.skybox.SetTexture("_Texture1", a);
        RenderSettings.skybox.SetTexture("_Texture2", b);
        RenderSettings.skybox.SetFloat("_Blend", 0);
        for (float i = 0; i < time; i += Time.deltaTime)
        {
            RenderSettings.skybox.SetFloat("_Blend", i / time);
            yield return null;
        }
        RenderSettings.skybox.SetTexture("_Texture1", b);
    }

    private IEnumerator LerpLight(Gradient lightGradient, float time)
    {
        for (float i = 0; i < time; i += Time.deltaTime)
        {
            globalLight.color = lightGradient.Evaluate(i / time);
            RenderSettings.fogColor = globalLight.color;
            yield return null;
        }
    }

    public void decreaseStarvation()
    {
        if (timer1 >= decreaseInterval)
        {
            Characters[0].GetComponent<PlayerController>().Starvation -=
                decreaseRateStarvation;

            // Ensure starvation doesn't go below 0
            if (Characters[0].GetComponent<PlayerController>().Starvation < 0)
            {
                Characters[0].GetComponent<PlayerController>().Starvation = 0;
            }
            timer1 = 0f;
        }

        if (timer2 >= decreaseInterval)
        {
            Characters[1].GetComponent<PlayerController>().Starvation -=
                decreaseRateStarvation;

            // Ensure starvation doesn't go below 0
            if (Characters[1].GetComponent<PlayerController>().Starvation < 0)
            {
                Characters[1].GetComponent<PlayerController>().Starvation = 0;
            }
            timer2 = 0f;
        }

        if (timer3 >= decreaseInterval)
        {
            Characters[2].GetComponent<PlayerController>().Starvation -=
                decreaseRateStarvation;

            // Ensure starvation doesn't go below 0
            if (Characters[2].GetComponent<PlayerController>().Starvation < 0)
            {
                Characters[2].GetComponent<PlayerController>().Starvation = 0;
            }
            timer3 = 0f;
        }
    }

    public void SetLevel(int dayIndex)
    {
        // Validate that the dayIndex is within range
        if (dayIndex < 0 || dayIndex > levelInfo.Count)
        {
            Debug
                .LogError("Please make sure the level info section in status manager is not empty");
            return;
        }

        // Disable all levels first
        for (int i = 0; i < levelInfo.Count; i++)
        {
            SetLevelVisibility(levelInfo[i].Object, false);
        }
        Debug.Log("day index is" + dayIndex);

        // Enable only the current level
        LevelInfo currentLevel = levelInfo[dayIndex - 1];
        SetLevelVisibility(currentLevel.Object, true);
    }

    private void SetLevelVisibility(List<GameObject> objects, bool isVisible)
    {
        foreach (GameObject obj in objects)
        {
            obj.SetActive (isVisible);
        }
    }

    public void LerpLightIntensity(float targetIntensity, float fadeDuration)
    {
        startIntensity = globalLight.intensity; // Always start from the current intensity

        StartCoroutine(FadeLight(targetIntensity, fadeDuration));
    }

    private IEnumerator FadeLight(float targetIntensity, float duration)
    {
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(timeElapsed / duration);

            globalLight.intensity =
                Mathf.Lerp(startIntensity, targetIntensity, progress);
            yield return null;
        }

        globalLight.intensity = targetIntensity; // Ensure exact target value
    }

    public string getCurrentTime()
    {
        return clockText.text;
    }

    public void Save(GameData data)
    {
        if (data == null) return;

        data.elapsedTimeSeconds = elapsedTime;
        data.timeScale = timeScale;
        data.currentSceneIndex = currentSceneIndex;

        // 从 UIManager 获取天数
        if (UIManager != null)
        {
            data.dayIndex = UIManager.GetComponent<UIManager>().DayIndex;
        }
    }

    public void Load(GameData data)
    {
        if (data == null) return;

        elapsedTime = data.elapsedTimeSeconds;
        timeScale = data.timeScale;
        currentSceneIndex = data.currentSceneIndex;

        // 恢复天数到 UIManager
        if (UIManager != null)
        {
            UIManager.GetComponent<UIManager>().DayIndex = data.dayIndex;
        }
    }

    // ═══════════════════════════════════════════
    // LLM 物品摆放
    // ═══════════════════════════════════════════

    private IEnumerator RequestLLMLayout()
    {
        // 等待 LLMDriver 初始化
        yield return new WaitForSeconds(0.5f);

        var driver = GhostSystem.GhostManager.Instance;
        if (driver == null || !driver.enableGhost)
        {
            Debug.Log("[StatusManager] LLM 未启用，使用物品默认位置。");
            yield break;
        }

        string systemPrompt = BuildLayoutSystemPrompt();
        string userPrompt = BuildLayoutUserPrompt();

        bool completed = false;
        driver.SendRequest(systemPrompt, userPrompt,
            onResult: (json) =>
            {
                OnLayoutReceived(json);
                completed = true;
            },
            onError: (err) =>
            {
                Debug.LogWarning($"[StatusManager] LLM 摆放失败: {err}，使用默认位置。");
                completed = true;
            });

        // 等待异步回调（最多等 30 秒）
        float timeout = 30f;
        while (!completed && timeout > 0f)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }

        if (!completed)
            Debug.LogWarning("[StatusManager] LLM 摆放超时，使用默认位置。");
    }

    private string BuildLayoutSystemPrompt()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"你是一个 {roomType} 布局 AI。根据以下规则摆放物品：");
        sb.AppendLine();
        sb.AppendLine($"规则：");
        sb.AppendLine($"- 房间类型: {roomType}");
        sb.AppendLine($"- 可用的 root + 挂载点:");
        for (int i = 0; i < roots.Count; i++)
        {
            var r = roots[i];
            string name = r.root != null ? r.root.name : $"(Root {i})";
            sb.AppendLine($"  root {i} ({name}):");
            if (r.placementPoints != null)
            {
                for (int j = 0; j < r.placementPoints.Count; j++)
                    sb.AppendLine($"    挂载点 {j}: ({r.placementPoints[j].x:F1}, {r.placementPoints[j].z:F1})");
            }
        }
        sb.AppendLine($"- 摆放约束: {placementRules}");
        sb.AppendLine();
        sb.AppendLine($"重要：");
        sb.AppendLine($"1. root 字段必须是 0~{roots.Count - 1} 之间的整数");
        sb.AppendLine($"2. point 字段必须是对应 root 下的有效挂载点索引");
        sb.AppendLine($"3. 每个挂载点最多放一个物品");
        sb.AppendLine($"4. 返回纯 JSON 对象（不要 markdown 代码块），格式如下：");
        sb.AppendLine("{{\"items\":[{{\"item\":\"物品名\",\"root\":0,\"point\":0}},...]}}");
        sb.AppendLine($"5. 每个物品名必须与输入列表完全一致");
        return sb.ToString();
    }

    private string BuildLayoutUserPrompt()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("请摆放以下物品：");
        for (int i = 0; i < placeableItems.Count; i++)
        {
            if (placeableItems[i] != null)
                sb.AppendLine($"  - {placeableItems[i].name}");
        }
        return sb.ToString();
    }

    private void OnLayoutReceived(string json)
    {
        // 提取 JSON（LLM 可能包裹在 markdown 代码块中）
        string cleanJson = ExtractJsonObject(json);
        if (string.IsNullOrEmpty(cleanJson))
        {
            Debug.LogWarning("[StatusManager] LLM 返回格式无效。");
            return;
        }

        try
        {
            var result = JsonUtility.FromJson<LLMLayoutResult>(cleanJson);
            if (result?.items == null || result.items.Length == 0)
            {
                Debug.LogWarning("[StatusManager] LLM 返回的 items 为空。");
                return;
            }
            ApplyLayout(result.items);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[StatusManager] LLM 响应解析失败: {ex.Message}");
        }
    }

    private static string ExtractJsonObject(string raw)
    {
        // 去掉可能的 markdown ```json ... ``` 包裹
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
            return raw.Substring(start, end - start + 1);
        return raw;
    }

    private void ApplyLayout(LLMLayoutItem[] layout)
    {
        var itemMap = new Dictionary<string, GameObject>();
        foreach (var obj in placeableItems)
        {
            if (obj != null) itemMap[obj.name] = obj;
        }

        var pointUsed = new HashSet<(int root, int point)>();

        int placed = 0;
        foreach (var entry in layout)
        {
            if (!itemMap.TryGetValue(entry.item, out var obj))
            {
                Debug.LogWarning($"[StatusManager] LLM 返回了未知物品: {entry.item}");
                continue;
            }

            // 校验 root 索引
            if (entry.root < 0 || entry.root >= roots.Count)
            {
                Debug.LogWarning($"[StatusManager] 无效 root 索引: {entry.root}");
                continue;
            }

            var r = roots[entry.root];
            if (r.root == null || r.placementPoints == null || r.placementPoints.Count == 0)
                continue;

            // 校验 point 索引
            if (entry.point < 0 || entry.point >= r.placementPoints.Count)
            {
                Debug.LogWarning($"[StatusManager] 无效 point 索引: {entry.point} (root {entry.root})");
                continue;
            }

            // 挂载点去重
            if (!pointUsed.Add((entry.root, entry.point)))
            {
                Debug.LogWarning($"[StatusManager] 挂载点重复: root {entry.root} point {entry.point}");
                continue;
            }

            // 设置 parent + position
            var point = r.placementPoints[entry.point];
            obj.transform.SetParent(r.root);
            obj.transform.position = new Vector3(point.x, obj.transform.position.y, point.z);
            placed++;
        }

        Debug.Log($"[StatusManager] LLM 摆放完成: {placed}/{layout.Length} 个物品，分布在 {roots.Count} 个 root");
    }
}
