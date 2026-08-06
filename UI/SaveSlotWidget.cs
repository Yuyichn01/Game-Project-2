using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SaveSlotWidget : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text slotNameText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text timestampText;
    [SerializeField] private GameObject emptyOverlay;
    [SerializeField] private Button slotButton;

    public int SlotIndex { get; set; }
    public UnityEvent<int> OnSlotClicked = new();

    private void Start()
    {
        slotButton?.onClick.AddListener(() => OnSlotClicked.Invoke(SlotIndex));
    }

    private void OnDestroy()
    {
        slotButton?.onClick.RemoveAllListeners();
    }

    public void Refresh(SaveMetadata? meta)
    {
        bool isEmpty = meta == null || (meta?.dayIndex == 0 && meta?.timestamp == 0);

        if (isEmpty)
        {
            if (slotNameText != null)
                slotNameText.text = LocalizationManager.Format("save_slot", SlotIndex);
            if (infoText != null)
                infoText.text = LocalizationManager.Get("save_empty");
            if (timestampText != null)
                timestampText.text = "";
            if (emptyOverlay != null)
                emptyOverlay.SetActive(true);
            if (slotButton != null)
                slotButton.interactable = false;
        }
        else
        {
            if (slotNameText != null)
                slotNameText.text = LocalizationManager.Format("save_slot", SlotIndex);
            if (infoText != null)
                infoText.text = string.Format(
                    "Day {0}  {1}",
                    meta?.dayIndex,
                    FormatTime(meta?.playTimeSeconds ?? 0f));
            if (timestampText != null)
                timestampText.text = FormatTimestamp(meta?.timestamp ?? 0);
            if (emptyOverlay != null)
                emptyOverlay.SetActive(false);
            if (slotButton != null)
                slotButton.interactable = true;
        }
    }

    private string FormatTime(float seconds)
    {
        TimeSpan ts = TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0
            ? string.Format("{0}h{1}m", ts.Hours, ts.Minutes)
            : string.Format("{0}m", ts.Minutes);
    }

    private string FormatTimestamp(long unixMs)
    {
        DateTimeOffset dt = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime;
        return dt.ToString("yyyy/MM/dd HH:mm");
    }
}
