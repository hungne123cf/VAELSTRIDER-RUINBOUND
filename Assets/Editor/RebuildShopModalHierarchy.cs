using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool to clean up stale ArmorShopModal from the scene.
/// ArmorShopUI now builds its entire UI at runtime via BuildUI() - no scene baking needed.
/// Run this once via Tools/Fix: Remove Stale Shop From Scene to clean up the YAML.
/// </summary>
public class RebuildShopModalHierarchy
{
    [MenuItem("Tools/Fix: Remove Stale Shop From Scene")]
    public static void RemoveStaleShopFromScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        bool dirty = false;

        // 1. Remove ALL ArmorShopModal objects from scene hierarchy
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            if (go.name == "ArmorShopModal" || go.name == "RosterModalPanel" || go.name == "HeroRosterModal" || go.name.Contains("OldRosterModal"))
            {
                Debug.Log($"[Fix] Removing stale modal from scene: {go.name} ({go.GetInstanceID()})");
                Object.DestroyImmediate(go);
                dirty = true;
            }
        }

        var mm = Object.FindAnyObjectByType<MainMenuManager>();
        if (mm != null)
        {
            SerializedObject so = new SerializedObject(mm);
            var rpProp = so.FindProperty("rosterPanel");
            if (rpProp != null && rpProp.objectReferenceValue != null)
            {
                rpProp.objectReferenceValue = null;
                so.ApplyModifiedProperties();
                dirty = true;
            }
        }

        // 2. Ensure EventSystem has the correct Input Module for Unity 6 New Input System
        var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            var esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            dirty = true;
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem != null)
        {
            var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
            {
                Object.DestroyImmediate(standalone);
                dirty = true;
            }

            var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                dirty = true;
            }
            inputModule.AssignDefaultActions();
            inputModule.enabled = true;
            EditorUtility.SetDirty(eventSystem.gameObject);
        }
#else
        if (eventSystem != null)
        {
            var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone == null)
            {
                standalone = eventSystem.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                dirty = true;
            }
            standalone.enabled = true;
            EditorUtility.SetDirty(eventSystem.gameObject);
        }
#endif

        // 3. Ensure main Canvas has a GraphicRaycaster
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                dirty = true;
                EditorUtility.SetDirty(canvas.gameObject);
            }
        }

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShopFix] DONE! Removed stale ArmorShopModal. UI will now be built 100% at runtime by ArmorShopUI.EnsureUIExists().");
        }
        else
        {
            Debug.Log("[ShopFix] No stale ArmorShopModal found in scene. Everything looks clean!");
        }
    }

    // Keep old menu item as alias for backward compatibility
    [MenuItem("Tools/Rebuild Shop Modal Hierarchy")]
    public static void Execute() => RemoveStaleShopFromScene();
}
