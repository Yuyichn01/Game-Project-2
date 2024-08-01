using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    [Header("Player value section")]
    public GameObject[] Characters;

    public float decreaseRateStarvation = 1f;

    public float decreaseRateHealth = 1f;

    public float decreaseInterval = 1f;

    private float timer1;

    private float timer2;

    private float timer3;

    public void Update()
    {
        // Calculate the rotation amount for this frame
        float rotationAmount = rotationSpeed * Time.deltaTime;

        // Apply the rotation to the light around the x-axis
        globalLight.transform.Rotate(Vector3.up, rotationAmount, Space.World);

        elapsedTime += Time.deltaTime * timeScale;
        elapsedTime %= timeInADay;
        UpdateClockUI();
        OnHoursChange (hours);
        UpdateDayIndex();

        //decrase starvation and fatigue over time
        timer1 += Time.deltaTime;
        timer2 += Time.deltaTime;
        timer3 += Time.deltaTime;

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

        UIManager.GetComponent<UIManager>().StarvationBar.value =
            UIManager
                .GetComponent<UIManager>()
                .CurrentCharacter
                .GetComponent<PlayerController>()
                .Starvation;
    }

    public void Start()
    {
        //Initialize managers
        UIManager = GameObject.FindWithTag("UIManager");
        elapsedTime = InitialTime * 3600f;

        //allign the sky with hour
        if (InitialTime > 6.0f && InitialTime < 9.0f)
        {
            RenderSettings.skybox.SetTexture("_Texture2", skyboxSunrise);
            StartCoroutine(LerpLight(graddientNightToSunrise, 10f));
        }
        else if (InitialTime > 8.0f && InitialTime < 19.0f)
        {
            RenderSettings.skybox.SetTexture("_Texture2", skyboxDay);
            StartCoroutine(LerpLight(graddientSunriseToDay, 10f));
        }
        else if (InitialTime > 18.0f && InitialTime < 23.0f)
        {
            RenderSettings.skybox.SetTexture("_Texture2", skyboxSunset);
            StartCoroutine(LerpLight(graddientDayToSunset, 10f));
        }
        else
        {
            RenderSettings.skybox.SetTexture("_Texture2", skyboxNight);
            StartCoroutine(LerpLight(graddientSunsetToNight, 10f));
        }
    }

    void UpdateClockUI()
    {
        hours = Mathf.FloorToInt(elapsedTime / 3600f);
        minutes = Mathf.FloorToInt((elapsedTime - hours * 3600f) / 60f);
        seconds =
            Mathf.FloorToInt((elapsedTime - hours * 3600f) - (minutes * 60f));

        /*Debug
            .Log("Hours is " +
            hours +
            " minutes is " +
            minutes +
            "seconds is " +
            seconds);
            */
        string clockString = string.Format("{0:00}:{1:00}", hours, minutes);
        clockText.text = clockString;
    }

    private void OnHoursChange(int hour)
    {
        if (hour == 6)
        {
            //initialize day count
            count = 0;
            StartCoroutine(LerpSkybox(skyboxNight, skyboxSunrise, 10f));
            StartCoroutine(LerpLight(graddientNightToSunrise, 10f));
        }
        if (hour == 8)
        {
            StartCoroutine(LerpSkybox(skyboxSunrise, skyboxDay, 10f));
            StartCoroutine(LerpLight(graddientSunriseToDay, 10f));
        }
        if (hour == 18)
        {
            StartCoroutine(LerpSkybox(skyboxDay, skyboxSunset, 10f));
            StartCoroutine(LerpLight(graddientDayToSunset, 10f));
        }
        if (hour == 22)
        {
            StartCoroutine(LerpSkybox(skyboxSunset, skyboxNight, 10f));
            StartCoroutine(LerpLight(graddientSunsetToNight, 10f));
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
}
