#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools 메뉴에서 플레이어 체력/스태미나 UI를 한 번에 만든다.
public static class PlayerStatsSetup
{
    private static readonly Color HealthColor = new Color(0.85f, 0.18f, 0.18f, 1f);
    private static readonly Color StaminaColor = new Color(0.95f, 0.55f, 0.12f, 1f);
    private static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.75f);

    [MenuItem("Tools/Create Player Stats UI")]
    public static void CreatePlayerStatsUI()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Player Stats", "씬에서 PlayerMove를 찾지 못했습니다.", "확인");
            return;
        }

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null)
        {
            stats = Undo.AddComponent<PlayerStats>(player.gameObject);
        }

        UI_PlayerStats existing = Object.FindFirstObjectByType<UI_PlayerStats>();
        if (existing != null)
        {
            AssignReferences(existing, stats);
            EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        GameObject root = new GameObject(
            "UI_PlayerStats",
            typeof(RectTransform),
            typeof(UI_PlayerStats));

        Undo.RegisterCreatedObjectUndo(root, "Create Player Stats UI");
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.zero;
        rootRect.pivot = Vector2.zero;
        rootRect.anchoredPosition = new Vector2(40f, 40f);
        rootRect.sizeDelta = new Vector2(340f, 64f);

        Slider healthSlider = CreateSlider(
            "HealthSlider",
            root.transform,
            new Vector2(0f, 34f),
            new Vector2(320f, 18f),
            HealthColor);

        Slider staminaSlider = CreateSlider(
            "StaminaSlider",
            root.transform,
            new Vector2(0f, 10f),
            new Vector2(300f, 14f),
            StaminaColor);

        UI_PlayerStats ui = root.GetComponent<UI_PlayerStats>();
        AssignReferences(ui, stats, healthSlider, staminaSlider);

        // PlayerMove가 같은 오브젝트의 PlayerStats를 바로 찾도록 연결해 둔다.
        SerializedObject playerObject = new SerializedObject(player);
        SerializedProperty statsProperty = playerObject.FindProperty("_stats");
        if (statsProperty != null)
        {
            statsProperty.objectReferenceValue = stats;
            playerObject.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Player Stats Canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    private static Slider CreateSlider(
        string name,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        Color fillColor)
    {
        DefaultControls.Resources resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        };

        GameObject sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.name = name;
        Undo.RegisterCreatedObjectUndo(sliderObject, "Create " + name);
        sliderObject.transform.SetParent(parent, false);
        sliderObject.transform.localScale = Vector3.one;
        sliderObject.transform.localRotation = Quaternion.identity;

        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        Image background = sliderObject.transform.Find("Background")?.GetComponent<Image>();
        if (background != null)
        {
            background.color = BackgroundColor;
        }

        Image fill = sliderObject.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
        if (fill != null)
        {
            fill.color = fillColor;
        }

        Image handle = sliderObject.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
        if (handle != null)
        {
            handle.color = fillColor;
        }

        return slider;
    }

    private static void AssignReferences(UI_PlayerStats ui, PlayerStats stats)
    {
        AssignReferences(
            ui,
            stats,
            ui.transform.Find("HealthSlider")?.GetComponent<Slider>(),
            ui.transform.Find("StaminaSlider")?.GetComponent<Slider>());
    }

    private static void AssignReferences(
        UI_PlayerStats ui,
        PlayerStats stats,
        Slider healthSlider,
        Slider staminaSlider)
    {
        SerializedObject serializedUI = new SerializedObject(ui);
        serializedUI.FindProperty("_playerStats").objectReferenceValue = stats;
        serializedUI.FindProperty("_healthSlider").objectReferenceValue = healthSlider;
        serializedUI.FindProperty("_staminaSlider").objectReferenceValue = staminaSlider;
        serializedUI.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
