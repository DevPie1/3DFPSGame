#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools 메뉴에서 HUD와 폭탄 던지기 설정을 한 번에 만들 수 있게 한다.
public static class AmmoHudSetup
{
    private const string BombPrefabPath = "Assets/03.Prefabs/Weapon/Bomb.prefab";

    [MenuItem("Tools/Create Ammo HUD")]
    public static void CreateAmmoHud()
    {
        AmmoHud existing = Object.FindFirstObjectByType<AmmoHud>();
        if (existing != null)
        {
            // 이미 HUD가 있으면 레이아웃과 폭탄 참조만 갱신한다.
            Text existingText = existing.GetComponent<Text>();
            if (existingText != null)
            {
                ApplyHudLayout(existingText);
            }

            SerializedObject existingSerialized = new SerializedObject(existing);
            SerializedProperty bombProperty = existingSerialized.FindProperty("_bomb");
            if (bombProperty != null && bombProperty.objectReferenceValue == null)
            {
                bombProperty.objectReferenceValue = Object.FindFirstObjectByType<PlayerBombFire>();
                existingSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
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

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.white;
        text.raycastTarget = false;
        ApplyHudLayout(text);
        text.text = "AMMO 50 / INF\nBOMB 10 / INF";

        // AmmoHud의 참조를 자동으로 연결한다.
        AmmoHud hud = textObject.GetComponent<AmmoHud>();
        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("_ammoText").objectReferenceValue = text;
        serializedHud.FindProperty("_gun").objectReferenceValue =
            Object.FindFirstObjectByType<PlayerGunFire>();
        serializedHud.FindProperty("_bomb").objectReferenceValue =
            Object.FindFirstObjectByType<PlayerBombFire>();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(textObject.scene);
        Selection.activeGameObject = textObject;
    }

    [MenuItem("Tools/Setup Bomb Thrower")]
    public static void SetupBombThrower()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Bomb Thrower", "씬에서 PlayerMove를 찾지 못했습니다.", "확인");
            return;
        }

        PlayerBombFire bombFire = player.GetComponent<PlayerBombFire>();
        if (bombFire == null)
        {
            bombFire = Undo.AddComponent<PlayerBombFire>(player.gameObject);
        }

        Bomb bombPrefab = AssetDatabase.LoadAssetAtPath<Bomb>(BombPrefabPath);
        if (bombPrefab == null)
        {
            EditorUtility.DisplayDialog("Bomb Thrower", BombPrefabPath + " 를 찾지 못했습니다.", "확인");
            return;
        }

        Transform firePoint = player.transform.Find("BombFirePoint");
        if (firePoint == null)
        {
            GameObject firePointObject = new GameObject("BombFirePoint");
            Undo.RegisterCreatedObjectUndo(firePointObject, "Create Bomb Fire Point");
            firePointObject.transform.SetParent(player.transform, false);
            // 플레이어 콜라이더(반지름 0.5) + 폭탄 콜라이더(0.5)와 겹치지 않게 앞으로 띄운다.
            firePointObject.transform.localPosition = new Vector3(0f, 1.2f, 1.2f);
            firePoint = firePointObject.transform;
        }

        SerializedObject serializedBombFire = new SerializedObject(bombFire);
        serializedBombFire.FindProperty("_firePoint").objectReferenceValue = firePoint;
        serializedBombFire.FindProperty("_bombPrefab").objectReferenceValue = bombPrefab;
        serializedBombFire.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(bombFire.gameObject.scene);
        Selection.activeGameObject = bombFire.gameObject;
    }

    private static void ApplyHudLayout(Text text)
    {
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = text.rectTransform;
        if (rect.sizeDelta.y < 90f)
        {
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 320f), 90f);
        }
    }
}
#endif
