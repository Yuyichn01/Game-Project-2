using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StatusManager : MonoBehaviour
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

    public List<LevelInfo> levelInfo;

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
            LerpLightIntensity(0.7f, fadeDuration);
        }
        if (hour == 8 && minutes == 0)
        {
            StartCoroutine(LerpSkybox(skyboxSunrise, skyboxDay, 10f));
            StartCoroutine(LerpLight(graddientSunriseToDay, 10f));
            LerpLightIntensity(1.0f, fadeDuration);
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
            globalLight.intensity = 0.7f;
        }
        else if (
            currentTime >= 8.0f && currentTime < 17.0f // Day
        )
        {
            RenderSettings.skybox.SetTexture("_Texture1", skyboxDay);
            RenderSettings.skybox.SetTexture("_Texture2", skyboxDay);
            StartCoroutine(LerpLight(graddientSunriseToDay, 1f));
            globalLight.intensity = 1.0f;
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
}
