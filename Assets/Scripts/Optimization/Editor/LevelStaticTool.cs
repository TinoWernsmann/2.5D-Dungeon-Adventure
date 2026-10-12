using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Markiert die feste Level-Geometrie (Wände, Böden, Decken, Deko) als Static,
/// damit Unity Occlusion Culling backen kann: Objekte hinter Wänden werden dann nicht gerendert.
///
/// NICHT static werden Objekte, die sich bewegen, verschwinden oder von Scripts gesteuert werden:
///  - alles mit eigenen Scripts (Türen, Truhen, Kisten, Items, Gegner, Trigger, ...)
///  - alles mit Rigidbody, NavMeshAgent, Animator oder Animation
///  - alles, was ein Script in der Szene referenziert (z.B. Tore, die per SetActive verschwinden)
/// Wäre z.B. ein Tor als "Occluder" markiert, würde dahinter auch nach dem Öffnen nichts gerendert.
/// </summary>
public static class LevelStaticTool
{
    private const string MenuPath = "Tools/Optimization/Mark Level Geometry Static";

    private const StaticEditorFlags LevelFlags =
        StaticEditorFlags.OccluderStatic |
        StaticEditorFlags.OccludeeStatic |
        StaticEditorFlags.BatchingStatic |
        StaticEditorFlags.ReflectionProbeStatic;

    [MenuItem(MenuPath)]
    public static void MarkLevelGeometryStatic()
    {
        Scene scene = SceneManager.GetActiveScene();

        HashSet<GameObject> excludedRoots = FindDynamicObjects(scene);
        List<GameObject> targets = FindStaticCandidates(scene, excludedRoots);

        bool confirmed = EditorUtility.DisplayDialog(
            "Level Geometry Static",
            $"{targets.Count} Objekte werden als Static markiert " +
            $"(Occluder, Occludee, Batching).\n" +
            $"{excludedRoots.Count} bewegliche/gesteuerte Objekte werden ausgelassen.\n\n" +
            "Rückgängig mit Strg+Z.",
            "Markieren",
            "Abbrechen");

        if (!confirmed)
        {
            return;
        }

        Undo.RecordObjects(targets.ToArray(), "Mark Level Geometry Static");

        foreach (GameObject target in targets)
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(target);
            GameObjectUtility.SetStaticEditorFlags(target, flags | LevelFlags);
        }

        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"[Optimization] {targets.Count} Objekte als Static markiert.");
    }

    /// <summary>Objekte, die (samt Kindern) NICHT static werden dürfen.</summary>
    private static HashSet<GameObject> FindDynamicObjects(Scene scene)
    {
        HashSet<GameObject> dynamicObjects = new();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    continue; // fehlendes Script
                }

                if (IsDynamicComponent(component))
                {
                    dynamicObjects.Add(component.gameObject);
                }

                if (component is MonoBehaviour behaviour && IsGameScript(behaviour))
                {
                    AddReferencedGameObjects(behaviour, dynamicObjects);
                }
            }
        }

        return dynamicObjects;
    }

    private static bool IsDynamicComponent(Component component)
    {
        return component is Rigidbody ||
               component is NavMeshAgent ||
               component is Animator ||
               component is Animation ||
               component is CharacterController ||
               (component is MonoBehaviour behaviour && IsGameScript(behaviour));
    }

    /// <summary>Eigene Scripts des Spiels (nicht Unity-Komponenten wie UI oder Lichter).</summary>
    private static bool IsGameScript(MonoBehaviour behaviour)
    {
        return behaviour.GetType().Assembly.GetName().Name == "Assembly-CSharp";
    }

    /// <summary>Alles, was ein Script per GameObject-/Component-Feld referenziert, wird vermutlich gesteuert.</summary>
    private static void AddReferencedGameObjects(MonoBehaviour behaviour, HashSet<GameObject> result)
    {
        SerializedProperty property = new SerializedObject(behaviour).GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                continue;
            }

            GameObject referenced = property.objectReferenceValue switch
            {
                GameObject gameObject => gameObject,
                Component component => component.gameObject,
                _ => null
            };

            if (referenced != null && referenced.scene == behaviour.gameObject.scene)
            {
                result.Add(referenced);
            }
        }
    }

    private static List<GameObject> FindStaticCandidates(Scene scene, HashSet<GameObject> excludedRoots)
    {
        List<GameObject> candidates = new();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MeshRenderer meshRenderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                GameObject candidate = meshRenderer.gameObject;

                if (!IsInsideExcludedObject(candidate.transform, excludedRoots))
                {
                    candidates.Add(candidate);
                }
            }
        }

        return candidates;
    }

    private static bool IsInsideExcludedObject(Transform transform, HashSet<GameObject> excludedRoots)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (excludedRoots.Contains(current.gameObject))
            {
                return true;
            }
        }

        return false;
    }
}
