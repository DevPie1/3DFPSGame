using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class MeleeCombatSetup
{
    private const string EffectFolder = "Assets/03.Prefabs/Weapon/Effects/EasyFPS";
    private const string IconPath = "Assets/04.Images/UI/Weapon_Katana.png";

    [MenuItem("Tools/Player/Setup Chapter 20 Melee Combat")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 종료하세요.");
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player == null) throw new InvalidOperationException("GameScene의 Player가 필요합니다.");
        Directory.CreateDirectory(".utmp");
        if (!File.Exists(".utmp/GameScene.before-chapter20.unity"))
            EditorSceneManager.SaveScene(player.gameObject.scene, ".utmp/GameScene.before-chapter20.unity", true);
        var controller = SpartanCombatAnimations.Build();
        int drumLayer = EnsureLayer("Drum");
        int zombieLayer = EnsureLayer("Zombie");
        GameObject blood = MakeBloodEffect();
        ConfigurePrefab("Assets/03.Prefabs/Monster/Zombie.prefab", zombieLayer, blood);
        ConfigurePrefab("Assets/03.Prefabs/DrumCylinder.prefab", drumLayer, null);
        foreach (Zombie zombie in Object.FindObjectsByType<Zombie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            PaintLayer(zombie.gameObject, zombieLayer);
            Assign(zombie, "_hitEffectPrefab", blood);
        }
        foreach (DrumCylinder drum in Object.FindObjectsByType<DrumCylinder>(FindObjectsInactive.Include, FindObjectsSortMode.None)) PaintLayer(drum.gameObject, drumLayer);
        foreach (Box box in Object.FindObjectsByType<Box>(FindObjectsInactive.Include, FindObjectsSortMode.None)) PaintLayer(box.gameObject, drumLayer);

        Transform model = player.transform.Find("Model");
        Animator animator = model.GetComponent<Animator>();
        Undo.RecordObject(animator, "Configure Melee Animator");
        animator.runtimeAnimatorController = controller;
        animator.avatar = null;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        PlayerMeleeAttack melee = Ensure<PlayerMeleeAttack>(player.gameObject);
        Ensure<PlayerMeleeEvent>(model.gameObject);
        Transform katana = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "SpartanKatana");
        Transform blade = katana.Find("Blade");
        if (blade == null)
        {
            var go = new GameObject("Blade");
            Undo.RegisterCreatedObjectUndo(go, "Create Blade Hitbox");
            go.transform.SetParent(katana, false);
            blade = go.transform;
        }
        // JTS 모델의 칼날은 로컬 Z축 방향이다. 코등이부터 끝부분만 덮는다.
        blade.localPosition = new Vector3(0, .018f, .365f);
        blade.localRotation = Quaternion.identity;
        blade.localScale = Vector3.one;
        blade.gameObject.layer = 2;
        BoxCollider hitbox = Ensure<BoxCollider>(blade.gameObject);
        hitbox.center = Vector3.zero;
        hitbox.size = new Vector3(.07f, .035f, .69f);
        hitbox.isTrigger = true;
        hitbox.enabled = false;
        Rigidbody rigidbody = Ensure<Rigidbody>(blade.gameObject);
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        Ensure<MeleeWeapon>(blade.gameObject);
        Assign(melee, "_katana", katana.gameObject);
        Assign(melee, "_katanaCollider", hitbox);
        Assign(melee, "_animator", animator);
        var meleeData = new SerializedObject(melee);
        meleeData.FindProperty("_hitMode").enumValueIndex = (int)MeleeHitMode.Fan;
        meleeData.FindProperty("_targetMask").intValue = (1 << drumLayer) | (1 << zombieLayer);
        meleeData.FindProperty("_obstacleMask").intValue = 1;
        meleeData.ApplyModifiedProperties();
        katana.gameObject.SetActive(false);

        PlayerGunFire gun = player.GetComponent<PlayerGunFire>();
        EasyFpsWeaponFeedback feedback = player.GetComponent<EasyFpsWeaponFeedback>();
        gun.enabled = true;
        if (feedback != null) feedback.enabled = true;
        Transform muzzle = Camera.main.transform.Find("GunMuzzle");
        if (muzzle == null)
        {
            var go = new GameObject("GunMuzzle");
            Undo.RegisterCreatedObjectUndo(go, "Reconnect Active Gun Muzzle");
            go.transform.SetParent(Camera.main.transform, false);
            go.transform.localPosition = new Vector3(.15f, -.12f, .5f);
            muzzle = go.transform;
        }
        Assign(gun, "_muzzle", muzzle);
        if (feedback != null) Assign(feedback, "_muzzle", muzzle);
        Assign(player, "_animator", animator);
        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (manager != null) Assign(manager, "_playerAnimator", animator);
        Sprite icon = MakeIcon(katana.GetComponent<MeshFilter>().sharedMesh);
        foreach (AmmoHud hud in Object.FindObjectsByType<AmmoHud>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Assign(hud, "_meleeSprite", icon);
        EditorUtility.SetDirty(gun);
        if (feedback != null) EditorUtility.SetDirty(feedback);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(player.gameObject.scene);
        Debug.Log("[Melee] 20.1~20.9: 1/2/3 모드, Slash 상체 레이어, 이벤트, Collider/Fan, 무기 UI 연결 완료.");
    }

    private static GameObject MakeBloodEffect()
    {
        SpartanCombatAnimations.EnsureFolder(EffectFolder);
        string materialPath = "Assets/09.materials/EasyFPS/BloodParticle.mat";
        string texturePath = "Assets/04.Images/UI/BloodParticle.png";
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float radius = new Vector2((x + .5f - 32) / 32f, (y + .5f - 32) / 32f).magnitude;
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - radius) * 4)));
            }
        texture.Apply();
        File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(texturePath);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(material, materialPath); }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 0);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        string path = EffectFolder + "/MeleeBlood.prefab";
        GameObject clone = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Easy FPS/Prefabs/bloodEffectParticlePrefab_melee.prefab"));
        try
        {
            clone.name = "MeleeBlood";
            foreach (ParticleSystemRenderer renderer in clone.GetComponentsInChildren<ParticleSystemRenderer>()) renderer.sharedMaterial = material;
            return PrefabUtility.SaveAsPrefabAsset(clone, path);
        }
        finally { Object.DestroyImmediate(clone); }
    }

    private static Sprite MakeIcon(Mesh mesh)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var rt = new RenderTexture(256, 96, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(256, 96, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            material.SetColor("_BaseColor", Color.white);
            var model = new GameObject("IconKatana");
            SceneManager.MoveGameObjectToScene(model, scene);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = material;
            var go = new GameObject("IconCamera");
            SceneManager.MoveGameObjectToScene(go, scene);
            Camera camera = go.AddComponent<Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = mesh.bounds.size.z * .5f / (256f / 96f) * 1.12f;
            camera.transform.position = mesh.bounds.center + Vector3.right * 2;
            camera.transform.LookAt(mesh.bounds.center, Vector3.up);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 5;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 256, 96), 0, 0);
            texture.Apply();
            File.WriteAllBytes(IconPath, texture.EncodeToPNG());
            camera.targetTexture = null;
        }
        finally
        {
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(material); Object.DestroyImmediate(rt); Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(IconPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
    }

    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 9; i < 32; i++)
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                layers.GetArrayElementAtIndex(i).stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        throw new InvalidOperationException("빈 레이어가 없습니다: " + name);
    }

    private static void ConfigurePrefab(string path, int layer, GameObject blood)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            PaintLayer(root, layer);
            Zombie zombie = root.GetComponent<Zombie>();
            if (zombie != null) Assign(zombie, "_hitEffectPrefab", blood);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void PaintLayer(GameObject root, int layer)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.gameObject.layer == 5 || child.gameObject.layer == 8) continue;
            Undo.RecordObject(child.gameObject, "Configure Melee Target Layer");
            child.gameObject.layer = layer;
            PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
        }
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static void Assign(Object target, string property, Object reference)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = reference;
        serialized.ApplyModifiedProperties();
    }
}
