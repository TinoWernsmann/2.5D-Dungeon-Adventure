using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Baut das Schwierigkeits-Menü als normale UI-Objekte in der Szene – mit denselben Bausteinen
/// und demselben Stil wie das Save-Menü (<see cref="SaveMenuBuilder"/>). Danach frei anpassbar.
/// </summary>
public static class DifficultyMenuBuilder
{
    private static readonly string[] Names = { "Easy", "Normal", "Hard" };

    [MenuItem("Tools/Difficulty/Create Difficulty Menu In Scene")]
    public static void CreateDifficultyMenuInScene()
    {
        DifficultyMenu existingMenu = Object.FindAnyObjectByType<DifficultyMenu>(FindObjectsInactive.Include);

        if (existingMenu != null)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "Difficulty",
                "Es gibt bereits ein Schwierigkeits-Menü in der Szene.\n" +
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

        DifficultyMenu menu = Build();
        Undo.RegisterCreatedObjectUndo(menu.gameObject, "Create Difficulty Menu");

        SaveSystemSetupTool.AssignField<PauseMenu>("_difficultyMenu", menu);
        SaveSystemSetupTool.AssignField<MainMenuManager>("_difficultyMenu", menu);

        Selection.activeGameObject = menu.gameObject;
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

        EditorUtility.DisplayDialog(
            "Difficulty",
            "Schwierigkeits-Menü erstellt und mit Pause-/Hauptmenü verbunden.\n\n" +
            "Jetzt beim Button 'Change Difficulty' unter On Click () eintragen:\n" +
            "Pause-Menü: PauseMenu → OpenDifficultyMenu\n" +
            "Hauptmenü: MainMenuManager → OpenDifficultyMenu",
            "OK");
    }

    private static DifficultyMenu Build()
    {
        SaveMenuBuilder.LoadStyle();

        GameObject root = SaveMenuBuilder.CreateCanvasRoot("DifficultyMenu");
        DifficultyMenu menu = root.AddComponent<DifficultyMenu>();

        GameObject panel = SaveMenuBuilder.CreateStretched("Panel", root.transform, SaveMenuBuilder.OverlayColor);
        GameObject window = SaveMenuBuilder.CreateWindow(panel.transform, new Vector2(800f, 620f));

        SaveMenuBuilder.CreateTitle(window.transform, "Difficulty");

        Button[] buttons = new Button[Names.Length];

        for (int i = 0; i < Names.Length; i++)
        {
            buttons[i] = SaveMenuBuilder.CreateButton(
                $"{Names[i]}Button", window.transform, Names[i], SaveMenuBuilder.ButtonTextColor, 0f, 70f);
        }

        TMP_Text description = SaveMenuBuilder.CreateText(
            "Description", window.transform, string.Empty, 20, TextAlignmentOptions.Center, 70f);
        description.color = SaveMenuBuilder.InfoTextColor;

        Button closeButton = SaveMenuBuilder.CreateButton(
            "CloseButton", window.transform, "Back", SaveMenuBuilder.ButtonTextColor, 0f, 55f);

        SerializedObject serialized = new(menu);
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("descriptionText").objectReferenceValue = description;
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;

        SerializedProperty buttonsProperty = serialized.FindProperty("difficultyButtons");
        buttonsProperty.arraySize = buttons.Length;

        for (int i = 0; i < buttons.Length; i++)
        {
            buttonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);

        return menu;
    }
}
