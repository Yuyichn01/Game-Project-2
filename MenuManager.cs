using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour, ISaveable
{
    [Header("Data section")]
    private bool confirm;

    [Header("Move Object")]
    [SerializeField] private GameObject moveTarget;
    [SerializeField] private float cameraDistance = 2f;
    [SerializeField] private float verticalOffset = 0.5f;
    [SerializeField] private float moveDuration = 0.8f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originPosition;
    private Quaternion originRotation;
    private bool originSaved;

    [Header("Help / Computer Terminal")]
    [SerializeField] private Button helpButton;
    [SerializeField] private ComputerTerminal computerTerminal;
    [SerializeField] private Transform cameraTarget;

    private Vector3 _cameraOriginPosition;
    private Quaternion _cameraOriginRotation;
    private bool _cameraOriginSaved;
    private CameraController _cameraController;
    private CameraSwing _cameraSwing;

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TMP_Text apiUrlLabel;
    [SerializeField] private TMP_InputField apiUrlInput;
    [SerializeField] private TMP_Text apiKeyLabel;
    [SerializeField] private TMP_InputField apiKeyInput;
    [SerializeField] private TMP_Text modelLabel;
    [SerializeField] private TMP_InputField modelNameInput;
    [SerializeField] private TMP_Text volumeLabel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button settingsApplyButton;
    [SerializeField] private TMP_Text settingsApplyButtonText;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private TMP_Text settingsCloseButtonText;
    [SerializeField] private Button[] menuButtons;

    [Header("Language")]
    [SerializeField] private TMP_Text languageLabel;
    [SerializeField] private TMP_Text currentLanguageText;
    [SerializeField] private Button languagePrevButton;
    [SerializeField] private Button languageNextButton;

    [Header("Glitch effect")]
    public TextMeshProUGUI infoText;

    public TextMeshProUGUI startButtonText;

    public RawImage overlay;

    public float glitchDuration = 0.7f;

    public float blackoutDuration = 0.8f;

    public AnimationCurve glitchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public AnimationCurve blackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public int nextScene = 0; // 需要的话填要加载的场景名

    Material mat;

    public AudioSource src; // 指向一个空AudioSource（不放Clip）

    public AudioClip clip; // 要播放的音效

    [Header("Manager section")]
    public List<AudioClip> bgmClips; // List of BGM clips

    public AudioSource BGMPlayer;

    private DataManager dataManager;

    [Header("Player Profile Questionnaire")]
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private TMP_Text profileQuestionText;
    [SerializeField] private TMP_InputField profileAnswerInput;
    [SerializeField] private Button profileNextButton;
    [SerializeField] private Button profileSubmitButton;
    [SerializeField] private Button profileCloseButton;
    [SerializeField] private TMP_FontAsset profileFont;

    /// <summary>3 个哲学问题的 localization key</summary>
    private static readonly string[] ProfileQuestionKeys = { "profile_q1", "profile_q2", "profile_q3" };

    private int currentQuestionIndex;
    private List<string> profileAnswers = new List<string>();
    private string[] _loadedProfileQuestions;
    private string _loadedWaitingText;

    LocalizedString msg = new LocalizedString("Main table", "Menu_text");

    async void Start()
    {
        confirm = false;

        // Ensure the AudioSource is set to loop
        BGMPlayer.loop = false;

        // Start playing music
        PlayRandomTrack();

        GameObject dmObj = GameObject.FindWithTag("DataManager");
        if (dmObj != null)
            dataManager = dmObj.GetComponent<DataManager>();

        if (dataManager != null)
            dataManager.Register(this);

        // 设置按钮回调
        if (settingsApplyButton != null)
            settingsApplyButton.onClick.AddListener(ApplySettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(HideSettings);
        if (helpButton != null)
            helpButton.onClick.AddListener(ShowHelp);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (profilePanel != null)
            profilePanel.SetActive(false);
        if (languagePrevButton != null)
            languagePrevButton.onClick.AddListener(() => SwitchLanguage(-1));
        if (languageNextButton != null)
            languageNextButton.onClick.AddListener(() => SwitchLanguage(1));

        // 初始化音量
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        await LocalizationSettings.InitializationOperation.Task;

        // 初始化菜单文字本地化
        InitMenuLabels();
    }

    private void InitMenuLabels()
    {
        SetLabelText(infoText, "menu_title");
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    void PlayRandomTrack()
    {
        if (bgmClips.Count == 0) return;

        // Choose a random clip from the list
        int randomIndex = Random.Range(0, bgmClips.Count);
        BGMPlayer.clip = bgmClips[randomIndex];
        BGMPlayer.Play();
    }

    void Awake()
    {
        if (!overlay) overlay = GetComponent<RawImage>();
        if (!overlay)
        {
            Debug.LogError("Assign RawImage!");
            return;
        }

        // 确保有纹理，避免 _MainTex 报错
        if (overlay.texture == null) overlay.texture = Texture2D.whiteTexture;

        // 运行时实例化材质再改参数
        mat = Instantiate(overlay.material);
        overlay.material = mat;
        overlay.enabled = false;

        // 初始参数
        mat.SetFloat("_Glitch", 0f);
        mat.SetFloat("_Black", 0f);

        // 可根据口味预设强度：_Slices/_Amplitude/_RGBSplit/_Speed/_Seed
    }

    public void StartGame()
    {
        //检查是否有存档（通过 DataManager 检测 Slot0）
        bool hasSave = DataManager.Instance != null && DataManager.Instance.SlotExists(SaveSlot.Slot0);
        if (!confirm && hasSave)
        {
            //有存档，显示警告
            //DataExist_warn
            SetKeyAndLog("DataExist_warn", infoText);
            SetKeyAndLog("continue", startButtonText);
            Debug.Log("Old Game data found!");
            confirm = true;
        }
        else
        {
            // 弹出玩家画像问卷
            StartCoroutine(ShowProfileQuestionnaire());
        }
    }

    // ──────────── 玩家画像问卷 ────────────

    /// <summary>从 Localization 表异步预加载所有问题文本</summary>
    private async System.Threading.Tasks.Task LoadProfileQuestionsAsync()
    {
        _loadedProfileQuestions = new string[ProfileQuestionKeys.Length];
        for (int i = 0; i < ProfileQuestionKeys.Length; i++)
        {
            msg.TableEntryReference = ProfileQuestionKeys[i];
            _loadedProfileQuestions[i] = await msg.GetLocalizedStringAsync().Task;
        }
        msg.TableEntryReference = "profile_waiting";
        _loadedWaitingText = await msg.GetLocalizedStringAsync().Task;
    }

    private IEnumerator ShowProfileQuestionnaire()
    {
        // 预加载多语言问题文本
        var loadTask = LoadProfileQuestionsAsync();
        yield return new WaitUntil(() => loadTask.IsCompleted);

        // 将面板移到摄像机正前方
        if (moveTarget != null)
            yield return CoMoveToTarget();

        currentQuestionIndex = 0;
        profileAnswers.Clear();

        // 隐藏菜单按钮，显示问卷面板
        SetMenuButtonsInteractable(false);

        // 运行时注入支持中文的 SDF 字体（双保险：即使 TMP Settings fallback 失效也能正常渲染）
        if (profileFont != null)
        {
            if (profileQuestionText != null) profileQuestionText.font = profileFont;
            if (profileAnswerInput != null)
            {
                if (profileAnswerInput.textComponent != null)
                    profileAnswerInput.textComponent.font = profileFont;
                if (profileAnswerInput.placeholder is TMP_Text placeholder)
                    placeholder.font = profileFont;
            }
        }

        profilePanel.SetActive(true);

        // 绑定按钮
        if (profileCloseButton != null)
            profileCloseButton.onClick.AddListener(CloseProfile);
        if (profileNextButton != null)
            profileNextButton.onClick.AddListener(OnProfileNext);
        if (profileSubmitButton != null)
        {
            profileSubmitButton.onClick.AddListener(OnProfileNext);
            profileSubmitButton.gameObject.SetActive(false);
        }

        // 清空输入框
        if (profileAnswerInput != null)
            profileAnswerInput.text = string.Empty;

        ShowQuestion(currentQuestionIndex);
    }

    private void ShowQuestion(int index)
    {
        if (_loadedProfileQuestions == null || index >= _loadedProfileQuestions.Length)
        {
            OnQuestionnaireComplete();
            return;
        }

        int total = _loadedProfileQuestions.Length;
        if (profileQuestionText != null)
            profileQuestionText.text = $"[{index + 1}/{total}] {_loadedProfileQuestions[index]}";

        if (profileAnswerInput != null)
        {
            profileAnswerInput.text = string.Empty;
            profileAnswerInput.ActivateInputField();
        }

        bool isLast = (index == total - 1);
        if (profileNextButton != null)
            profileNextButton.gameObject.SetActive(!isLast);
        if (profileSubmitButton != null)
            profileSubmitButton.gameObject.SetActive(isLast);
    }

    private void OnProfileNext()
    {
        string answer = profileAnswerInput != null ? profileAnswerInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(answer))
        {
            Debug.Log("[MenuManager] 请输入回答");
            return;
        }

        int qIndex = currentQuestionIndex;
        int total = _loadedProfileQuestions?.Length ?? ProfileQuestionKeys.Length;
        string question = qIndex < total ? _loadedProfileQuestions[qIndex] : "";
        profileAnswers.Add($"Q: {question}\nA: {answer}");
        currentQuestionIndex++;

        if (currentQuestionIndex >= total)
        {
            // 清理按钮监听
            if (profileNextButton != null)
                profileNextButton.onClick.RemoveAllListeners();
            if (profileSubmitButton != null)
                profileSubmitButton.onClick.RemoveAllListeners();
            // 交给 LLM 分析
            OnQuestionnaireComplete();
        }
        else
        {
            ShowQuestion(currentQuestionIndex);
        }
    }

    private void OnQuestionnaireComplete()
    {
        // 1. 把答案托管给 DataManager
        if (DataManager.Instance != null)
            DataManager.Instance.SetProfileAnswers(profileAnswers.ToArray());

        // 2. 隐藏输入框和按钮，显示等待提示
        if (profileAnswerInput != null)
            profileAnswerInput.gameObject.SetActive(false);
        if (profileNextButton != null)
            profileNextButton.gameObject.SetActive(false);
        if (profileSubmitButton != null)
            profileSubmitButton.gameObject.SetActive(false);
        if (profileCloseButton != null)
            profileCloseButton.gameObject.SetActive(false);
        if (profileQuestionText != null)
            profileQuestionText.text = !string.IsNullOrEmpty(_loadedWaitingText)
                ? _loadedWaitingText
                : "AI is analyzing your personality...\nPlease wait";

        // 3. 调用 LLMDriver 分析（异步）
        var driver = GhostSystem.GhostManager.Instance;
        if (driver != null && driver.enableGhost)
        {
            driver.AnalyzePlayerProfile(OnProfileAnalysisDone, OnProfileAnalysisError);
        }
        else
        {
            // 没有 LLMDriver，直接开始游戏
            StartGameWithProfile();
        }
    }

    private void OnProfileAnalysisDone(string analysis)
    {
        // 存储分析结果
        if (DataManager.Instance != null)
            DataManager.Instance.SetProfileAnalysis(analysis);

        Debug.Log($"[MenuManager] Profile analysis received:\n{analysis}");
        StartGameWithProfile();
    }

    private void OnProfileAnalysisError(string error)
    {
        Debug.LogWarning($"[MenuManager] Profile analysis failed: {error}. Starting game anyway.");
        StartGameWithProfile();
    }

    private void CloseProfile()
    {
        if (profilePanel != null)
            profilePanel.SetActive(false);

        // 清理按钮监听
        if (profileNextButton != null)
            profileNextButton.onClick.RemoveAllListeners();
        if (profileSubmitButton != null)
            profileSubmitButton.onClick.RemoveAllListeners();
        if (profileCloseButton != null)
            profileCloseButton.onClick.RemoveAllListeners();

        MoveBackToOrigin();
        SetMenuButtonsInteractable(true);
    }

    private void StartGameWithProfile()
    {
        if (profilePanel != null)
            profilePanel.SetActive(false);

        dataManager.CreateNewGameData(0, 1, nextScene);
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(CoPlay());

        if (src && clip) src.PlayOneShot(clip);
        Debug.Log($"New Game data created! Profile answers: {profileAnswers.Count}");
    }

    // ──────────────────────────────────────

    public void LoadGame()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("MenuManager: DataManager.Instance is null, cannot load game.");
            return;
        }

        // 优先 Slot0（手动存档），回退到 Autosave
        SaveSlot slot = SaveSlot.Slot0;
        if (!DataManager.Instance.SlotExists(SaveSlot.Slot0))
        {
            if (DataManager.Instance.SlotExists(SaveSlot.Autosave))
            {
                slot = SaveSlot.Autosave;
                Debug.Log("MenuManager: Slot0 not found, falling back to Autosave.");
            }
            else
            {
                Debug.LogWarning("MenuManager: No save file found.");
                return;
            }
        }

        SaveResult result = DataManager.Instance.LoadFromSlot(slot);
        if (!result.Success)
        {
            Debug.LogError($"MenuManager: LoadGame failed — {result.ErrorMessage}");
            return;
        }

        int sceneIndex = result.Data.currentSceneIndex;
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            if (src && clip) src.PlayOneShot(clip);
            PlayTransitionAndLoad(sceneIndex);
        }
        else
        {
            Debug.LogError($"MenuManager: LoadGame failed — invalid scene index {sceneIndex}.");
        }
    }

    private void PlayTransitionAndLoad(int sceneIndex)
    {
        StartCoroutine(CoLoadScene(sceneIndex));
    }

    IEnumerator CoLoadScene(int sceneIndex)
    {
        overlay.enabled = true;

        // 1) Glitch 阶段（短一点表示读档）
        float t = 0f;
        while (t < glitchDuration * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float p = glitchCurve.Evaluate(Mathf.Clamp01(t / (glitchDuration * 0.5f)));
            mat.SetFloat("_Glitch", p);
            mat.SetFloat("_Seed", Random.Range(0f, 999f));
            yield return null;
        }
        mat.SetFloat("_Glitch", 1f);

        // 2) Blackout 阶段
        t = 0f;
        while (t < blackoutDuration)
        {
            t += Time.unscaledDeltaTime;
            float b = blackCurve.Evaluate(Mathf.Clamp01(t / blackoutDuration));
            mat.SetFloat("_Black", b);
            yield return null;
        }
        mat.SetFloat("_Black", 1f);

        // 3) 切换场景
        SceneManager.LoadSceneAsync(sceneIndex);
    }

    IEnumerator CoPlay()
    {
        overlay.enabled = true;

        // 1) Glitch 阶段
        float t = 0f;
        while (t < glitchDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = glitchCurve.Evaluate(Mathf.Clamp01(t / glitchDuration));
            mat.SetFloat("_Glitch", p);

            // 可选：逐帧变Seed让分片图样更乱
            mat.SetFloat("_Seed", Random.Range(0f, 999f));
            yield return null;
        }
        mat.SetFloat("_Glitch", 1f);

        // 2) Blackout 阶段（同时逐渐减弱Glitch也行）
        t = 0f;
        while (t < blackoutDuration)
        {
            t += Time.unscaledDeltaTime;
            float b = blackCurve.Evaluate(Mathf.Clamp01(t / blackoutDuration));
            mat.SetFloat("_Black", b);

            // 让 glitch 在变黑时稍微回落
            mat.SetFloat("_Glitch", 1f - 0.7f * b);
            yield return null;
        }
        mat.SetFloat("_Black", 1f);
        mat.SetFloat("_Glitch", 0.3f);

        SceneManager.LoadScene (nextScene);
    }

    public void QuitGame()
    {
        // 关闭菜单窗口
        gameObject.SetActive(false);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("[MenuManager] settingsPanel 未赋值");
            return;
        }
        settingsPanel.SetActive(true);
        SetMenuButtonsInteractable(false);
        SetLabelText(languageLabel, "setting_language");
        UpdateLanguageDisplay();
        LoadSettingsFromDriver();
    }

    public void HideSettings()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(false);
        SetMenuButtonsInteractable(true);
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        foreach (var btn in menuButtons)
        {
            if (btn != null)
                btn.interactable = interactable;
        }
    }

    private void SetMenuButtonsVisible(bool visible)
    {
        foreach (var btn in menuButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(visible);
        }
    }

    private void LoadSettingsFromDriver()
    {
        // 设置标签
        SetLabelText(apiUrlLabel, "settings_api_url");
        SetLabelText(apiKeyLabel, "settings_api_key");
        SetLabelText(modelLabel, "settings_model");
        SetLabelText(volumeLabel, "settings_volume");
        SetLabelText(settingsApplyButtonText, "settings_apply");
        SetLabelText(settingsCloseButtonText, "settings_close");

        var driver = GhostSystem.GhostManager.Instance;
        if (driver == null)
        {
            Debug.LogWarning("[MenuManager] GhostManager.Instance 不存在");
            return;
        }

        SetText(apiUrlInput, driver.apiUrl);
        SetText(apiKeyInput, driver.apiKey);
        SetText(modelNameInput, driver.modelName);

        // 声音：从 PlayerPrefs 读取，默认 1
        if (volumeSlider != null)
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
    }

    private void SwitchLanguage(int direction)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales.Count == 0) return;

        int currentIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);
        int newIndex = (currentIndex + direction + locales.Count) % locales.Count;

        LocalizationSettings.SelectedLocale = locales[newIndex];
        PlayerPrefs.SetString("SelectedLocale", locales[newIndex].Identifier.Code);
        PlayerPrefs.Save();

        UpdateLanguageDisplay();
        InitMenuLabels();
        LoadSettingsFromDriver();
    }

    private void UpdateLanguageDisplay()
    {
        if (currentLanguageText == null) return;
        var locale = LocalizationSettings.SelectedLocale;
        string localeKey = "lang_" + locale.Identifier.Code;
        currentLanguageText.text = LocalizationManager.Get(localeKey);
    }

    private static void SetLabelText(TMP_Text label, string key)
    {
        if (label != null)
            label.text = LocalizationManager.Get(key);
    }

    public void ApplySettings()
    {
        var driver = GhostSystem.GhostManager.Instance;
        if (driver == null)
        {
            Debug.LogWarning("[MenuManager] GhostManager.Instance 不存在");
            return;
        }

        driver.apiUrl = GetText(apiUrlInput);
        driver.apiKey = GetText(apiKeyInput);
        driver.modelName = GetText(modelNameInput);

        // 保存声音
        if (volumeSlider != null)
        {
            PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
            AudioListener.volume = volumeSlider.value;
        }
        PlayerPrefs.Save();

        Debug.Log("[MenuManager] 设置已保存");
    }

    private static string GetText(TMP_InputField field) =>
        field != null ? field.text : string.Empty;

    private static void SetText(TMP_InputField field, string value)
    {
        if (field != null) field.text = value;
    }

    /// <summary>
    /// 将 moveTarget 平滑移动到摄像机正前方，加速-减速，可选摆正。
    /// </summary>
    public void MoveToTargetPosition()
    {
        if (moveTarget == null)
        {
            Debug.LogWarning("[MenuManager] moveTarget 未赋值");
            return;
        }
        StartCoroutine(CoMoveToTarget());
    }

    private IEnumerator CoMoveToTarget()
    {
        // 记录原始 Transform 信息
        originPosition = moveTarget.transform.position;
        originRotation = moveTarget.transform.rotation;
        originSaved = true;

        Vector3 destination = Camera.main.transform.position
            + Camera.main.transform.forward * cameraDistance
            + Vector3.up * verticalOffset;

        // 正对摄像机
        Quaternion faceCamera = Quaternion.LookRotation(Camera.main.transform.forward);

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float curvedT = moveCurve.Evaluate(t);

            moveTarget.transform.SetPositionAndRotation(
                Vector3.Lerp(originPosition, destination, curvedT),
                Quaternion.Slerp(originRotation, faceCamera, curvedT));

            yield return null;
        }

        moveTarget.transform.SetPositionAndRotation(destination, faceCamera);
    }

    /// <summary>
    /// 将 moveTarget 平滑移回原始位置，旋转保持不变。
    /// </summary>
    public void MoveBackToOrigin()
    {
        if (moveTarget == null)
        {
            Debug.LogWarning("[MenuManager] moveTarget 未赋值");
            return;
        }
        if (!originSaved)
        {
            Debug.LogWarning("[MenuManager] 尚未记录原始位置，请先调用 MoveToTargetPosition");
            return;
        }
        StartCoroutine(CoMoveBackToOrigin());
    }

    private IEnumerator CoMoveBackToOrigin()
    {
        Vector3 startPos = moveTarget.transform.position;
        Quaternion startRot = moveTarget.transform.rotation;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float curvedT = moveCurve.Evaluate(t);

            moveTarget.transform.SetPositionAndRotation(
                Vector3.Lerp(startPos, originPosition, curvedT),
                Quaternion.Slerp(startRot, originRotation, curvedT));

            yield return null;
        }

        moveTarget.transform.SetPositionAndRotation(originPosition, originRotation);
    }

    // ──────────── 电脑终端 Help 系统 ────────────

    public void ShowHelp()
    {
        if (Camera.main == null || cameraTarget == null)
        {
            Debug.LogWarning("[MenuManager] Camera.main 或 cameraTarget 未赋值");
            return;
        }

        // 隐藏所有菜单按钮，防止透过终端可见
        SetMenuButtonsVisible(false);

        // 禁用 CameraController 和 CameraSwing，防止它们覆盖摄像机位置
        _cameraController = Camera.main.GetComponent<CameraController>();
        if (_cameraController != null)
            _cameraController.enabled = false;
        _cameraSwing = Camera.main.GetComponent<CameraSwing>();
        if (_cameraSwing != null)
            _cameraSwing.enabled = false;

        // 保存摄像机原始位置
        if (!_cameraOriginSaved)
        {
            _cameraOriginPosition = Camera.main.transform.position;
            _cameraOriginRotation = Camera.main.transform.rotation;
            _cameraOriginSaved = true;
        }

        StartCoroutine(CoMoveCameraToTerminal());
    }

    public void CloseHelp()
    {
        if (!_cameraOriginSaved) return;

        if (computerTerminal != null)
            computerTerminal.Hide();

        StartCoroutine(CoMoveCameraBack());
    }

    private IEnumerator CoMoveCameraToTerminal()
    {
        if (cameraTarget == null) yield break;

        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float curvedT = moveCurve.Evaluate(t);

            Camera.main.transform.SetPositionAndRotation(
                Vector3.Lerp(startPos, cameraTarget.position, curvedT),
                Quaternion.Slerp(startRot, cameraTarget.rotation, curvedT));

            yield return null;
        }

        Camera.main.transform.SetPositionAndRotation(cameraTarget.position, cameraTarget.rotation);

        if (computerTerminal != null)
            computerTerminal.Show();
    }

    private IEnumerator CoMoveCameraBack()
    {
        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float curvedT = moveCurve.Evaluate(t);

            Camera.main.transform.SetPositionAndRotation(
                Vector3.Lerp(startPos, _cameraOriginPosition, curvedT),
                Quaternion.Slerp(startRot, _cameraOriginRotation, curvedT));

            yield return null;
        }

        Camera.main.transform.SetPositionAndRotation(_cameraOriginPosition, _cameraOriginRotation);

        // 恢复菜单按钮
        SetMenuButtonsVisible(true);

        // 恢复 CameraController 和 CameraSwing
        if (_cameraController != null)
        {
            _cameraController.enabled = true;
            _cameraController = null;
        }
        if (_cameraSwing != null)
        {
            _cameraSwing.enabled = true;
            _cameraSwing = null;
        }
    }

    // ──────────────────────────────────────

    private void OnDestroy()
    {
        if (dataManager != null)
            dataManager.Unregister(this);

        if (settingsApplyButton != null)
            settingsApplyButton.onClick.RemoveListener(ApplySettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.RemoveListener(HideSettings);
        if (helpButton != null)
            helpButton.onClick.RemoveListener(ShowHelp);
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (languagePrevButton != null)
            languagePrevButton.onClick.RemoveAllListeners();
        if (languageNextButton != null)
            languageNextButton.onClick.RemoveAllListeners();

        // 清理问卷按钮监听
        if (profileNextButton != null)
            profileNextButton.onClick.RemoveAllListeners();
        if (profileSubmitButton != null)
            profileSubmitButton.onClick.RemoveAllListeners();
        if (profileCloseButton != null)
            profileCloseButton.onClick.RemoveAllListeners();
    }

    // 切换键
    public async void SetKeyAndLog(string key, TextMeshProUGUI mytext)
    {
        msg.TableEntryReference = key; // 例如 "Options"
        mytext.text = await msg.GetLocalizedStringAsync().Task;
    }

    public void Save(GameData data)
    {
        if (data?.settings == null)
            data.settings = new SettingsSaveData();

        // 音量
        data.settings.masterVolume = AudioListener.volume;

        // LLM 设置
        var driver = GhostSystem.GhostManager.Instance;
        if (driver != null)
        {
            data.settings.llmApiUrl = driver.apiUrl ?? string.Empty;
            data.settings.llmApiKey = driver.apiKey ?? string.Empty;
            data.settings.llmModelName = driver.modelName ?? string.Empty;
        }

        // 语言
        var locale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
        data.settings.localeCode = locale?.Identifier.Code ?? string.Empty;

        // 玩家画像
        if (profileAnswers.Count > 0)
            data.profileAnswers = profileAnswers.ToArray();
    }

    public void Load(GameData data)
    {
        if (data?.settings == null) return;

        SettingsSaveData settings = data.settings;

        // 恢复音量
        float volume = settings.masterVolume;
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        if (volumeSlider != null)
            volumeSlider.value = volume;

        // 恢复 LLM 设置
        var driver = GhostSystem.GhostManager.Instance;
        if (driver != null)
        {
            driver.apiUrl = settings.llmApiUrl ?? string.Empty;
            driver.apiKey = settings.llmApiKey ?? string.Empty;
            driver.modelName = settings.llmModelName ?? string.Empty;
        }

        // 恢复语言
        if (!string.IsNullOrEmpty(settings.localeCode))
        {
            var locales = UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.Locales;
            foreach (var locale in locales)
            {
                if (locale.Identifier.Code == settings.localeCode)
                {
                    UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale = locale;
                    PlayerPrefs.SetString("SelectedLocale", settings.localeCode);
                    break;
                }
            }
        }

        // 恢复玩家画像
        if (data.profileAnswers != null && data.profileAnswers.Length > 0)
            profileAnswers = new List<string>(data.profileAnswers);

        PlayerPrefs.Save();
    }
}
