using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RebuildShopModalHierarchy
{
    [MenuItem("Tools/Rebuild Shop Modal Hierarchy")]
    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity");

        // Ensure EventSystem has InputSystemUIInputModule (Fix freeze/unresponsive UI in Unity 6 New Input System)
        var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            var esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem != null)
        {
            var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null) Object.DestroyImmediate(standalone);

            var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule == null) inputModule = eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
            inputModule.enabled = true;
            EditorUtility.SetDirty(eventSystem.gameObject);
        }
#else
        if (eventSystem != null)
        {
            var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone == null) standalone = eventSystem.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            standalone.enabled = true;
            EditorUtility.SetDirty(eventSystem.gameObject);
        }
#endif

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;

            var shopUI = ArmorShopUI.EnsureUIExists();
            if (shopUI != null)
            {
                shopUI.BuildUI(canvas);
                shopUI.gameObject.SetActive(false);
                EditorUtility.SetDirty(shopUI.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[RebuildShopModalHierarchy] Successfully rebuilt & saved Shop Modal and Input Module in Hierarchy!");
            }
        }
    }
}
