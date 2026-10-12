using System.Text.RegularExpressions;
using UnityEngine;

public class FPSDropDown : DropDownManager
{
    private const string VSyncOptionText = "VSync";

    // In Start, damit Settings.Awake das Standard-Limit schon gesetzt hat.
    protected override void Start()
    {
        base.Start();
        ShowCurrentFrameRate();
    }

    public override void ValueChanged(int index)
    {
        base.ValueChanged(index);

        string optionText = _drop.options[index].text;

        if (IsVSyncOption(optionText))
        {
            Settings.Instance.EnableVSync();
            return;
        }

        int? fps = ParseFps(optionText);

        if (fps.HasValue)
        {
            Settings.Instance.ChangeFPSLock(fps.Value);
        }
        else
        {
            Settings.Instance.UnlockFPS();
        }
    }

    /// <summary>
    /// Das Dropdown zeigt die tatsächlich aktive Einstellung an
    /// (vorher stand dort "30", obwohl das Spiel mit 60 lief).
    /// </summary>
    private void ShowCurrentFrameRate()
    {
        int currentFps = Application.targetFrameRate;
        bool isVSyncActive = QualitySettings.vSyncCount > 0;

        for (int i = 0; i < _drop.options.Count; i++)
        {
            string optionText = _drop.options[i].text;
            int? optionFps = ParseFps(optionText);

            bool matches = IsVSyncOption(optionText)
                ? isVSyncActive
                : !isVSyncActive && (optionFps.HasValue
                    ? optionFps.Value == currentFps
                    : currentFps <= 0);

            if (matches)
            {
                _drop.SetValueWithoutNotify(i);
                return;
            }
        }
    }

    private static bool IsVSyncOption(string optionText)
    {
        return optionText.IndexOf(VSyncOptionText, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <returns>Die Zahl aus dem Text (z.B. "60"), oder null bei "Unlocked".</returns>
    private static int? ParseFps(string optionText)
    {
        Match match = Regex.Match(optionText, @"\d+");

        return match.Success ? int.Parse(match.Value) : null;
    }
}
