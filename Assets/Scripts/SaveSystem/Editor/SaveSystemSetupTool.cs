using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Menü "Tools → Save System": richtet Szenen für das Savesystem ein.
/// </summary>
public static class SaveSystemSetupTool
{
    private const string MenuRoot = "Tools/Save System/";
    private const string RegistryFolder = "Assets/Resources";
    private const string RegistryPath = RegistryFolder + "/" + SaveRegistry.ResourcePath + ".asset";

    #region Setup Open Scene

    [MenuItem(MenuRoot + "1. Setup Open Scene", priority = 1)]
    public static void SetupOpenScenes()
    {
        SaveRegistry registry = GetOrCreateRegistry();
        registry.RefreshFromProject();
        AssetDatabase.SaveAssets();

        int addedEntities = 0;

        foreach (Scene scene in GetLoadedScenes())
        {
            foreach (GameObject target in FindObjectsThatNeedSaving(scene))
            {
                if (target.GetComponent<SaveableEntity>() != null)
                {
                    continue;
                }

                Undo.AddComponent<SaveableEntity>(target);
                addedEntities++;
            }
        }

        int newIds = AssignUniqueIds();

        foreach (Scene scene in GetLoadedScenes())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        string message =
            $"SaveableEntity hinzugefügt: {addedEntities}\n" +
            $"Neue / reparierte IDs: {newIds}\n" +
            $"Registry: {CountItems(registry)}\n\n" +
            "Szene jetzt speichern (Strg+S).";

        Debug.Log("[Save Setup] " + message.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Save System Setup", message, "OK");
    }

    private static IEnumerable<GameObject> FindObjectsThatNeedSaving(Scene scene)
    {
        HashSet<GameObject> targets = new();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue; // fehlendes Script
                }

                Type type = behaviour.GetType();

                if (behaviour is ISaveable || type.IsDefined(typeof(SaveActiveStateAttribute), true))
                {
                    targets.Add(behaviour.gameObject);
                }

                foreach (GameObject referenced in GetMarkedGameObjectFields(behaviour))
                {
                    if (referenced.scene == scene)
                    {
                        targets.Add(referenced);
                    }
                }
            }
        }

        return targets;
    }

    /// <summary>Alle GameObject-Felder mit [SaveActiveState] (auch private und aus Basisklassen).</summary>
    private static IEnumerable<GameObject> GetMarkedGameObjectFields(MonoBehaviour behaviour)
    {
        const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        for (Type type = behaviour.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
        {
            foreach (FieldInfo field in type.GetFields(Flags))
            {
                if (field.FieldType == typeof(GameObject) &&
                    field.IsDefined(typeof(SaveActiveStateAttribute), true) &&
                    field.GetValue(behaviour) is GameObject referenced &&
                    referenced != null)
                {
                    yield return referenced;
                }
            }
        }
    }

    private static int AssignUniqueIds()
    {
        HashSet<string> usedIds = new();
        int changed = 0;

        foreach (Scene scene in GetLoadedScenes())
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (SaveableEntity entity in root.GetComponentsInChildren<SaveableEntity>(true))
                {
                    if (entity.EnsureUniqueId(usedIds))
                    {
                        changed++;
                    }
                }
            }
        }

        return changed;
    }

    private static SaveRegistry GetOrCreateRegistry()
    {
        SaveRegistry registry = AssetDatabase.LoadAssetAtPath<SaveRegistry>(RegistryPath);

        if (registry != null)
        {
            return registry;
        }

        if (!AssetDatabase.IsValidFolder(RegistryFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        registry = ScriptableObject.CreateInstance<SaveRegistry>();
        AssetDatabase.CreateAsset(registry, RegistryPath);

        return registry;
    }

    private static string CountItems(SaveRegistry registry)
    {
        SerializedObject serialized = new(registry);

        return $"{serialized.FindProperty("items").arraySize} Items, " +
               $"{serialized.FindProperty("quests").arraySize} Quests";
    }

    private static IEnumerable<Scene> GetLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded)
            {
                yield return scene;
            }
        }
    }

    #endregion

    #region Create Save Menu

    [MenuItem(MenuRoot + "2. Create Save Menu In Scene", priority = 2)]
    public static void CreateSaveMenuInScene()
    {
        SaveSlotMenu existingMenu = Object.FindAnyObjectByType<SaveSlotMenu>(FindObjectsInactive.Include);

        if (existingMenu != null)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "Save System",
                "Es gibt bereits ein Save-Menü in der Szene.\n" +
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

        SaveSlotMenu menu = SaveMenuBuilder.Build();
        Undo.RegisterCreatedObjectUndo(menu.gameObject, "Create Save Menu");

        ConnectMenuToSceneManagers(menu);

        Selection.activeGameObject = menu.gameObject;
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

        EditorUtility.DisplayDialog(
            "Save System",
            "Save-Menü erstellt und mit Pause-/Hauptmenü verbunden.\n\n" +
            "Als Nächstes: einen bestehenden Button im Menü auswählen und\n" +
            "'Tools → Save System → 3. Add Menu Buttons' ausführen.",
            "OK");
    }

    private static void ConnectMenuToSceneManagers(SaveSlotMenu menu)
    {
        AssignField<PauseMenu>("_saveSlotMenu", menu);
        AssignField<MainMenuManager>("_saveSlotMenu", menu);
        AssignField<DeathScreenManager>("_saveSlotMenu", menu);
    }

    private static void AssignField<TOwner>(string fieldName, Object value) where TOwner : Object
    {
        TOwner owner = Object.FindAnyObjectByType<TOwner>(FindObjectsInactive.Include);

        if (owner == null)
        {
            return;
        }

        SerializedObject serialized = new(owner);
        serialized.FindProperty(fieldName).objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    #endregion

    #region Add Menu Buttons

    [MenuItem(MenuRoot + "3. Add Menu Buttons (select an existing button)", priority = 3)]
    public static void AddMenuButtons()
    {
        GameObject template = Selection.activeGameObject;

        if (template == null || template.GetComponent<Button>() == null)
        {
            EditorUtility.DisplayDialog(
                "Save System",
                "Bitte zuerst einen bestehenden Button in deinem Pause-Menü oder Hauptmenü auswählen.\n" +
                "Er wird als Vorlage kopiert (gleiches Aussehen).",
                "OK");
            return;
        }

        PauseMenu pauseMenu = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        MainMenuManager mainMenu = Object.FindAnyObjectByType<MainMenuManager>(FindObjectsInactive.Include);

        if (pauseMenu != null)
        {
            CreateButtonFrom(template, "Save", pauseMenu.OpenSaveMenu, 1);
            CreateButtonFrom(template, "Load", pauseMenu.OpenLoadMenu, 2);
        }
        else if (mainMenu != null)
        {
            Button continueButton = CreateButtonFrom(template, "Continue", mainMenu.ContinueGame, 1);
            CreateButtonFrom(template, "Load", mainMenu.OpenLoadMenu, 2);

            AssignField<MainMenuManager>("_continueButton", continueButton);
        }
        else
        {
            EditorUtility.DisplayDialog("Save System", "Weder PauseMenu noch MainMenuManager in der Szene gefunden.", "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(template.scene);
    }

    /// <summary>Kopiert den Vorlagen-Button, ändert die Beschriftung und verknüpft den Klick.</summary>
    private static Button CreateButtonFrom(GameObject template, string label, UnityAction onClick, int offset)
    {
        GameObject copy = Object.Instantiate(template, template.transform.parent);
        copy.name = label + "Button";
        copy.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + offset);
        Undo.RegisterCreatedObjectUndo(copy, "Add " + label + " Button");

        SetButtonLabel(copy, label);

        Button button = copy.GetComponent<Button>();

        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        UnityEventTools.AddPersistentListener(button.onClick, onClick);

        return button;
    }

    private static void SetButtonLabel(GameObject button, string label)
    {
        TMPro.TMP_Text tmpText = button.GetComponentInChildren<TMPro.TMP_Text>(true);

        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);

        if (legacyText != null)
        {
            legacyText.text = label;
        }
    }

    #endregion

    #region Utilities

    [MenuItem(MenuRoot + "Open Save Folder", priority = 20)]
    public static void OpenSaveFolder()
    {
        Directory.CreateDirectory(SaveFileStorage.SaveFolder);
        EditorUtility.RevealInFinder(SaveFileStorage.SaveFolder);
    }

    [MenuItem(MenuRoot + "Delete All Saves", priority = 21)]
    public static void DeleteAllSaves()
    {
        if (!EditorUtility.DisplayDialog("Save System", "Alle Spielstände löschen?", "Löschen", "Abbrechen"))
        {
            return;
        }

        for (int slot = 0; slot < SaveManager.SlotCount; slot++)
        {
            SaveFileStorage.Delete(slot);
        }

        Debug.Log("[Save] Alle Spielstände gelöscht.");
    }

    #endregion
}
