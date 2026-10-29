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
        PlayerWeaponsSetup.Configure();
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
