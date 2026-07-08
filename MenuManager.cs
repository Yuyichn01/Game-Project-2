using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Data section")]
    private string filePath;

    private bool confirm;

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

    private GameObject DataManager;

    LocalizedString msg = new LocalizedString("Main table", "Menu_text");

    async void Start()
    {
        confirm = false;

        // Ensure the AudioSource is set to loop
        BGMPlayer.loop = false;

        // Start playing music
        PlayRandomTrack();

        DataManager = GameObject.FindWithTag("DataManager");

        await LocalizationSettings.InitializationOperation.Task;
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
        filePath = Application.persistentDataPath + "/gamedata.dat";
    }

    public void StartGame()
    {
        //检查是否有存档
        if (!confirm && File.Exists(filePath))
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
            //无存档，新建存档
            DataManager.GetComponent<DataManager>().CreateNewGameData(0);
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(CoPlay());

            if (src && clip) src.PlayOneShot(clip);
            Debug.Log("New Game data created!");
        }
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

    // 切换键
    public async void SetKeyAndLog(string key, TextMeshProUGUI mytext)
    {
        msg.TableEntryReference = key; // 例如 "Options"
        mytext.text = await msg.GetLocalizedStringAsync().Task;
    }
}
