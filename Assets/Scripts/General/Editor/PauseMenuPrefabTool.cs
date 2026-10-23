using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Macht aus dem Pause-Menü einer Szene ein Prefab (inkl. Save-, Schwierigkeits- und Erfolgs-Menü)
/// und tauscht es in anderen Szenen gegen das alte Pause-Menü aus.
/// Danach wird das Pause-Menü nur noch im Prefab bearbeitet – alle Szenen übernehmen die Änderungen.
/// </summary>
public static class PauseMenuPrefabTool
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string PrefabPath = PrefabFolder + "/PauseMenu.prefab";
    private const string Title = "Pause Menu";

    // Felder im PauseMenu, die auf die zugehörigen Menüs zeigen.
    private static readonly string[] SubMenuFields = { "_saveSlotMenu", "_difficultyMenu", "_achievementMenu" };

    [MenuItem("Tools/Pause Menu/1. Create Prefab From Open Scene")]
    public static void CreatePrefabFromOpenScene()
    {
        PauseMenu pauseMenu = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);

        if (pauseMenu == null)
        {
            EditorUtility.DisplayDialog(Title, "In dieser Szene gibt es kein PauseMenu.", "OK");
            return;
        }

        if (PrefabUtility.IsPartOfPrefabInstance(pauseMenu))
        {
            EditorUtility.DisplayDialog(Title, "Das Pause-Menü ist hier bereits ein Prefab.", "OK");
            Selection.activeObject = PrefabUtility.GetOutermostPrefabInstanceRoot(pauseMenu);
            return;
        }

        if (!IsStandaloneHost(pauseMenu))
        {
            EditorUtility.DisplayDialog(
                Title,
                $"Das PauseMenu-Script liegt auf '{pauseMenu.name}', das noch andere Komponenten oder Kinder hat.\n" +
                "Bitte das Script auf ein eigenes, leeres GameObject legen und erneut versuchen.",
                "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
            !EditorUtility.DisplayDialog(Title, $"{PrefabPath} gibt es bereits. Überschreiben?", "Überschreiben", "Abbrechen"))
        {
            return;
        }

        GameObject panel = GetPanel(pauseMenu);

        if (panel == null)
        {
            EditorUtility.DisplayDialog(Title, "Im PauseMenu ist kein 'Pause Menu'-Panel zugewiesen.", "OK");
            return;
        }

        GameObject root = CreateRoot(panel.GetComponentInParent<Canvas>(true));

        // Reihenfolge = Zeichenreihenfolge: Panel unten, die anderen Menüs darüber.
        Undo.SetTransformParent(pauseMenu.transform, root.transform, false, "Move Pause Menu");
        Undo.SetTransformParent(panel.transform, root.transform, false, "Move Pause Menu");

        foreach (GameObject subMenu in GetSubMenus(pauseMenu))
        {
            Undo.SetTransformParent(subMenu.transform, root.transform, false, "Move Pause Menu");
            StretchToParent(subMenu);
        }

        EnsureFolder(PrefabFolder);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);

        EditorUtility.DisplayDialog(
            Title,
            $"Prefab erstellt: {PrefabPath}\n\n" +
            "Szene jetzt speichern (Strg+S).\n" +
            "In den anderen Szenen: Tools → Pause Menu → 2. Replace Pause Menu In Open Scene.",
            "OK");
    }

    [MenuItem("Tools/Pause Menu/2. Replace Pause Menu In Open Scene")]
    public static void ReplacePauseMenuInOpenScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null)
        {
            EditorUtility.DisplayDialog(Title, "Es gibt noch kein Prefab. Zuerst Schritt 1 in einer fertigen Szene ausführen.", "OK");
            return;
        }

        PauseMenu oldMenu = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);

        if (oldMenu != null && PrefabUtility.GetCorrespondingObjectFromOriginalSource(oldMenu.gameObject) != null)
        {
            EditorUtility.DisplayDialog(Title, "In dieser Szene ist das Prefab bereits eingesetzt.", "OK");
            Selection.activeObject = PrefabUtility.GetOutermostPrefabInstanceRoot(oldMenu);
            return;
        }

        if (oldMenu != null && !ConfirmRemoval(oldMenu))
        {
            return;
        }

        if (oldMenu != null)
        {
            RemoveOldPauseMenu(oldMenu);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Insert Pause Menu Prefab");

        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);

        EditorUtility.DisplayDialog(Title, "Pause-Menü-Prefab eingesetzt. Szene jetzt speichern (Strg+S).", "OK");
    }

    private static bool ConfirmRemoval(PauseMenu oldMenu)
    {
        List<string> names = new() { oldMenu.name };

        GameObject panel = GetPanel(oldMenu);
        if (panel != null) names.Add(panel.name);

        foreach (GameObject subMenu in GetSubMenus(oldMenu)) names.Add(subMenu.name);

        return EditorUtility.DisplayDialog(
            Title,
            "Das alte Pause-Menü wird entfernt und durch das Prefab ersetzt:\n\n" +
            string.Join("\n", names) +
            "\n\n(Rückgängig mit Strg+Z.)",
            "Ersetzen",
            "Abbrechen");
    }

    private static void RemoveOldPauseMenu(PauseMenu oldMenu)
    {
        GameObject panel = GetPanel(oldMenu);
        List<GameObject> subMenus = GetSubMenus(oldMenu);

        if (panel != null) Undo.DestroyObjectImmediate(panel);

        foreach (GameObject subMenu in subMenus)
        {
            if (subMenu != null) Undo.DestroyObjectImmediate(subMenu);
        }

        // Liegt das Script auf einem Objekt mit anderen Aufgaben, nur das Script entfernen.
        if (IsStandaloneHost(oldMenu))
        {
            Undo.DestroyObjectImmediate(oldMenu.gameObject);
        }
        else
        {
            Undo.DestroyObjectImmediate(oldMenu);
        }
    }

    /// <summary>Eigene Canvas mit denselben Skalierungs-Einstellungen wie die bisherige UI, über dem HUD.</summary>
    private static GameObject CreateRoot(Canvas sourceCanvas)
    {
        GameObject root = new("PauseMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create Pause Menu Prefab");

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        Canvas rootCanvas = sourceCanvas != null ? sourceCanvas.rootCanvas : null;
        canvas.sortingOrder = rootCanvas != null ? rootCanvas.sortingOrder + 10 : 50;

        if (rootCanvas != null && rootCanvas.TryGetComponent(out CanvasScaler sourceScaler))
        {
            EditorUtility.CopySerialized(sourceScaler, root.GetComponent<CanvasScaler>());
        }

        return root;
    }

    /// <summary>Die Menüs waren eigene Canvas-Wurzeln – als Kind müssen sie den ganzen Bildschirm füllen.</summary>
    private static void StretchToParent(GameObject gameObject)
    {
        if (gameObject.transform is not RectTransform rect)
        {
            return;
        }

        Undo.RecordObject(rect, "Stretch Menu");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static bool IsStandaloneHost(PauseMenu pauseMenu)
    {
        return pauseMenu.transform.childCount == 0 &&
               pauseMenu.GetComponents<Component>().Length == 2; // Transform + PauseMenu
    }

    private static GameObject GetPanel(PauseMenu pauseMenu)
    {
        return new SerializedObject(pauseMenu).FindProperty("_pauseMenu").objectReferenceValue as GameObject;
    }

    private static List<GameObject> GetSubMenus(PauseMenu pauseMenu)
    {
        SerializedObject serialized = new(pauseMenu);
        List<GameObject> subMenus = new();

        foreach (string field in SubMenuFields)
        {
            if (serialized.FindProperty(field).objectReferenceValue is Component menu)
            {
                subMenus.Add(menu.gameObject);
            }
        }

        return subMenus;
    }

    private static void EnsureFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }
    }
}
