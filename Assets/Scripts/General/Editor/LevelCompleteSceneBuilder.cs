using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Baut die Szene "Level abgeschlossen" mit demselben Stil wie das Save-Menü (<see cref="SaveMenuBuilder"/>)
/// und trägt sie (und das nächste Level) in die Build Settings ein. Danach frei anpassbar.
/// </summary>
public static class LevelCompleteSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MenuScreens/LevelCompleteScene.unity";
    private const string NextLevelPath = "Assets/Scenes/Levels/Level 2.unity";

    // Schmale Buttons wie im Todes-Bildschirm statt über die ganze Fensterbreite.
    private const float ButtonWidth = 400f;
    private const float ButtonHeight = 60f;

    [MenuItem("Tools/Level Complete/Create Level Complete Scene")]
    public static void CreateLevelCompleteScene()
    {
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog(
                "Level Complete",
                "Die Szene gibt es bereits.\nNeu erstellen? (Eigene Änderungen daran gehen verloren.)",
                "Neu erstellen",
                "Abbrechen"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();
        Build();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath, NextLevelPath);

        EditorUtility.DisplayDialog(
            "Level Complete",
            $"Szene erstellt: {ScenePath}\n\n" +
            "LevelCompleteScene und Level 2 sind jetzt in den Build Settings.",
            "OK");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static void Build()
    {
        SaveMenuBuilder.LoadStyle();

        GameObject root = SaveMenuBuilder.CreateCanvasRoot("LevelCompleteScreen");
        LevelCompleteScreen screen = root.AddComponent<LevelCompleteScreen>();

        GameObject window = SaveMenuBuilder.CreateWindow(root.transform, new Vector2(1250f, 720f));

        VerticalLayoutGroup layout = window.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = false;

        TMP_Text title = SaveMenuBuilder.CreateTitle(window.transform, "Level 1 erfolgreich abgeschlossen!");

        TMP_Text time = SaveMenuBuilder.CreateText(
            "Time", window.transform, "Zeit: 0:00:00", 28, TextAlignmentOptions.Center, 90f);
        time.color = SaveMenuBuilder.InfoTextColor;

        TMP_Text difficulty = SaveMenuBuilder.CreateText(
            "Difficulty", window.transform, "Schwierigkeit: Mittel", 24, TextAlignmentOptions.Center, 80f);
        difficulty.color = SaveMenuBuilder.InfoTextColor;
        difficulty.GetComponent<LayoutElement>().flexibleWidth = 1f;

        TMP_Text stats = SaveMenuBuilder.CreateText(
            "Stats", window.transform, "Items: 0 / 0", 18, TextAlignmentOptions.Center, 80f);
        stats.color = SaveMenuBuilder.InfoTextColor;
        stats.GetComponent<LayoutElement>().flexibleWidth = 1f;

        // Texte füllen weiterhin die Fensterbreite (und brechen um), nur die Buttons bleiben schmal.
        title.GetComponent<LayoutElement>().flexibleWidth = 1f;
        time.GetComponent<LayoutElement>().flexibleWidth = 1f;

        Button continueButton = SaveMenuBuilder.CreateButton(
            "ContinueButton", window.transform, "Weiter", SaveMenuBuilder.ButtonTextColor, ButtonWidth, ButtonHeight);
        UnityEventTools.AddPersistentListener(continueButton.onClick, screen.Continue);

        Button menuButton = SaveMenuBuilder.CreateButton(
            "MainMenuButton", window.transform, "Hauptmenü", SaveMenuBuilder.ButtonTextColor, ButtonWidth, ButtonHeight);
        UnityEventTools.AddPersistentListener(menuButton.onClick, screen.GoToMainMenu);

        SerializedObject serialized = new(screen);
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("timeText").objectReferenceValue = time;
        serialized.FindProperty("difficultyText").objectReferenceValue = difficulty;
        serialized.FindProperty("statsText").objectReferenceValue = stats;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddToBuildSettings(params string[] scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        foreach (string path in scenePaths)
        {
            if (File.Exists(path) && scenes.All(scene => scene.path != path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
