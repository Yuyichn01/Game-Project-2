using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSaveManager : MonoBehaviour
{
    [SerializeField] private float autoSaveIntervalSeconds = 300f;

    private Coroutine timerCoroutine;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (autoSaveIntervalSeconds > 0f)
            timerCoroutine = StartCoroutine(AutoSaveTimer());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TriggerAutoSave();
    }

    /// <summary>
    /// 由 StatusManager 在 Day 切换时调用
    /// </summary>
    public void OnDayChanged()
    {
        TriggerAutoSave();
    }

    public void TriggerAutoSave()
    {
        if (DataManager.Instance == null)
            return;

        SaveResult result = DataManager.Instance.SaveToSlot(SaveSlot.Autosave);
        if (!result.Success)
        {
            Debug.LogWarning($"AutoSaveManager: Auto-save to slot Autosave failed: {result.ErrorMessage}");
        }
    }

    private IEnumerator AutoSaveTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveIntervalSeconds);
            TriggerAutoSave();
        }
    }
}
