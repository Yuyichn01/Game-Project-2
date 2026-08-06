using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmDialog : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialogRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private TMP_Text cancelButtonText;

    private Action onConfirm;
    private Action onCancel;

    public bool IsOpen { get; private set; }

    private void Start()
    {
        confirmButton?.onClick.AddListener(OnConfirmClicked);
        cancelButton?.onClick.AddListener(OnCancelClicked);
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        confirmButton?.onClick.RemoveListener(OnConfirmClicked);
        cancelButton?.onClick.RemoveListener(OnCancelClicked);
    }

    /// <summary>
    /// 显示确认对话框（双按钮：确认 + 取消）
    /// </summary>
    public void Show(string title, string message, Action onConfirm, Action onCancel = null)
    {
        if (titleText != null)
            titleText.text = title;
        if (messageText != null)
            messageText.text = message;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(true);
        SetOpen(true);
    }

    /// <summary>
    /// 显示提示对话框（单按钮：确定）
    /// </summary>
    public void ShowAlert(string title, string message, Action onOk = null)
    {
        if (titleText != null)
            titleText.text = title;
        if (messageText != null)
            messageText.text = message;
        onConfirm = onOk;
        onCancel = null;
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(false);
        SetOpen(true);
    }

    public void Hide()
    {
        SetOpen(false);
    }

    private void OnConfirmClicked()
    {
        SetOpen(false);
        onConfirm?.Invoke();
    }

    private void OnCancelClicked()
    {
        SetOpen(false);
        onCancel?.Invoke();
    }

    private void SetOpen(bool open)
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(open);
        IsOpen = open;
    }
}
