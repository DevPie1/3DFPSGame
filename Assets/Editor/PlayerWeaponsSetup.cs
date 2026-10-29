#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerWeaponsSetup
{
    private const string IconFolder = "Assets/04.Images/UI/";

    [MenuItem("Tools/Setup Player Weapons HUD")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Play를 종료한 뒤 HUD를 구성하세요.");
        PlayerGunFire gun = Object.FindFirstObjectByType<PlayerGunFire>();
        if (gun == null) throw new System.InvalidOperationException("플레이어 총을 찾지 못했습니다.");
        PlayerBombFire bomb = gun.GetComponent<PlayerBombFire>();
        if (bomb == null) throw new System.InvalidOperationException("폭탄 컴포넌트를 찾지 못했습니다.");
        PlayerWeapons weapons = gun.GetComponent<PlayerWeapons>();
        if (weapons == null) weapons = Undo.AddComponent<PlayerWeapons>(gun.gameObject);
        Assign(weapons, "_gun", gun);
        Assign(weapons, "_bomb", bomb);
        Assign(weapons, "_aimCamera", Camera.main);
        SerializedObject bombSettings = new SerializedObject(bomb);
        bombSettings.FindProperty("_throwKey").intValue = (int)KeyCode.G;
        bombSettings.FindProperty("_reloadKey").intValue = (int)KeyCode.B;
        bombSettings.ApplyModifiedProperties();

        AmmoHud existing = Object.FindFirstObjectByType<AmmoHud>(FindObjectsInactive.Include);
        Canvas canvas = existing != null ? existing.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (candidate.isRootCanvas && candidate.renderMode != RenderMode.WorldSpace) { canvas = candidate; break; }
        }
        if (canvas == null) throw new System.InvalidOperationException("화면용 Canvas를 찾지 못했습니다.");

        RectTransform panel = EnsureRect(canvas.transform, "PlayerWeaponsHUD");
        Layout(panel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 24), new Vector2(240, 170));
        Text ammo;
        if (existing != null && existing.GetComponent<Text>() != null)
        {
            ammo = existing.GetComponent<Text>();
            Undo.SetTransformParent(ammo.transform, panel, "Move existing ammo HUD");
        }
        else
        {
            ammo = EnsureText(panel, "AmmoText");
            if (existing != null) Undo.DestroyObjectImmediate(existing);
        }
        AmmoHud hud = ammo.GetComponent<AmmoHud>();
        if (hud == null) hud = Undo.AddComponent<AmmoHud>(ammo.gameObject);
        ammo.gameObject.SetActive(true);
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/08.Fonts/강한육군 Medium.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Style(ammo, font, 36);
        Layout(ammo.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 40), new Vector2(230, 50));
        ammo.text = gun.MaxBulletCount + " / " + gun.MaxBulletCount;

        Text bombs = EnsureText(panel, "BombText");
        Style(bombs, font, 24);
        Layout(bombs.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 4), new Vector2(230, 34));
        bombs.text = "폭탄 " + bomb.MaxBombs;
        RectTransform iconRect = EnsureRect(panel, "WeaponIcon");
        Layout(iconRect, new Vector2(.5f, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(210, 74));
        Image icon = Ensure<Image>(iconRect.gameObject);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.color = Color.white;
        Sprite rifle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/EasyFPS/Weapon_Auto.png");
        Sprite sniper = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/EasyFPS/Weapon_Semi.png");
        if (rifle == null) rifle = MakeIcon(false);
        if (sniper == null) sniper = MakeIcon(true);
        icon.sprite = rifle;

        RectTransform scopeRect = EnsureRect(canvas.transform, "WeaponScopeOverlay");
        Undo.RecordObject(scopeRect, "Configure scope overlay");
        scopeRect.anchorMin = Vector2.zero;
        scopeRect.anchorMax = Vector2.one;
        scopeRect.offsetMin = scopeRect.offsetMax = Vector2.zero;
        scopeRect.SetAsFirstSibling();
        UI_ScopeOverlay scope = Ensure<UI_ScopeOverlay>(scopeRect.gameObject);
        scope.raycastTarget = false;
        scope.enabled = false;
        Graphic crosshair = null;
        foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
            if (graphic.name == "Crosshair") { crosshair = graphic; break; }
        Assign(hud, "_weapons", weapons);
        Assign(hud, "_ammoText", ammo);
        Assign(hud, "_bombText", bombs);
        Assign(hud, "_weaponImage", icon);
        Assign(hud, "_rifleSprite", rifle);
        Assign(hud, "_sniperSprite", sniper);
        Assign(hud, "_scopeOverlay", scope);
        Assign(hud, "_normalCrosshair", crosshair);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[PlayerWeapons] HUD 연결 완료: 우클릭 저격, G 충전 투척, R 총 재장전, B 폭탄 재장전");
    }

    private static void Assign(Object target, string field, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    private static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return (RectTransform)found;
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create weapon UI");
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return (RectTransform)go.transform;
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static Text EnsureText(Transform parent, string name)
    {
        return Ensure<Text>(EnsureRect(parent, name).gameObject);
    }

    private static void Layout(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        Undo.RecordObject(rect, "Layout weapon UI");
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Style(Text text, Font font, int fontSize)
    {
        Undo.RecordObject(text, "Style weapon HUD");
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleRight;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.enabled = true;
        Outline outline = Ensure<Outline>(text.gameObject);
        outline.effectColor = new Color(0, 0, 0, .85f);
        outline.effectDistance = new Vector2(1, -1);
    }

    private static Sprite MakeIcon(bool sniper)
    {
        string path = IconFolder + (sniper ? "Weapon_Sniper.png" : "Weapon_Rifle.png");
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;
        // 다각형으로 흰색 무기 실루엣을 생성한다.
        Vector2[] silhouette = sniper
            ? new[] {new Vector2(8,35),new Vector2(45,45),new Vector2(56,59),new Vector2(151,59),new Vector2(155,54),new Vector2(244,54),new Vector2(244,48),new Vector2(163,48),new Vector2(158,40),new Vector2(95,40),new Vector2(88,23),new Vector2(76,23),new Vector2(79,40),new Vector2(52,40),new Vector2(35,29),new Vector2(8,26)}
            : new[] {new Vector2(8,37),new Vector2(39,45),new Vector2(52,61),new Vector2(145,61),new Vector2(150,56),new Vector2(231,56),new Vector2(231,51),new Vector2(249,51),new Vector2(249,46),new Vector2(152,46),new Vector2(147,39),new Vector2(111,39),new Vector2(125,14),new Vector2(107,11),new Vector2(94,39),new Vector2(79,39),new Vector2(72,22),new Vector2(60,22),new Vector2(64,39),new Vector2(44,39),new Vector2(21,26),new Vector2(8,26)};
        Texture2D texture = new Texture2D(256, 96, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[256 * 96];
        for (int y = 0; y < 96; y++)
            for (int x = 0; x < 256; x++)
            {
                bool inside = false;
                for (int i = 0, j = silhouette.Length - 1; i < silhouette.Length; j = i++)
                {
                    Vector2 a = silhouette[i], b = silhouette[j];
                    if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                }
                bool sight = sniper && x >= 82 && x <= 137 && y >= 66 && y <= 77;
                bool mount = sniper && x >= 96 && x <= 121 && y >= 58 && y <= 66;
                bool bipod = sniper && y >= 16 && y <= 47 && (Mathf.Abs(x - (185 + (47 - y) * .35f)) < 2 || Mathf.Abs(x - (185 - (47 - y) * .35f)) < 2);
                if (inside || sight || mount || bipod) pixels[y * 256 + x] = new Color32(255, 255, 255, 255);
            }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
