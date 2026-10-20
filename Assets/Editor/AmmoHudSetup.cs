#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Create Ammo HUD 로 Canvas + AmmoText + AmmoHud 연결을 한 번에 만든다.
public static class AmmoHudSetup
{
    [MenuItem("Tools/Create Ammo HUD")]
    public static void CreateAmmoHud()
    {
        AmmoHud existing = Object.FindFirstObjectByType<AmmoHud>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Ammo HUD", "AmmoHud가 이미 씬에 있습니다.", "확인");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // 기존 Canvas가 있으면 재사용하고, 없으면 새로 만든다.
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(
                "Ammo Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Ammo Canvas");
        }

        GameObject textObject = new GameObject(
            "AmmoText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text),
            typeof(AmmoHud));

        Undo.RegisterCreatedObjectUndo(textObject, "Create Ammo Text");
        textObject.transform.SetParent(canvas.transform, false);

        // 화면 오른쪽 아래에 배치한다.
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-40f, 40f);
        rect.sizeDelta = new Vector2(280f, 50f);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = "AMMO 50 / INF";

        // AmmoHud의 참조를 자동으로 연결한다.
        AmmoHud hud = textObject.GetComponent<AmmoHud>();
        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("_ammoText").objectReferenceValue = text;
        serializedHud.FindProperty("_gun").objectReferenceValue =
            Object.FindFirstObjectByType<PlayerGunFire>();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = textObject;
        EditorSceneManager.MarkSceneDirty(textObject.scene);
    }
}
#endif
