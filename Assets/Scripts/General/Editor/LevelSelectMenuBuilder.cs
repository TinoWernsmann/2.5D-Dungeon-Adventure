using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Baut die Level-Auswahl als normale UI-Objekte in der Szene – mit denselben Bausteinen
/// und demselben Stil wie das Save-Menü (<see cref="SaveMenuBuilder"/>). Danach frei anpassbar.
/// </summary>
public static class LevelSelectMenuBuilder
{
    private const float ButtonWidth = 520f;
    private const float ButtonHeight = 60f;

    [MenuItem("Tools/Level Select/Create Level Select Menu In Scene")]
    public static void CreateLevelSelectMenuInScene()
    {
        LevelSelectMenu existingMenu = Object.FindAnyObjectByType<LevelSelectMenu>(FindObjectsInactive.Include);

        if (existingMenu != null)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "Level Select",
                "Es gibt bereits eine Level-Auswahl in der Szene.\n" +
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

        LevelSelectMenu menu = Build();
        Undo.RegisterCreatedObjectUndo(menu.gameObject, "Create Level Select Menu");

        SaveSystemSetupTool.AssignField<MainMenuManager>("_levelSelectMenu", menu);

        Selection.activeGameObject = menu.gameObject;
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

        EditorUtility.DisplayDialog(
            "Level Select",
            "Level-Auswahl erstellt und mit dem Hauptmenü verbunden.\n\n" +
            "Jetzt beim Button 'Load Level' unter On Click () eintragen:\n" +
            "MainMenuManager → OpenLevelSelect",
            "OK");
    }

    private static LevelSelectMenu Build()
    {
        SaveMenuBuilder.LoadStyle();

        GameObject root = SaveMenuBuilder.CreateCanvasRoot("LevelSelectMenu");
        LevelSelectMenu menu = root.AddComponent<LevelSelectMenu>();

        GameObject panel = SaveMenuBuilder.CreateStretched("Panel", root.transform, SaveMenuBuilder.OverlayColor);
        GameObject window = SaveMenuBuilder.CreateWindow(panel.transform, new Vector2(800f, 760f));

        VerticalLayoutGroup windowLayout = window.GetComponent<VerticalLayoutGroup>();
        windowLayout.childAlignment = TextAnchor.UpperCenter;
        windowLayout.childForceExpandWidth = false;

        SaveMenuBuilder.CreateTitle(window.transform, "Level")
            .GetComponent<LayoutElement>().flexibleWidth = 1f;

        // Eigener Container, damit die Level-Buttons zwischen Titel und "Back" erscheinen.
        GameObject levels = new("Levels", typeof(RectTransform));
        levels.transform.SetParent(window.transform, false);
        SaveMenuBuilder.AddLayoutElement(levels, flexibleWidth: 1f);
        levels.GetComponent<LayoutElement>().flexibleHeight = 1f;

        VerticalLayoutGroup levelsLayout = levels.AddComponent<VerticalLayoutGroup>();
        levelsLayout.spacing = 15f;
        levelsLayout.childAlignment = TextAnchor.UpperCenter;
        levelsLayout.childControlWidth = true;
        levelsLayout.childControlHeight = true;
        levelsLayout.childForceExpandWidth = false;
        levelsLayout.childForceExpandHeight = false;

        Button template = SaveMenuBuilder.CreateButton(
            "LevelButtonTemplate", levels.transform, "Level", SaveMenuBuilder.ButtonTextColor, ButtonWidth, ButtonHeight);

        Button closeButton = SaveMenuBuilder.CreateButton(
            "CloseButton", window.transform, "Back", SaveMenuBuilder.ButtonTextColor, ButtonWidth, 55f);

        SerializedObject serialized = new(menu);
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("buttonParent").objectReferenceValue = levels.transform;
        serialized.FindProperty("buttonTemplate").objectReferenceValue = template;
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);

        return menu;
    }
}
