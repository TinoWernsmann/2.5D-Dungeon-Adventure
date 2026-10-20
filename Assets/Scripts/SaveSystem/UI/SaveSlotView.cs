using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Eine Zeile im Slot-Menü: Titel, Infos (Datum, Spielzeit, Quest) und Buttons.</summary>
public class SaveSlotView : MonoBehaviour
{
    private const string EmptySlotText = "- Empty -";
    private const string SaveLabel = "Save";
    private const string LoadLabel = "Load";
    private const string AutoSaveTitle = "Auto Save";

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private Button deleteButton;

    private int slot;

    public event Action<int> ActionClicked;
    public event Action<int> DeleteClicked;

    private void Awake()
    {
        actionButton.onClick.AddListener(() => ActionClicked?.Invoke(slot));
        deleteButton.onClick.AddListener(() => DeleteClicked?.Invoke(slot));
    }

    public void Show(int slotIndex, SaveGameData info, SaveSlotMenu.Mode mode)
    {
        slot = slotIndex;
        bool hasSave = info != null;
        bool isAutoSave = SaveManager.IsAutoSaveSlot(slotIndex);

        titleText.text = isAutoSave ? AutoSaveTitle : $"Slot {slotIndex + 1}";
        infoText.text = hasSave ? FormatInfo(info) : EmptySlotText;

        actionLabel.text = mode == SaveSlotMenu.Mode.Save ? SaveLabel : LoadLabel;

        // Laden geht nur, wenn es etwas zu laden gibt. Speichern geht immer – außer in Autosave-Slots.
        actionButton.interactable = mode == SaveSlotMenu.Mode.Save ? !isAutoSave : hasSave;
        deleteButton.interactable = hasSave;
    }

    private static string FormatInfo(SaveGameData info)
    {
        TimeSpan playTime = TimeSpan.FromSeconds(info.playTimeSeconds);
        string date = info.SavedAt.ToString("dd.MM.yyyy  HH:mm");
        string time = $"{(int)playTime.TotalHours}:{playTime.Minutes:00}:{playTime.Seconds:00}";

        return string.IsNullOrEmpty(info.questTitle)
            ? $"{date}   Time {time}"
            : $"{date}   Time {time}\n{info.questTitle}";
    }
}
