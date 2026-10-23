#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools 메뉴에서 게임 상태 문구(준비/시작/게임오버)를 표시할 UI를 만든다.
public static class GameManagerSetup
{
    [MenuItem("Tools/Create Game Over UI")]
    public static void CreateGameOverUI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (manager == null)
        {
            GameObject managerObject = new GameObject("GameManager", typeof(GameManager));
            Undo.RegisterCreatedObjectUndo(managerObject, "Create GameManager");
            manager = managerObject.GetComponent<GameManager>();
        }

        TextMeshProUGUI stateText = FindStateText();
        if (stateText == null)
        {
            GameObject textObject = new GameObject(
                "StateText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            Undo.RegisterCreatedObjectUndo(textObject, "Create State Text");
            textObject.transform.SetParent(canvas.transform, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1000f, 240f);

            stateText = textObject.GetComponent<TextMeshProUGUI>();
            stateText.text = "준비";
            stateText.fontSize = 96f;
            stateText.alignment = TextAlignmentOptions.Center;
            stateText.color = Color.white;
            stateText.raycastTarget = false;

            TMP_FontAsset koreanFont = FindKoreanFontAsset();
            if (koreanFont != null)
            {
                stateText.font = koreanFont;
            }
        }

        ApplyCenterLayout(stateText);
        stateText.gameObject.SetActive(false);

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty stateTextProperty = serializedManager.FindProperty("_stateText");
        stateTextProperty.objectReferenceValue = stateText;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Selection.activeGameObject = manager.gameObject;
    }

    // StateText를 Canvas 정중앙에 고정하고, pivot도 중앙으로 맞춘다.
    private static void ApplyCenterLayout(TextMeshProUGUI stateText)
    {
        RectTransform rect = stateText.rectTransform;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        if (rect.sizeDelta.x <= 0f || rect.sizeDelta.y <= 0f)
        {
            rect.sizeDelta = new Vector2(1000f, 240f);
        }

        stateText.alignment = TextAlignmentOptions.Center;
        stateText.raycastTarget = false;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Game Over Canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    private static TextMeshProUGUI FindStateText()
    {
        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (TextMeshProUGUI text in texts)
        {
            if (text.gameObject.name == "StateText")
            {
                return text;
            }
        }

        return null;
    }

    private static TMP_FontAsset FindKoreanFontAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/08.Fonts" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Fallback"))
            {
                continue;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
            {
                return font;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }
}
#endif
