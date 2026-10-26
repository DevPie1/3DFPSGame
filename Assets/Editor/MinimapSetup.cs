#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools 메뉴에서 미니맵 UI, 카메라, 렌더 텍스처, 아이콘을 한 번에 만든다.
public static class MinimapSetup
{
    private const string ImageFolder = "Assets/04.Images/Minimap";
    private const string RenderTexturePath = "Assets/09.materials/MinimapRenderTexture.renderTexture";
    private const string ZombiePrefabPath = "Assets/03.Prefabs/Monster/Zombie.prefab";
    private const string DrumPrefabPath = "Assets/03.Prefabs/DrumCylinder.prefab";
    private const string MinimapLayerName = "Minimap";

    [MenuItem("Tools/Create Minimap")]
    public static void CreateMinimap()
    {
        PlayerMove playerMove = Object.FindFirstObjectByType<PlayerMove>();
        Transform player = playerMove != null ? playerMove.transform : GameObject.FindWithTag("Player")?.transform;

        if (player == null)
        {
            EditorUtility.DisplayDialog("Minimap", "씬에서 Player를 찾지 못했습니다.", "확인");
            return;
        }

        int minimapLayer = EnsureLayer(MinimapLayerName);
        Sprite maskSprite = EnsureSprite("MinimapMask", 256, Color.white, 0f, 0.98f);
        Sprite roundSprite = EnsureSprite("MinimapRound", 256, Color.white, 0.9f, 1f);
        Sprite playerSprite = EnsureSprite("MinimapIcon_Player", 64, new Color(0.2f, 0.9f, 0.3f, 1f), 0f, 0.92f);
        Sprite enemySprite = EnsureSprite("MinimapIcon_Enemy", 64, new Color(0.9f, 0.2f, 0.2f, 1f), 0f, 0.92f);
        Sprite drumSprite = EnsureSprite("MinimapIcon_Drum", 64, new Color(0.95f, 0.8f, 0.1f, 1f), 0f, 0.92f);

        RenderTexture renderTexture = EnsureRenderTexture();
        Camera minimapCamera = EnsureMinimapCamera(player, minimapLayer, renderTexture);

        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();

        GameObject panel = EnsurePanel(canvas);
        UI_Minimap ui = panel.GetComponent<UI_Minimap>();
        if (ui == null)
        {
            ui = Undo.AddComponent<UI_Minimap>(panel);
        }

        EnsureBorder(panel.transform, roundSprite);
        EnsureMaskAndImage(panel.transform, maskSprite, renderTexture);
        TextMeshProUGUI mapNameText = EnsureMapNameText(panel.transform);
        Button zoomOutButton = EnsureButton(panel.transform, "ZoomOutButton", "-", false);
        Button zoomInButton = EnsureButton(panel.transform, "ZoomInButton", "+", true);

        AssignUI(ui, minimapCamera, mapNameText);
        AssignButton(zoomOutButton, ui, false);
        AssignButton(zoomInButton, ui, true);

        EnsurePlayerIcon(player, minimapLayer, playerSprite);
        EnsurePrefabIcon(ZombiePrefabPath, minimapLayer, enemySprite, new Vector3(0f, 3f, 0f));
        EnsurePrefabIcon(DrumPrefabPath, minimapLayer, drumSprite, new Vector3(0f, 2f, 0f));

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.cullingMask &= ~(1 << minimapLayer);
            EditorUtility.SetDirty(mainCamera);
        }

        EditorSceneManager.MarkSceneDirty(panel.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = panel;

        Debug.Log("[Minimap] 생성 완료. 씬을 저장한 뒤 Play로 확인하세요.");
    }

