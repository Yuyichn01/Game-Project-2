using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class SaveLoadPanel : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private SaveLoadMode currentMode;

    [Header("UI Refs")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Animator animator;

    [Header("Slots")]
    [SerializeField] private SaveSlotWidget[] slotWidgets;

    [Header("Dialog")]
    [SerializeField] private ConfirmDialog confirmDialog;

    public bool IsOpen { get; private set; }

    public void Show(SaveLoadMode mode)
    {
        currentMode = mode;
        if (titleText != null)
        {
            string key = mode == SaveLoadMode.Save ? "save_title" : "load_title";
            titleText.text = LocalizationManager.Get(key);
        }

        RefreshSlots();
        SetPanelActive(true);
    }

    public void Hide()
    {
        SetPanelActive(false);
    }

    private void SetPanelActive(bool active)
    {
        if (panelRoot != null)
            panelRoot.SetActive(active);
        if (animator != null)
            animator.SetBool("IsOpen", active);
        IsOpen = active;
    }

    private void RefreshSlots()
    {
        SaveSlot[] slots = { SaveSlot.Slot0, SaveSlot.Slot1, SaveSlot.Slot2, SaveSlot.Autosave };

        for (int i = 0; i < slotWidgets.Length && i < slots.Length; i++)
        {
            if (slotWidgets[i] == null)
                continue;

            SaveSlot slot = slots[i];
            slotWidgets[i].SlotIndex = (int)slot;

            if (DataManager.Instance != null && DataManager.Instance.SlotExists(slot))
            {
                SaveMetadata meta = DataManager.Instance.GetSlotMetadata(slot);
                slotWidgets[i].Refresh(meta);
            }
            else
            {
                slotWidgets[i].Refresh(null);
            }
        }
    }

    private void Start()
    {
        closeButton?.onClick.AddListener(Hide);

        foreach (SaveSlotWidget sw in slotWidgets)
        {
            if (sw != null)
                sw.OnSlotClicked.AddListener(OnSlotClicked);
        }
    }

    private void OnDestroy()
    {
        closeButton?.onClick.RemoveListener(Hide);

        foreach (SaveSlotWidget sw in slotWidgets)
        {
            if (sw != null)
                sw.OnSlotClicked.RemoveListener(OnSlotClicked);
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        SaveSlot slot = (SaveSlot)slotIndex;

        if (currentMode == SaveLoadMode.Save)
        {
            HandleSave(slot);
        }
        else
        {
            HandleLoad(slot);
        }
    }

    private void HandleSave(SaveSlot slot)
    {
        if (DataManager.Instance == null)
            return;

        if (DataManager.Instance.SlotExists(slot))
        {
            SaveMetadata meta = DataManager.Instance.GetSlotMetadata(slot);
            confirmDialog.Show(
                LocalizationManager.Get("save_overwrite"),
                LocalizationManager.Format("save_overwrite_msg", (int)slot, meta.dayIndex),
                () => { DoSave(slot); });
        }
        else
        {
            DoSave(slot);
        }
    }

    private void DoSave(SaveSlot slot)
    {
        if (DataManager.Instance == null)
            return;

        SaveResult result = DataManager.Instance.SaveToSlot(slot);

        if (result.Success)
        {
            RefreshSlots();
        }
        else
        {
            confirmDialog.ShowAlert(
                LocalizationManager.Get("save_failed"),
                result.ErrorMessage);
        }
    }

    private void HandleLoad(SaveSlot slot)
    {
        if (DataManager.Instance == null)
            return;

        if (!DataManager.Instance.SlotExists(slot))
            return;

        SaveMetadata meta = DataManager.Instance.GetSlotMetadata(slot);

        confirmDialog.Show(
            LocalizationManager.Get("save_load"),
            LocalizationManager.Format("save_load_msg", (int)slot),
            () =>
            {
                SaveResult result = DataManager.Instance.LoadFromSlot(slot);

                if (result.Success)
                {
                    int sceneIndex = result.Data.currentSceneIndex;
                    if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
                    {
                        SceneManager.LoadSceneAsync(sceneIndex);
                    }
                    else
                    {
                        confirmDialog.ShowAlert(
                            LocalizationManager.Get("save_load_failed"),
                            $"Invalid scene index: {sceneIndex}");
                        Debug.LogError($"SaveLoadPanel: Load failed — scene index {sceneIndex} is out of range (0–{SceneManager.sceneCountInBuildSettings - 1}).");
                    }
                }
                else
                {
                    confirmDialog.ShowAlert(
                        LocalizationManager.Get("save_load_failed"),
                        result.ErrorMessage);
                }
            });
    }
}
