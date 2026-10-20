using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menü mit allen Spielstand-Slots. Im Modus "Save" wird gespeichert, im Modus "Load" geladen.
/// In beiden Modi kann gelöscht werden. Überschreiben, Löschen und Laden während des Spiels
/// werden vorher bestätigt.
///
/// Wird per "Tools → Save System → Create Save Menu In Scene" erstellt.
/// </summary>
public class SaveSlotMenu : MonoBehaviour
{
    public enum Mode
    {
        Save,
        Load
    }

    [Header("Menu")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private SaveSlotView[] slotViews;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button closeButton;

    [Header("Confirm Dialog")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    private Mode mode;
    private Action pendingConfirmAction;

    /// <summary>Das Menü wurde geschlossen (z.B. um das Pause-Menü wieder zu zeigen).</summary>
    public event Action Closed;

    public bool IsOpen => panel.activeSelf;

    private void Awake()
    {
        foreach (SaveSlotView slotView in slotViews)
        {
            slotView.ActionClicked += HandleActionClicked;
            slotView.DeleteClicked += HandleDeleteClicked;
        }

        closeButton.onClick.AddListener(Close);
        confirmYesButton.onClick.AddListener(ConfirmPendingAction);
        confirmNoButton.onClick.AddListener(HideConfirm);

        panel.SetActive(false);
        HideConfirm();
    }

    private void OnEnable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SlotsChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SlotsChanged -= Refresh;
        }
    }

    public void Open(Mode menuMode)
    {
        mode = menuMode;

        titleText.text = mode == Mode.Save ? "Save Game" : "Load Game";
        feedbackText.text = string.Empty;

        panel.SetActive(true);
        HideConfirm();
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        HideConfirm();
        panel.SetActive(false);

        Closed?.Invoke();
    }

    private void Refresh()
    {
        int[] slots = GetDisplayOrder();

        for (int i = 0; i < slotViews.Length; i++)
        {
            bool hasSlot = i < slots.Length;
            slotViews[i].gameObject.SetActive(hasSlot);

            if (!hasSlot)
            {
                continue;
            }

            SaveManager.Instance.TryGetSlotInfo(slots[i], out SaveGameData info);
            slotViews[i].Show(slots[i], info, mode);
        }
    }

    /// <summary>Erst die manuellen Slots, dann die Autosaves – neuester oben, leere unten.</summary>
    private static int[] GetDisplayOrder()
    {
        IEnumerable<int> autoSaves = Enumerable
            .Range(SaveManager.SlotCount, SaveManager.AutoSaveSlotCount)
            .OrderByDescending(slot =>
                SaveManager.Instance.TryGetSlotInfo(slot, out SaveGameData info) ? info.SavedAt : DateTime.MinValue);

        return Enumerable.Range(0, SaveManager.SlotCount).Concat(autoSaves).ToArray();
    }

    #region Slot Actions

    private void HandleActionClicked(int slot)
    {
        if (mode == Mode.Save)
        {
            RequestSave(slot);
        }
        else
        {
            RequestLoad(slot);
        }
    }

    private void RequestSave(int slot)
    {
        if (SaveManager.IsAutoSaveSlot(slot))
        {
            return;
        }

        if (!SaveManager.Instance.HasSave(slot))
        {
            Save(slot);
            return;
        }

        ShowConfirm($"Overwrite slot {slot + 1}?", () => Save(slot));
    }

    private void Save(int slot)
    {
        bool saved = SaveManager.Instance.Save(slot);

        feedbackText.text = saved
            ? $"Saved in slot {slot + 1}."
            : "Saving is not possible right now.";
    }

    private void RequestLoad(int slot)
    {
        if (!SaveManager.Instance.HasSave(slot))
        {
            return;
        }

        // Im laufenden Spiel geht ungespeicherter Fortschritt verloren -> nachfragen.
        if (SaveManager.Instance.CanSave)
        {
            ShowConfirm($"Load {SlotName(slot)}?\nUnsaved progress will be lost.", () => Load(slot));
            return;
        }

        Load(slot);
    }

    private void Load(int slot)
    {
        if (!SaveManager.Instance.Load(slot))
        {
            feedbackText.text = "This save could not be loaded.";
        }
    }

    private void HandleDeleteClicked(int slot)
    {
        ShowConfirm($"Delete {SlotName(slot)}?", () =>
        {
            SaveManager.Instance.Delete(slot);
            feedbackText.text = $"{SlotName(slot)} deleted.";
        });
    }

    private static string SlotName(int slot)
    {
        return SaveManager.IsAutoSaveSlot(slot) ? "auto save" : $"slot {slot + 1}";
    }

    #endregion

    #region Confirm Dialog

    private void ShowConfirm(string message, Action onConfirm)
    {
        pendingConfirmAction = onConfirm;
        confirmText.text = message;
        confirmPanel.SetActive(true);
    }

    private void ConfirmPendingAction()
    {
        Action action = pendingConfirmAction;

        HideConfirm();
        action?.Invoke();
    }

    private void HideConfirm()
    {
        pendingConfirmAction = null;
        confirmPanel.SetActive(false);
    }

    #endregion
}
