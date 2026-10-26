using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Baut die Erfolgs-Liste als normale UI-Objekte in der Szene – mit denselben Bausteinen
/// und demselben Stil wie das Save-Menü (<see cref="SaveMenuBuilder"/>). Danach frei anpassbar.
/// </summary>
public static class AchievementMenuBuilder
{
    [MenuItem("Tools/Achievements/Create Achievement Menu In Scene")]
    public static void CreateAchievementMenuInScene()
    {
        AchievementMenu existingMenu = Object.FindAnyObjectByType<AchievementMenu>(FindObjectsInactive.Include);

        if (existingMenu != null)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "Achievements",
                "Es gibt bereits eine Erfolgs-Liste in der Szene.\n" +
                "Neu erstellen? (Eigene Änderungen daran gehen verloren.)",
                "Neu erstellen",
                "Abbrechen");

            if (!rebuild)
            {
                Selection.activeGameObject = existingMenu.gameObject;
                return;
            }

            Undo.DestroyObjectImmediate(existingMenu.gameObject);
        }

        AchievementMenu menu = Build();
        Undo.RegisterCreatedObjectUndo(menu.gameObject, "Create Achievement Menu");

        SaveSystemSetupTool.AssignField<PauseMenu>("_achievementMenu", menu);
        SaveSystemSetupTool.AssignField<MainMenuManager>("_achievementMenu", menu);

        Selection.activeGameObject = menu.gameObject;
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

        EditorUtility.DisplayDialog(
            "Achievements",
            "Erfolgs-Liste erstellt und mit Pause-/Hauptmenü verbunden.\n\n" +
            "Jetzt beim Button 'Erfolge' unter On Click () eintragen:\n" +
            "Pause-Menü: PauseMenu → OpenAchievementMenu\n" +
            "Hauptmenü: MainMenuManager → OpenAchievementMenu",
            "OK");
    }

    private static AchievementMenu Build()
    {
        SaveMenuBuilder.LoadStyle();

        GameObject root = SaveMenuBuilder.CreateCanvasRoot("AchievementMenu");
        AchievementMenu menu = root.AddComponent<AchievementMenu>();

        GameObject panel = SaveMenuBuilder.CreateStretched("Panel", root.transform, SaveMenuBuilder.OverlayColor);
        GameObject window = SaveMenuBuilder.CreateWindow(panel.transform, new Vector2(1250f, 820f));

        SaveMenuBuilder.CreateTitle(window.transform, "Erfolge");

        TMP_Text header = SaveMenuBuilder.CreateText(
            "Header", window.transform, "0 / 0 freigeschaltet", 20, TextAlignmentOptions.Center, 30f);
        header.color = SaveMenuBuilder.InfoTextColor;

        Transform rowParent = SaveMenuBuilder.CreateScrollList(window.transform);
        AchievementRow rowTemplate = CreateRow(rowParent);

        Button closeButton = SaveMenuBuilder.CreateButton(
            "CloseButton", window.transform, "Back", SaveMenuBuilder.ButtonTextColor, 0f, 55f);

        SerializedObject serialized = new(menu);
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("headerText").objectReferenceValue = header;
        serialized.FindProperty("rowParent").objectReferenceValue = rowParent;
        serialized.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);

        return menu;
    }

    /// <summary>Vorlage für eine Zeile – das Menü klont sie für jeden Erfolg.</summary>
    private static AchievementRow CreateRow(Transform parent)
    {
        GameObject row = SaveMenuBuilder.CreateImage("RowTemplate", parent, SaveMenuBuilder.RowColor);
        SaveMenuBuilder.AddLayoutElement(row, preferredHeight: 130f);
        CanvasGroup canvasGroup = row.AddComponent<CanvasGroup>();

        VerticalLayoutGroup layout = row.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(25, 25, 18, 18);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text title = SaveMenuBuilder.CreateText("Title", row.transform, "???", 26, TextAlignmentOptions.Left, 36f);

        TMP_Text description = SaveMenuBuilder.CreateText(
            "Description", row.transform, "Verborgener Erfolg", 18, TextAlignmentOptions.Left, 26f);
        description.color = SaveMenuBuilder.InfoTextColor;

        TMP_Text date = SaveMenuBuilder.CreateText("Date", row.transform, string.Empty, 16, TextAlignmentOptions.Left, 22f);
        date.color = SaveMenuBuilder.InfoTextColor;

        AchievementRow view = row.AddComponent<AchievementRow>();

        SerializedObject serialized = new(view);
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("descriptionText").objectReferenceValue = description;
        serialized.FindProperty("dateText").objectReferenceValue = date;
        serialized.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return view;
    }
}