    private static int EnsureLayer(string layerName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets.Length == 0)
        {
            return 10;
        }

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
            {
                return i;
            }
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                return i;
            }
        }

        Debug.LogError("[Minimap] 빈 User Layer가 없습니다. Layer를 직접 추가하세요.");
        return 10;
    }

    private static Sprite EnsureSprite(string fileName, int size, Color color, float innerRatio, float outerRatio)
    {
        Directory.CreateDirectory(ImageFolder);

        string path = ImageFolder + "/" + fileName + ".png";

        if (File.Exists(path) == false)
        {
            WriteCirclePng(path, size, color, innerRatio, outerRatio);
            AssetDatabase.ImportAsset(path);
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.alphaIsTransparency == false)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            importer.spritePixelsPerUnit = 100f;

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void WriteCirclePng(string path, int size, Color color, float innerRatio, float outerRatio)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                float alpha = Mathf.Clamp01((outerRatio - distance) * radius);

                if (innerRatio > 0f)
                {
                    alpha *= Mathf.Clamp01((distance - innerRatio) * radius);
                }

                pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static RenderTexture EnsureRenderTexture()
    {
        RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);

        if (renderTexture == null)
        {
            Directory.CreateDirectory("Assets/09.materials");
            renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            renderTexture.name = "MinimapRenderTexture";
            AssetDatabase.CreateAsset(renderTexture, RenderTexturePath);
        }

        renderTexture.antiAliasing = 1;
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        EditorUtility.SetDirty(renderTexture);

        return renderTexture;
    }

    private static Camera EnsureMinimapCamera(Transform player, int minimapLayer, RenderTexture renderTexture)
    {
        GameObject cameraObject = GameObject.Find("MinimapCamera");

        if (cameraObject == null)
        {
            cameraObject = new GameObject("MinimapCamera", typeof(Camera), typeof(MinimapCamera));
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create Minimap Camera");
        }

        Camera camera = cameraObject.GetComponent<Camera>();
        if (camera == null)
        {
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        camera.targetTexture = renderTexture;
        camera.cullingMask = (1 << 0) | (1 << minimapLayer);
        camera.depth = 0f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 100f;
        camera.allowHDR = false;
        camera.allowMSAA = false;

        AudioListener listener = cameraObject.GetComponent<AudioListener>();
        if (listener != null)
        {
            Object.DestroyImmediate(listener);
        }

        MinimapCamera minimapCamera = cameraObject.GetComponent<MinimapCamera>();
        if (minimapCamera == null)
        {
            minimapCamera = cameraObject.AddComponent<MinimapCamera>();
        }

        SerializedObject serializedCamera = new SerializedObject(minimapCamera);
        serializedCamera.FindProperty("_target").objectReferenceValue = player;
        serializedCamera.FindProperty("_height").floatValue = 10f;
        serializedCamera.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(camera);
        return camera;
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = null;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas candidate in canvases)
        {
            // 좀비의 월드 공간 체력바는 화면 HUD로 사용하지 않는다.
            if (candidate.renderMode == RenderMode.ScreenSpaceOverlay &&
                candidate.transform.parent == null)
            {
                canvas = candidate;
                break;
            }
        }

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Minimap Canvas");
            canvas = canvasObject.GetComponent<Canvas>();
        }

        // 미니맵 UI는 화면 위에 고정되어야 하므로 Overlay로 맞춘다.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        EditorUtility.SetDirty(canvas);
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));

        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
    }

    private static GameObject EnsurePanel(Canvas canvas)
    {
        UI_Minimap existing = Object.FindFirstObjectByType<UI_Minimap>();
        GameObject panel;

        if (existing != null)
        {
            panel = existing.gameObject;
        }
        else
        {
            panel = new GameObject(
                "UI_Minimap",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(UI_Minimap));

            Undo.RegisterCreatedObjectUndo(panel, "Create Minimap Panel");
        }

        panel.SetActive(true);
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling();

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-12f, -12f);
        rect.sizeDelta = new Vector2(330f, 440f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        Image background = panel.GetComponent<Image>();
        if (background == null)
        {
            background = panel.AddComponent<Image>();
        }

        background.enabled = true;
        background.color = new Color(0f, 0f, 0f, 150f / 255f);
        background.raycastTarget = false;

        return panel;
    }

    private static void EnsureBorder(Transform parent, Sprite sprite)
    {
        Transform existing = parent.Find("MinimapRound");
        GameObject border = existing != null
            ? existing.gameObject
            : CreateImageObject("MinimapRound", parent);

        border.SetActive(true);

        Image image = border.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.black;
        image.raycastTarget = false;

        PlaceTopCenter(border.GetComponent<RectTransform>(), 300f, 300f, -55f);

        // 테두리는 마스크보다 먼저 그려져 미니맵 뒤쪽에 깔린다.
        border.transform.SetSiblingIndex(0);
    }

    private static void EnsureMaskAndImage(Transform parent, Sprite maskSprite, RenderTexture renderTexture)
    {
        Transform existingMask = parent.Find("MinimapMask");
        GameObject maskObject = existingMask != null
            ? existingMask.gameObject
            : new GameObject("MinimapMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));

        if (existingMask == null)
        {
            Undo.RegisterCreatedObjectUndo(maskObject, "Create Minimap Mask");
            maskObject.transform.SetParent(parent, false);
        }

        maskObject.SetActive(true);

        RectTransform maskRect = maskObject.GetComponent<RectTransform>();
        PlaceTopCenter(maskRect, 300f, 300f, -55f);

        Image maskImage = maskObject.GetComponent<Image>();
        maskImage.sprite = maskSprite;
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;

        Mask mask = maskObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        Transform existingImage = maskObject.transform.Find("MinimapImage");
        GameObject imageObject = existingImage != null
            ? existingImage.gameObject
            : new GameObject("MinimapImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));

        if (existingImage == null)
        {
            Undo.RegisterCreatedObjectUndo(imageObject, "Create Minimap Image");
            imageObject.transform.SetParent(maskObject.transform, false);
        }

        imageObject.SetActive(true);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        RawImage rawImage = imageObject.GetComponent<RawImage>();
        rawImage.texture = renderTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;
    }

    private static TextMeshProUGUI EnsureMapNameText(Transform parent)
    {
        Transform existing = parent.Find("MapNameText");
        GameObject textObject = existing != null
            ? existing.gameObject
            : new GameObject("MapNameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));

        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(textObject, "Create Minimap Map Name");
            textObject.transform.SetParent(parent, false);
        }

        textObject.SetActive(true);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        PlaceTopCenter(rect, 300f, 40f, -10f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = SceneManager.GetActiveScene().name;
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        TMP_FontAsset koreanFont = FindKoreanFontAsset();
        if (koreanFont != null)
        {
            text.font = koreanFont;
        }

        return text;
    }

    private static Button EnsureButton(Transform parent, string name, string label, bool rightSide)
    {
        Transform existing = parent.Find(name);
        GameObject buttonObject;

        if (existing != null)
        {
            buttonObject = existing.gameObject;
        }
        else
        {
            buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            Undo.RegisterCreatedObjectUndo(buttonObject, "Create " + name);
            buttonObject.transform.SetParent(parent, false);

            GameObject labelObject = new GameObject(
                "Text (TMP)",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset koreanFont = FindKoreanFontAsset();
            if (koreanFont != null)
            {
                labelText.font = koreanFont;
            }

            labelText.fontSize = 40f;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
        }

        buttonObject.SetActive(true);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(rightSide ? 1f : 0f, 0f);
        rect.anchorMax = new Vector2(rightSide ? 1f : 0f, 0f);
        rect.pivot = new Vector2(rightSide ? 1f : 0f, 0f);
        rect.anchoredPosition = new Vector2(rightSide ? -15f : 15f, 15f);
        rect.sizeDelta = new Vector2(140f, 50f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI childText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        if (childText != null)
        {
            childText.text = label;
        }

        return button;
    }

    private static void AssignUI(UI_Minimap ui, Camera minimapCamera, TextMeshProUGUI mapNameText)
    {
        SerializedObject serializedUI = new SerializedObject(ui);
        serializedUI.FindProperty("_minimapCamera").objectReferenceValue = minimapCamera;
        serializedUI.FindProperty("_mapNameText").objectReferenceValue = mapNameText;
        serializedUI.FindProperty("_zoomStep").floatValue = 2f;
        serializedUI.FindProperty("_minSize").floatValue = 4f;
        serializedUI.FindProperty("_maxSize").floatValue = 16f;
        serializedUI.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignButton(Button button, UI_Minimap ui, bool zoomIn)
    {
        while (button.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        }

        if (zoomIn)
        {
            UnityEventTools.AddPersistentListener(button.onClick, ui.ZoomIn);
        }
        else
        {
            UnityEventTools.AddPersistentListener(button.onClick, ui.ZoomOut);
        }

        EditorUtility.SetDirty(button);
    }

    private static void EnsurePlayerIcon(Transform player, int minimapLayer, Sprite sprite)
    {
        Transform existing = player.Find("MinimapIcon");
        GameObject iconObject = existing != null
            ? existing.gameObject
            : new GameObject("MinimapIcon", typeof(SpriteRenderer));

        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(iconObject, "Create Player Minimap Icon");
            iconObject.transform.SetParent(player, false);
        }

        iconObject.transform.localPosition = new Vector3(0f, 3f, 0f);
        iconObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        iconObject.transform.localScale = Vector3.one * 1.5f;
        iconObject.layer = minimapLayer;

        SpriteRenderer renderer = iconObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 10;
    }

    private static void EnsurePrefabIcon(string prefabPath, int minimapLayer, Sprite sprite, Vector3 localPosition)
    {
        if (File.Exists(prefabPath) == false)
        {
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            Transform existing = root.transform.Find("MinimapIcon");
            GameObject iconObject = existing != null
                ? existing.gameObject
                : new GameObject("MinimapIcon", typeof(SpriteRenderer));

            if (existing == null)
            {
                iconObject.transform.SetParent(root.transform, false);
            }

            iconObject.transform.localPosition = localPosition;
            iconObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            iconObject.transform.localScale = Vector3.one * 1.5f;
            iconObject.layer = minimapLayer;

            SpriteRenderer renderer = iconObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 10;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static GameObject CreateImageObject(string name, Transform parent)
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        Undo.RegisterCreatedObjectUndo(imageObject, "Create " + name);
        imageObject.transform.SetParent(parent, false);
        return imageObject;
    }

    private static void PlaceTopCenter(RectTransform rect, float width, float height, float y)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(width, height);
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
