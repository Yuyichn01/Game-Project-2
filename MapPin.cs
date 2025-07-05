using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapPin : MonoBehaviour
{
    public int sceneIndex;

    public static MapPin selectedPin; // Track the currently selected pin

    public Vector3 normalScale = Vector3.one;

    public Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f); // Scale when selected

    public float scaleDuration = 0.2f; // Time to scale

    public Dialog pinDialog;

    private Button button;

    private GameObject UIManager;

    private GameObject DialogManager;

    private Coroutine scalingCoroutine; // Track coroutine

    void Start()
    {
        // Initialize managers
        DialogManager = GameObject.FindWithTag("DialogManager");
        UIManager = GameObject.FindWithTag("UIManager");

        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener (OnButtonClick);
        }
    }

    void OnButtonClick()
    {
        if (selectedPin != null && selectedPin != this)
        {
            selectedPin.ResetScale();
        }

        // Start smooth scaling
        if (scalingCoroutine != null) StopCoroutine(scalingCoroutine);
        scalingCoroutine =
            StartCoroutine(SmoothScale(transform,
            transform.localScale,
            selectedScale,
            scaleDuration));

        selectedPin = this;

        // Display the dialog
        if (pinDialog != null)
        {
            DialogManager.GetComponent<DialogManager>().StartDialog(pinDialog);
        }

        // Set the to-go button to active
        UIManager
            .GetComponent<UIManager>()
            .ToGoButton
            .gameObject
            .SetActive(true);
    }

    public void ResetScale()
    {
        if (scalingCoroutine != null) StopCoroutine(scalingCoroutine);
        scalingCoroutine =
            StartCoroutine(SmoothScale(transform,
            transform.localScale,
            normalScale,
            scaleDuration));
    }

    IEnumerator
    SmoothScale(Transform target, Vector3 start, Vector3 end, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            target.localScale = Vector3.Lerp(start, end, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        target.localScale = end;
    }
}
